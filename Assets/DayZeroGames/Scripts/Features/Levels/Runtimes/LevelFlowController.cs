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
        private readonly Transform _levelRoot;
        private readonly int _startLevelIndex;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private GameObject _currentLevel;
        private LevelContext _currentContext;
        private int _currentIndex = -1;
        private bool _isTransitioning;

        public LevelFlowController(
            IScreenFader fader,
            ISignalBus signalBus,
            IObjectResolver objectResolver,
            PlayerController playerController,
            LevelCatalogSo levelCatalogSo,
            Transform levelRoot,
            int startLevelIndex)
        {
            _fader = fader;
            _signalBus = signalBus;
            _objectResolver = objectResolver;
            _playerController = playerController;
            _levelCatalogSo = levelCatalogSo;
            _levelRoot = levelRoot;
            _startLevelIndex = startLevelIndex;
        }

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            _signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus.Subscribe<PlayerDiedSignal>(OnPlayerDied);

            // Nothing is loaded yet — don't let the player run around behind the boot fade.
            _playerController.LockPlayer();

            await LoadLevelAsync(_startLevelIndex, _cts.Token);
        }

        private void OnPlayerDied(PlayerDiedSignal signal) => RetryLevelAsync().Forget();
        private void OnLevelCompleted(LevelCompletedSignal obj) => AdvanceLevelAsync().Forget();

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
                _currentContext = _currentLevel.GetComponent<LevelContext>();

                if (_currentContext == null)
                {
                    Debug.LogError($"Level {index} has no {nameof(LevelContext)} component");
                    return;
                }

                _playerController.TeleportTo(_currentContext.SpawnPoint.position);

                // Let the new hierarchy run its Awake/Start before we place the player.
                await UniTask.NextFrame(cancellation);


                _currentIndex = index;
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
            _currentContext = null;
        }

        private async UniTaskVoid AdvanceLevelAsync()
        {
            var nextLevel = _currentIndex + 1;
            if (!_levelCatalogSo.HasLevel(nextLevel))
            {
                Debug.LogWarning($"No Level at index {nextLevel}");
                return;
            }
            await LoadLevelAsync(nextLevel, _cts.Token);
        }

        private async UniTaskVoid RetryLevelAsync()
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