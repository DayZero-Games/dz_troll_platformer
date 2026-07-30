using System;
using System.Threading;
using System.Threading.Tasks;
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
            //subscriber to level completed signal.
            //subscribe to playerDied signal.
            await LoadLevelAsync(_startLevelIndex,cancellation);
        }

        private void OnPlayerDied(PlayerDiedSignal signal) => RetryLevelAsync().Forget();
        private void OnLevelCompleted(LevelCompletedSignal obj) => AdvanceLevelAsync().Forget();

        private async Task LoadLevelAsync(int index, CancellationToken cancellation)
        {
            if (!_levelCatalogSo.HasLevel(index))
            {
                Debug.LogWarning($"No Level at index {index}");
            }

            if (_isTransitioning) return;
            _isTransitioning = true;

            try
            {
                await _fader.FadeToBlackAsync(cancellation);
                UnloadCurrentLevel();

                var definition = _levelCatalogSo.GetLevel(index);
                _currentLevel = _objectResolver.Instantiate(definition.LevelPrefab, _levelRoot);
                _currentContext = _currentLevel.GetComponent<LevelContext>();

                if (_currentContext == null)
                {
                    Debug.LogError($"Level {index} has no {nameof(LevelContext)} component");
                    return;
                }

                await UniTask.NextFrame(cancellation);
               // _playerController.EnterCutScene();
                _playerController.TeleportTo(_currentContext.SpawnPoint.position);
                _currentIndex = index;
                _signalBus.Publish(new LevelReadySignal());
                 await _fader.FadeFromBlackAsync(cancellation);
                // _playerController.ExitCutScene();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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