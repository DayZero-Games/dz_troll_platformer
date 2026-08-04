using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace DZ.Features
{
    public class LevelFlowController : IAsyncStartable, IDisposable
    {
        private readonly IScreenFader _fader;
        private readonly ISignalBus _signalBus;
        private readonly IObjectResolver _objectResolver;
        private readonly PlayerController _playerController;
        private readonly LevelCatalogSo _levelCatalogSo;
        private readonly ILevelSelection _levelSelection;
        private readonly Transform _levelRoot;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private GameObject _currentLevel;
        private LevelContext _currentLvlContext;
        private int _currentLvlIndex = -1;
        private bool _isTransitioning;

        public LevelFlowController(
            IScreenFader fader,
            ISignalBus signalBus,
            IObjectResolver objectResolver,
            PlayerController playerController,
            LevelCatalogSo levelCatalogSo,
            ILevelSelection levelSelection,
            Transform levelRoot)
        {
            _fader = fader;
            _signalBus = signalBus;
            _objectResolver = objectResolver;
            _playerController = playerController;
            _levelCatalogSo = levelCatalogSo;
            _levelSelection = levelSelection;
            _levelRoot = levelRoot;
        }

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            _signalBus.Subscribe<RequestNextLevelSignal>(OnNextLevelRequested);
            _signalBus.Subscribe<PlayerDiedSignal>(OnPlayerDied);

            _playerController.LockPlayer();

            var startIndex = _levelSelection.HasSelection ? _levelSelection.SelectedIndex : 0;
            _levelSelection.Clear();

            await LoadLevelAsync(startIndex, _cts.Token);
        }

        private void OnPlayerDied(PlayerDiedSignal signal) => RetryLevelAsync().Forget();
        private void OnNextLevelRequested(RequestNextLevelSignal signal) => AdvanceLevelAsync().Forget();

        private async UniTask LoadLevelAsync(int index, CancellationToken cancellation)
        {
            if (!_levelCatalogSo.HasLevel(index))
            {
                Debug.LogWarning($"No Level at index {index}");
                return;
            }

            if (_isTransitioning) return;
            _isTransitioning = true;

            try
            {
                _signalBus.Publish(new LevelLoadStartedSignal(index));

                await _fader.FadeToBlackAsync(cancellation);

                // Lock before the level is destroyed — a simulating player would fall
                // the moment its ground is unloaded.
                _playerController.LockPlayer();

                UnloadCurrentLevel();

                var definition = _levelCatalogSo.GetLevel(index);
                _currentLevel = _objectResolver.Instantiate(definition.LevelPrefab, _levelRoot);
                _currentLvlContext = _currentLevel.GetComponent<LevelContext>();

                if (_currentLvlContext == null)
                {
                    Debug.LogError($"Level {index} has no {nameof(LevelContext)} component");
                    return;
                }

                _playerController.TeleportTo(_currentLvlContext.SpawnPoint.position);

                // Let the new hierarchy run its Awake/Start before we place the player.
                await UniTask.NextFrame(cancellation);


                _currentLvlIndex = index;
                _signalBus.Publish(new LevelReadySignal(index));

                await _fader.FadeFromBlackAsync(cancellation);

                _playerController.UnlockPlayer();
            }
            catch (OperationCanceledException)
            {
                // Scope torn down mid-transition; the finally below is all the cleanup needed.
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private void UnloadCurrentLevel()
        {
            if (_currentLevel == null) return;
            Object.Destroy(_currentLevel);
            _currentLevel = null;
            _currentLvlContext = null;
        }

        private async UniTaskVoid AdvanceLevelAsync()
        {
            // The door only reports that an exit was reached. Stamping the index is ours,
            // and it has to happen before _currentIndex moves on to the next level.
            _signalBus.Publish(new LevelCompletedSignal(_currentLvlIndex));
            

            var nextLevel = _currentLvlIndex + 1;
            if (!_levelCatalogSo.HasLevel(nextLevel))
            {
                await CompleteGameAsync();
                return;
            }

            await LoadLevelAsync(nextLevel, _cts.Token);
        }

        /// <summary>
        /// The door sequence leaves the player invisible and locked, so we cannot just stop —
        /// that strands the game on a finished level with no way out.
        /// </summary>
        private async UniTask CompleteGameAsync()
        {
            Debug.Log($"All {_levelCatalogSo.Count} levels complete.");

            try
            {
                await _fader.FadeToBlackAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _signalBus.Publish(new GameCompletedSignal());
        }

        private async UniTaskVoid RetryLevelAsync()
        {
            await LoadLevelAsync(_currentLvlIndex, _cts.Token);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<RequestNextLevelSignal>(OnNextLevelRequested);
            _signalBus.Unsubscribe<PlayerDiedSignal>(OnPlayerDied);
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}