# Level Flow — Loading Sequence & Exit Door

Design for the full level lifecycle: fade → load level prefab → spawn player → play →
walk into exit door → fade → next level.

Written against the existing architecture: VContainer DI, UniTask, PrimeTween,
`ISignalBus`, `BaseFeatureInstaller` / `BaseLifetimeScope`.

Nothing here modifies existing scripts except the small additions listed in
[§7 Changes to existing scripts](#7-changes-to-existing-scripts).

---

## 1. The two sequences

### Level load

```
1.  fader.FadeToBlackAsync()              screen covered
2.  Destroy(current level instance)
3.  resolver.Instantiate(level prefab)    NOT Object.Instantiate — see §8
4.  await UniTask.NextFrame()             let the level's Awake/Start run
5.  player.EnterCutscene()                input off, velocity zeroed
6.  player.TeleportTo(ctx.SpawnPoint)     + Physics2D.SyncTransforms()
7.  camera snapped to player              while still black
8.  publish LevelReadySignal
9.  fader.FadeFromBlackAsync()
10. player.ExitCutscene()                 control handed back
```

### Level complete (exit door)

```
1.  Player enters door trigger            guarded, fires once
2.  player.EnterCutscene()                input off
3.  Tween player position → door anchor   ~0.35s, Ease.OutQuad
4.  Tween player sprite alpha → 0         ~0.4s, Ease.InQuad
5.  Animator "Close" trigger              await clip length
6.  fader.FadeToBlackAsync()
7.  publish LevelCompletedSignal
8.  LevelFlowController hears it → load next level (sequence above)
```

Both sequences leave the screen black at the boundary, so they compose: the door
sequence ends covered and the load sequence begins covered.

---

## 2. Scene & hierarchy layout

```
Bootstrap (scene)
├── RootLifetimeScope                  ISignalBus, ISceneLoader, IInputReader,
│                                      IAudioService, + IScreenFader (new)
└── ScreenFaderCanvas                  Canvas, Screen Space Overlay, Sort Order 1000
    └── FadeImage                      full-screen black Image + CanvasGroup
                                       → ScreenFaderView lives here

Gameplay (scene, loaded additively by BootstrapEntryPoint)
├── GameLifetimeScope
│   ├── PlayerFeatureInstaller         existing
│   └── LevelFeatureInstaller          new: catalog, levelRoot, startLevelIndex
├── Player                             persistent across levels
├── CameraRig
│   └── Main Camera                    CameraShaker
└── LevelRoot                          empty; level prefabs instantiate here
```

Level prefab:

```
Level2 (prefab)
├── LevelContext                       spawnPoint, exitDoor  ← the contract
├── SpawnPoint                         empty transform
├── Level_Grid                         tilemaps + CompositeCollider2D
├── Doors
│   ├── DoorEntry
│   └── DoorExit                       LevelExitDoor + Animator
│       └── EntryAnchor                empty; where the player lines up
├── FakeTiles
└── Obstacles                          MovingPlatformAction / MovingSpikeAction
```

**Keep level prefab roots at scale (1,1,1) and rotation identity.** Obstacle
offsets in `MoveObstacleAction` are expressed in parent space, so a scaled level
root silently scales every platform's travel distance.

---

## 3. New contracts (DZ.Core / Contracts)

### IScreenFader.cs

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DZ.Core.Contracts
{
    public interface IScreenFader
    {
        bool IsCovered { get; }
        UniTask FadeToBlackAsync(float duration = -1f, CancellationToken cancellation = default);
        UniTask FadeFromBlackAsync(float duration = -1f, CancellationToken cancellation = default);
        void SetCoveredImmediate(bool covered);
    }
}
```

`duration = -1f` means "use the serialized default" — keeps call sites clean.

### LevelSignals.cs

`ISignalBus` constrains to `where T : struct`, so these are readonly structs.

```csharp
namespace DZ.Core.Contracts
{
    public readonly struct LevelLoadStartedSignal
    {
        public readonly int LevelIndex;
        public LevelLoadStartedSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    public readonly struct LevelReadySignal
    {
        public readonly int LevelIndex;
        public LevelReadySignal(int levelIndex) => LevelIndex = levelIndex;
    }

    public readonly struct LevelCompletedSignal
    {
        public readonly int LevelIndex;
        public LevelCompletedSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    public readonly struct PlayerDiedSignal { }
}
```

---

## 4. Core runtime

### ScreenFaderView.cs (DZ.Core / Runtime / UI)

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using PrimeTween;
using UnityEngine;

namespace DZ.Core.Runtime
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFaderView : MonoBehaviour, IScreenFader
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _defaultDuration = 0.4f;

        private Tween _fadeTween;

        public bool IsCovered => _canvasGroup.alpha >= 0.999f;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            SetCoveredImmediate(true);   // the game boots black
        }

        public UniTask FadeToBlackAsync(float duration = -1f, CancellationToken cancellation = default)
            => FadeAsync(1f, duration, cancellation);

        public UniTask FadeFromBlackAsync(float duration = -1f, CancellationToken cancellation = default)
            => FadeAsync(0f, duration, cancellation);

        public void SetCoveredImmediate(bool covered)
        {
            if (_fadeTween.isAlive) _fadeTween.Stop();
            _canvasGroup.alpha = covered ? 1f : 0f;
            _canvasGroup.blocksRaycasts = covered;
        }

        private async UniTask FadeAsync(float targetAlpha, float duration, CancellationToken cancellation)
        {
            if (duration < 0f) duration = _defaultDuration;
            if (_fadeTween.isAlive) _fadeTween.Stop();

            _canvasGroup.blocksRaycasts = true;

            // useUnscaledTime so a paused / slow-mo game still fades.
            _fadeTween = Tween.Alpha(_canvasGroup, targetAlpha, duration,
                                     ease: Ease.InOutQuad, useUnscaledTime: true);

            try
            {
                await _fadeTween.ToUniTask().AttachExternalCancellation(cancellation);
            }
            catch (OperationCanceledException)
            {
                // fall through — finally still lands us on the target alpha
            }
            finally
            {
                if (_fadeTween.isAlive) _fadeTween.Stop();
                _canvasGroup.alpha = targetAlpha;
                _canvasGroup.blocksRaycasts = targetAlpha > 0.001f;
            }
        }
    }
}
```

`Tween.ToUniTask()` has no cancellation-token overload, hence
`AttachExternalCancellation`. That detaches the await but leaves the tween
running, so the `finally` stops it explicitly.

---

## 5. Level data (DZ.Features / Level)

### LevelDefinitionSo.cs

```csharp
using UnityEngine;

namespace DZ.Features
{
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "DayZeroGames/Level Definition")]
    public sealed class LevelDefinitionSo : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _levelPrefab;

        public string DisplayName => _displayName;
        public GameObject LevelPrefab => _levelPrefab;
    }
}
```

### LevelCatalogSo.cs

```csharp
using UnityEngine;

namespace DZ.Features
{
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "DayZeroGames/Level Catalog")]
    public sealed class LevelCatalogSo : ScriptableObject
    {
        [SerializeField] private LevelDefinitionSo[] _levels;

        public int Count => _levels.Length;
        public bool HasLevel(int index) => index >= 0 && index < _levels.Length;
        public LevelDefinitionSo Get(int index) => _levels[index];
    }
}
```

### LevelContext.cs — sits on the level prefab root

This is the only thing the loader knows about a level. Everything else is
internal to the prefab.

```csharp
using UnityEngine;

namespace DZ.Features
{
    public sealed class LevelContext : MonoBehaviour
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private LevelExitDoor _exitDoor;

        public Transform SpawnPoint => _spawnPoint;
        public LevelExitDoor ExitDoor => _exitDoor;

        private void Awake()
        {
            if (_spawnPoint == null) Debug.LogError($"{name}: no spawn point assigned.", this);
            if (_exitDoor == null)   Debug.LogError($"{name}: no exit door assigned.", this);
        }
    }
}
```

---

## 6. The orchestrator

### LevelFlowController.cs

Plain class, registered as a VContainer entry point — same shape as the existing
`BootstrapEntryPoint`.

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public sealed class LevelFlowController : IAsyncStartable, IDisposable
    {
        private readonly IScreenFader _fader;
        private readonly ISignalBus _signalBus;
        private readonly IObjectResolver _resolver;
        private readonly PlayerController _player;
        private readonly LevelCatalogSo _catalog;
        private readonly Transform _levelRoot;
        private readonly int _startLevelIndex;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private GameObject _currentInstance;
        private LevelContext _currentContext;
        private int _currentIndex = -1;
        private bool _isTransitioning;

        public LevelFlowController(
            IScreenFader fader,
            ISignalBus signalBus,
            IObjectResolver resolver,
            PlayerController player,
            LevelCatalogSo catalog,
            Transform levelRoot,
            int startLevelIndex)
        {
            _fader = fader;
            _signalBus = signalBus;
            _resolver = resolver;
            _player = player;
            _catalog = catalog;
            _levelRoot = levelRoot;
            _startLevelIndex = startLevelIndex;
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            _signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus.Subscribe<PlayerDiedSignal>(OnPlayerDied);

            await LoadLevelAsync(_startLevelIndex, _cts.Token);
        }

        public async UniTask LoadLevelAsync(int index, CancellationToken cancellation)
        {
            if (!_catalog.HasLevel(index))
            {
                Debug.LogWarning($"No level at index {index}. Catalog holds {_catalog.Count}.");
                return;
            }

            if (_isTransitioning) return;
            _isTransitioning = true;

            try
            {
                _signalBus.Publish(new LevelLoadStartedSignal(index));

                await _fader.FadeToBlackAsync(cancellation: cancellation);

                UnloadCurrentLevel();

                // resolver.Instantiate, NOT Object.Instantiate — see §8
                var definition = _catalog.Get(index);
                _currentInstance = _resolver.Instantiate(definition.LevelPrefab, _levelRoot);
                _currentContext = _currentInstance.GetComponent<LevelContext>();

                if (_currentContext == null)
                {
                    Debug.LogError($"'{definition.LevelPrefab.name}' has no LevelContext on its root.");
                    return;
                }

                // Let the new hierarchy run Awake/Start before positioning the player.
                await UniTask.NextFrame(cancellation);

                _player.EnterCutscene();
                _player.TeleportTo(_currentContext.SpawnPoint.position);

                _currentIndex = index;
                _signalBus.Publish(new LevelReadySignal(index));

                await _fader.FadeFromBlackAsync(cancellation: cancellation);

                _player.ExitCutscene();
            }
            catch (OperationCanceledException)
            {
                // scope torn down mid-transition; nothing to clean beyond finally
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private void UnloadCurrentLevel()
        {
            if (_currentInstance == null) return;
            UnityEngine.Object.Destroy(_currentInstance);
            _currentInstance = null;
            _currentContext = null;
        }

        private void OnLevelCompleted(LevelCompletedSignal signal) => AdvanceAsync().Forget();
        private void OnPlayerDied(PlayerDiedSignal signal) => RetryAsync().Forget();

        private async UniTaskVoid AdvanceAsync()
        {
            var next = _currentIndex + 1;
            if (!_catalog.HasLevel(next))
            {
                Debug.Log("All levels complete.");
                return;
            }
            await LoadLevelAsync(next, _cts.Token);
        }

        private async UniTaskVoid RetryAsync()
        {
            await LoadLevelAsync(_currentIndex, _cts.Token);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus.Unsubscribe<PlayerDiedSignal>(OnPlayerDied);
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
```

### LevelExitDoor.cs

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelExitDoor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [Tooltip("Where the player lines up before fading out. Child of the door.")]
        [SerializeField] private Transform _entryAnchor;
        [SerializeField] private string _playerTag = "Player";

        [Header("Timing")]
        [SerializeField] private float _walkInDuration = 0.35f;
        [SerializeField] private float _playerFadeDuration = 0.4f;
        [SerializeField] private float _doorCloseDuration = 0.6f;

        private static readonly int CloseHash = Animator.StringToHash("Close");

        [Inject] private readonly ISignalBus _signalBus;
        [Inject] private readonly IScreenFader _fader;

        private bool _isRunning;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (_entryAnchor == null) _entryAnchor = transform;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isRunning || !other.CompareTag(_playerTag)) return;
            if (!other.TryGetComponent(out PlayerController player)) return;

            RunExitSequenceAsync(player, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunExitSequenceAsync(PlayerController player, CancellationToken ct)
        {
            _isRunning = true;

            try
            {
                player.EnterCutscene();

                // 1. slide the player into the doorway
                await Tween.Position(player.transform, _entryAnchor.position,
                                     _walkInDuration, Ease.OutQuad)
                           .ToUniTask().AttachExternalCancellation(ct);

                // 2. fade the player out
                await Tween.Alpha(player.SpriteRenderer, 0f,
                                  _playerFadeDuration, Ease.InQuad)
                           .ToUniTask().AttachExternalCancellation(ct);

                // 3. close the door
                if (_animator != null) _animator.SetTrigger(CloseHash);
                await UniTask.Delay(TimeSpan.FromSeconds(_doorCloseDuration), cancellationToken: ct);

                // 4. cover the screen, then hand off to the flow controller
                await _fader.FadeToBlackAsync(cancellation: ct);

                _signalBus.Publish(new LevelCompletedSignal(0));
            }
            catch (OperationCanceledException)
            {
                // level torn down mid-sequence
            }
            finally
            {
                _isRunning = false;
            }
        }
    }
}
```

The `LevelCompletedSignal(0)` index is ignored by `LevelFlowController`, which
tracks `_currentIndex` itself — the door has no reason to know its own index.

### LevelFeatureInstaller.cs

```csharp
using DZ.Core.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public sealed class LevelFeatureInstaller : BaseFeatureInstaller
    {
        [SerializeField] private LevelCatalogSo _catalog;
        [SerializeField] private Transform _levelRoot;
        [SerializeField] private int _startLevelIndex;

        public override void Register(IContainerBuilder builder)
        {
            builder.RegisterInstance(_catalog);
            builder.RegisterEntryPoint<LevelFlowController>()
                   .WithParameter("levelRoot", _levelRoot)
                   .WithParameter("startLevelIndex", _startLevelIndex);
        }
    }
}
```

Add to `RootLifetimeScope.Configure`:

```csharp
[SerializeField] private ScreenFaderView _screenFader;
// ...
builder.RegisterComponent(_screenFader).As<IScreenFader>();
```

---

## 7. Changes to existing scripts

### PlayerCutsceneState.cs (new state, same pattern as PlayerDeadState)

```csharp
using DZ.Core.Contracts;

namespace DZ.Features
{
    /// Inert state: no input subscription, no transitions out on its own.
    /// The flow controller / door drives entry and exit explicitly.
    public sealed class PlayerCutsceneState : BaseState
    {
        public PlayerCutsceneState(PlayerController playerController,
            PlayerAnimationController playerAnimationController,
            PlayerStateMachine playerStateMachine, IInputReader inputReader)
            : base(playerController, playerAnimationController, playerStateMachine, inputReader) { }

        public override void Enter()
        {
            playerController.StopMovingPlayer();
            playerAnimationController.PlayIdleAnimation();
        }

        public override void FixedUpdate() => playerController.StopMovingPlayer();
    }
}
```

### PlayerController additions

```csharp
[SerializeField] private SpriteRenderer _spriteRenderer;
private PlayerCutsceneState _cutsceneState;

public SpriteRenderer SpriteRenderer => _spriteRenderer;
public PlayerCutsceneState CutsceneState => _cutsceneState;

// in CreatePlayerStates():
_cutsceneState = new PlayerCutsceneState(this, playerAnimationController, _playerStateMachine, _inputReader);
_deadState     = new PlayerDeadState(this, playerAnimationController, _playerStateMachine, _inputReader);

public void EnterCutscene() => _playerStateMachine.ChangeState(_cutsceneState);
public void ExitCutscene()  => _playerStateMachine.ChangeState(_idleState);

public void TeleportTo(Vector3 worldPosition)
{
    _playerRb.linearVelocity = Vector2.zero;
    _playerRb.angularVelocity = 0f;
    transform.position = worldPosition;

    // Physics2D.autoSyncTransforms is OFF in this project — without this the
    // ground check queries the old position until the next physics step.
    Physics2D.SyncTransforms();

    _isDead = false;
    RestoreSpriteAlpha();
}

private void RestoreSpriteAlpha()
{
    var c = _spriteRenderer.color;
    c.a = 1f;
    _spriteRenderer.color = c;
}
```

### PlayerDeadState addition

Publish the death signal so the flow controller can reload:

```csharp
// injected or resolved ISignalBus
_signalBus.Publish(new PlayerDiedSignal());
```

`_deadState` is currently **never constructed** in `CreatePlayerStates()`, so
`ChangeState(DeadState)` receives null and no-ops. Fix that at the same time.

---

## 8. Gotchas

**Use `IObjectResolver.Instantiate`, not `Object.Instantiate`.** VContainer does
not inject into prefabs created with `Object.Instantiate`. Every `[Inject]` field
inside the level prefab — including `LevelExitDoor`'s `_signalBus` and `_fader` —
stays null, and the door silently does nothing. This is the single easiest way to
break the whole flow.

**Call `Physics2D.SyncTransforms()` after teleporting.** This project has
`m_AutoSyncTransforms: 0`. Assigning `transform.position` does not update the
physics world until the next `FixedUpdate`, so a ground check running before that
reads the *old* position — the player spawns "airborne" for a frame.

**Wait a frame after instantiating.** The level's `Awake`/`Start` — including
`MoveObstacleAction.Awake` caching `OriginalLocalPosition` — has not run at the
moment `Instantiate` returns. Placing the player before that risks reading a
`SpawnPoint` whose own setup is incomplete.

**Snap the camera while the screen is black.** If the camera lerps to the new
spawn point after the fade-out, the player watches it slide across the level.

**Fade with `useUnscaledTime: true`.** Otherwise a paused or slow-mo game
(hit-stop on death) stalls the fade.

**Restore the player's sprite alpha on spawn.** The door sequence fades it to 0.
Without a reset, level 2 starts with an invisible player.

**Guard the door trigger.** Same `_isRunning` pattern as `ObstacleTrigger` — the
player's collider can generate multiple enters, and each would start a competing
sequence.

**Level prefab roots at scale 1, rotation identity.** `MoveObstacleAction`
computes targets in parent space; a scaled level root rescales every platform's
travel distance.

**Per-level state resets for free.** `ObstacleTrigger` tracks `_triggerCount` and
deactivates itself after `_maxTriggerCount`. Because each level is a fresh
instance, that state resets on reload with no extra work — do not "optimize" this
by pooling level instances without also resetting trigger state.

---

## 9. Build order

1. `IScreenFader` + `ScreenFaderView`, wired into `RootLifetimeScope`. Test with a
   debug key that toggles the fade — nothing else depends on it.
2. `LevelDefinitionSo` + `LevelCatalogSo`, one entry pointing at `Level2.prefab`.
3. `LevelContext` on `Level2`, with a `SpawnPoint` child.
4. `PlayerCutsceneState` + the `PlayerController` additions.
5. `LevelFlowController` + `LevelFeatureInstaller`. At this point the load
   sequence works end to end.
6. `LevelExitDoor` on `DoorExit`, plus the `Close` trigger in the door's Animator.
7. Wire `PlayerDiedSignal` from `PlayerDeadState` for death-reload.

Steps 1–5 give a working loading sequence. The door is additive on top.
