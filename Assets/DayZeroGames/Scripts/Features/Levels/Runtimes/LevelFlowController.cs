using System;
using System.Collections.Generic;
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
        private readonly IInputReader _inputReader;
        private readonly Transform _levelRoot;
        private readonly IAdService _adService;
        private readonly IAnalyticsService _analyticsService;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private GameObject _currentLevel;
        private LevelContext _currentLvlContext;
        private int _currentLvlIndex = -1;
        private bool _isTransitioning;
        private int _deathCount = 0;
        private int _deathsThisLevel = 0;
        private int _deathsUntilNextAd;

        public LevelFlowController(
            IScreenFader fader,
            ISignalBus signalBus,
            IObjectResolver objectResolver,
            PlayerController playerController,
            LevelCatalogSo levelCatalogSo,
            ILevelSelection levelSelection,
            IInputReader inputReader,
            Transform levelRoot,
            IAdService adService,
            IAnalyticsService analyticsService)
        {
            _fader = fader;
            _signalBus = signalBus;
            _objectResolver = objectResolver;
            _playerController = playerController;
            _levelCatalogSo = levelCatalogSo;
            _levelSelection = levelSelection;
            _inputReader = inputReader;
            _levelRoot = levelRoot;
            _adService = adService;
            _analyticsService = analyticsService;

            _deathsUntilNextAd = RollDeathsUntilNextAd();
        }

        private static int RollDeathsUntilNextAd() =>
            UnityEngine.Random.Range(7, 10 + 1);

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            _signalBus.Subscribe<RequestNextLevelSignal>(OnNextLevelRequested);
            _signalBus.Subscribe<PlayerDiedSignal>(OnPlayerDied);

            _playerController.LockPlayer();

            var startIndex = _levelSelection.HasSelection ? _levelSelection.SelectedIndex : 0;
            _levelSelection.Clear();

            await LoadLevelAsync(startIndex, _cts.Token);
        }

        private void OnPlayerDied(PlayerDiedSignal signal)
        {
            _deathCount++;
            _deathsThisLevel++;
            _analyticsService.LogEvent("player_died", "level_number", _currentLvlIndex+1);

            if (_deathCount >= _deathsUntilNextAd && _adService.IsInterstitialReady())
            {
                _deathCount = 0;
                _deathsUntilNextAd = RollDeathsUntilNextAd();
                HandleDeathWithAdAsync().Forget();
            }
            else
            {
                RetryLevelAsync().Forget();
            }
        }

        private async UniTaskVoid HandleDeathWithAdAsync()
        {
            try
            {
                // Wait a short time to let the player process the death before the ad pops up
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: _cts.Token);
                _adService.ShowInterstitial(() => RetryLevelAsync().Forget());
            }
            catch (OperationCanceledException)
            {
                // Ignore if cancelled
            }
        }
        private void OnNextLevelRequested(RequestNextLevelSignal signal) => AdvanceLevelAsync().Forget();

        private async UniTask LoadLevelAsync(int index, CancellationToken cancellation, bool isRetry = false)
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

                _playerController.LockPlayer();

                UnloadCurrentLevel();

                var definition = _levelCatalogSo.GetLevel(index);

                // Set explicitly on every load (never toggled) so a normal level following
                // an inverted one does not inherit the reversed controls.
                _inputReader.SetInverted(definition.InvertControls);

                _currentLevel = _objectResolver.Instantiate(definition.LevelPrefab, _levelRoot);
                _currentLvlContext = _currentLevel.GetComponent<LevelContext>();

                if (_currentLvlContext == null)
                {
                    Debug.LogError($"Level {index} has no {nameof(LevelContext)} component");
                    return;
                }

                _playerController.TeleportTo(_currentLvlContext.SpawnPoint.position);

                
                await UniTask.NextFrame(cancellation);


                _currentLvlIndex = index;
                _signalBus.Publish(new LevelReadySignal(index));

                if (!isRetry)
                {
                    _deathsThisLevel = 0;
                    _analyticsService.LogEvent("level_started", "level_number", index+1);
                }

                await _fader.FadeFromBlackAsync(cancellation);

                _playerController.UnlockPlayer();
            }
            catch (OperationCanceledException)
            {
                
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
            _signalBus.Publish(new LevelCompletedSignal(_currentLvlIndex));
            
            var parameters = new Dictionary<string, object>
            {
                { "level_number", _currentLvlIndex+1 },
                { "death_count", _deathsThisLevel }
            };
            _analyticsService.LogEvent("level_completed", parameters);

            var nextLevel = _currentLvlIndex + 1;
            if (!_levelCatalogSo.HasLevel(nextLevel))
            {
                await CompleteGameAsync();
                return;
            }

            await LoadLevelAsync(nextLevel, _cts.Token);
        }

        private async UniTask CompleteGameAsync()
        {

            try
            {
                await _fader.FadeToBlackAsync(_cts.Token);
                
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _signalBus.Publish(new GameCompletedSignal());
            _analyticsService.LogEvent("game_completed");
        }

        private async UniTaskVoid RetryLevelAsync()
        {
            await LoadLevelAsync(_currentLvlIndex, _cts.Token, true);
        }

        public void Dispose()
        {
            _inputReader.SetInverted(false);
            _signalBus.Unsubscribe<RequestNextLevelSignal>(OnNextLevelRequested);
            _signalBus.Unsubscribe<PlayerDiedSignal>(OnPlayerDied);
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}