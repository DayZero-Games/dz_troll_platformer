using System;
using DZ.Core;
using DZ.Core.Contracts;
using PrimeTween;
using UnityEngine;
using VContainer.Unity;

namespace DZ.Features
{
    /// <summary>
    /// Shakes the camera in response to gameplay signals.
    ///
    /// Plain C# — the container owns the lifetime, so Start/Dispose stand in for
    /// Awake/OnDisable. Nothing holds a direct reference to this; it reacts to the
    /// signal bus, which is why any future death path shakes without extra wiring.
    /// </summary>
    public sealed class CameraShaker : IStartable, IDisposable
    {
        private readonly ISignalBus _signalBus;
        private readonly Transform _shakeTarget;
        private readonly CameraShakeConfigSo _config;

        private Tween _positionTween;
        private Vector3 _baseLocalPosition;
        private bool _isReady;

        public CameraShaker(ISignalBus signalBus, Transform shakeTarget, CameraShakeConfigSo config)
        {
            _signalBus = signalBus;
            _shakeTarget = shakeTarget;
            _config = config;
        }

        public void Start()
        {
            if (_shakeTarget == null || _config == null)
            {
                Debug.LogError($"{nameof(CameraShaker)} is missing a shake target or config — " +
                               $"assign both on {nameof(CameraFeatureInstaller)}. Camera shake is disabled.");
                return;
            }
            _baseLocalPosition = _shakeTarget.localPosition;
            _isReady = true;

            _signalBus.Subscribe<PlayerDiedSignal>(OnPlayerDied);
            _signalBus.Subscribe<LevelExitReachedSignal>(ShakeScreen);
        }

        private void ShakeScreen(LevelExitReachedSignal obj) => Shake();

        public void Dispose()
        {
            if (!_isReady) return;

            _signalBus.Unsubscribe<PlayerDiedSignal>(OnPlayerDied);
            _signalBus.Unsubscribe<LevelExitReachedSignal>(ShakeScreen);
            if (_positionTween.isAlive) _positionTween.Stop();
        }

        private void OnPlayerDied(PlayerDiedSignal signal) => Shake();

        public void Shake()
        {
            if (!_isReady) return;
            if (_positionTween.isAlive) _positionTween.Stop();
            _shakeTarget.localPosition = _baseLocalPosition;

            _positionTween = Tween.ShakeLocalPosition(_shakeTarget, _config.Bump);
        }
    }
}
