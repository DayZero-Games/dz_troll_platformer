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
    public class LevelFlowController : IAsyncStartable, IDisposable, ILevelRuntimeActions
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
            _signalBus.Subscribe<LevelExitReachedSignal>(OnLevelExitReached);

            _playerController.LockPlayer();

            var startIndex = _levelSelection.HasSelection ? _levelSelection.SelectedIndex : 0;
            _levelSelection.Clear();

            await LoadLevelAsync(startIndex, _cts.Token);
        }

        private void OnPlayerDied(PlayerDiedSignal signal)
        {
            LockAllPuppets();

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
        /// <summary>
        /// The exit door only locks the avatar that actually touched it, which is not enough in a
        /// puppet level: if the door reaches the frozen player instead of the puppet, the puppet
        /// would stay live and keep taking input all the way through the walk-in and fade.
        /// </summary>
        private void OnLevelExitReached(LevelExitReachedSignal signal) => LockAllPuppets();

        /// Whoever is being controlled, the level is over for them - freeze the puppet too.
        /// Safe to call on an already-locked puppet.
        private void LockAllPuppets()
        {
            if (_currentLvlContext == null) return;

            foreach (var puppet in _currentLvlContext.GetPuppets())
                puppet.Lock();
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

                // Applied wholesale on every load (never toggled) so a level can never
                // inherit the previous level's rules.
                var rules = definition.Rules;
                _inputReader.SetInverted(rules.InvertControls);
                _playerController.ApplyRules(rules);

                _currentLevel = _objectResolver.Instantiate(definition.LevelPrefab, _levelRoot);
                _currentLvlContext = _currentLevel.GetComponent<LevelContext>();

                if (_currentLvlContext == null)
                {
                    Debug.LogError($"Level {index} has no {nameof(LevelContext)} component");
                    return;
                }

                _playerController.TeleportTo(_currentLvlContext.SpawnPoint.position);

                // A puppet level drives one of the level's own objects instead of the player,
                // so the same physics rules have to reach it. It spawns locked with the level.
                ApplyRulesToPuppets(rules);


                await UniTask.NextFrame(cancellation);


                _currentLvlIndex = index;
                _signalBus.Publish(new LevelReadySignal(index));

                if (!isRetry)
                {
                    _deathsThisLevel = 0;
                    _analyticsService.LogEvent("level_started", "level_number", index+1);
                }

                await _fader.FadeFromBlackAsync(cancellation);

                // In a puppet level the player stays frozen at the spawn point and the
                // puppet takes the input for the whole level.
                SwitchControl(
                    _currentLvlContext.StartControlTarget,
                    _currentLvlContext.StartPuppetId);
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
            if (_playerController != null) _playerController.ApplyRules(LevelRules.Default);
            _signalBus.Unsubscribe<RequestNextLevelSignal>(OnNextLevelRequested);
            _signalBus.Unsubscribe<PlayerDiedSignal>(OnPlayerDied);
            _signalBus.Unsubscribe<LevelExitReachedSignal>(OnLevelExitReached);
            _cts.Cancel();
            _cts.Dispose();
        }

        public void SetInvertControls(bool inverted)
        {
            _inputReader.SetInverted(inverted);
        }

        public bool FlipInvertControls()
        {
            var inverted = !_inputReader.IsInverted;
            SetInvertControls(inverted);
            return inverted;
        }

        public void SetGravityScale(float gravityScale)
        {
            _playerController.SetGravityScale(gravityScale);
            if (_currentLvlContext == null) return;

            foreach (var puppet in _currentLvlContext.GetPuppets())
                puppet.SetGravityScale(gravityScale);
        }

        public void SetJumpRules(int maxAirJumps, float jumpForceMultiplier)
        {
            _playerController.SetJumpRules(maxAirJumps, jumpForceMultiplier);
            if (_currentLvlContext == null) return;

            foreach (var puppet in _currentLvlContext.GetPuppets())
                puppet.SetJumpRules(maxAirJumps, jumpForceMultiplier);
        }

        public void SetJumpEnabled(bool enabled)
        {
            _playerController.SetJumpEnabled(enabled);
            if (_currentLvlContext == null) return;

            foreach (var puppet in _currentLvlContext.GetPuppets())
                puppet.SetJumpEnabled(enabled);
        }

        public void ApplyRuntimeRules(LevelRules rules)
        {
            rules ??= LevelRules.Default;

            SetInvertControls(rules.InvertControls);
            _playerController.ApplyRules(rules);

            ApplyRulesToPuppets(rules);
        }

        public void RestoreCatalogRules()
        {
            var rules = _levelCatalogSo.HasLevel(_currentLvlIndex)
                ? _levelCatalogSo.GetLevel(_currentLvlIndex).Rules
                : LevelRules.Default;

            ApplyRuntimeRules(rules);
        }

        public bool SwitchControl(LevelControlTarget target, string puppetId = null)
        {
            if (_currentLvlContext == null) return false;

            switch (target)
            {
                case LevelControlTarget.Player:
                    LockAllPuppets();
                    _playerController.UnlockPlayer();
                    return true;

                case LevelControlTarget.Puppet:
                    if (!_currentLvlContext.TryGetPuppet(puppetId, out var puppet))
                    {
                        var puppetDescription = string.IsNullOrWhiteSpace(puppetId)
                            ? "the first puppet"
                            : $"puppet '{puppetId}'";

                        Debug.LogWarning($"Cannot switch control to {puppetDescription}: current level has no matching puppet.");
                        return false;
                    }

                    _playerController.LockPlayer();
                    LockAllPuppets();
                    puppet.Unlock();
                    return true;

                default:
                    return false;
            }
        }

        private void ApplyRulesToPuppets(LevelRules rules)
        {
            if (_currentLvlContext == null) return;

            foreach (var puppet in _currentLvlContext.GetPuppets())
                puppet.ApplyRules(rules);
        }
    }
}
