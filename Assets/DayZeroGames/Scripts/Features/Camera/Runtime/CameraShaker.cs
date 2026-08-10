using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core;
using DZ.Core.Contracts;
using PrimeTween;
using UnityEngine;
using VContainer.Unity;

namespace DZ.Features
{
    public sealed class CameraShaker : ICameraShaker, IStartable, IDisposable
    {
        private readonly ISignalBus _signalBus;
        private readonly Transform _shakeTarget;
        private readonly CameraShakeConfigSo _config;

        private Tween _positionTween;
        private Vector3 _baseLocalPosition;
        private bool _isReady;
        private int _shakeVersion;

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
            _shakeVersion++;
            StopActiveShake(false);
        }

        private void OnPlayerDied(PlayerDiedSignal signal) => Shake();

        public async UniTask ShakeAsync(ShakeSettings shakeSettings, CancellationToken cancellation = default)
        {
            if (!_isReady) return;
            cancellation.ThrowIfCancellationRequested();

            var shakeVersion = ++_shakeVersion;
            StopActiveShake(true);
            _positionTween = Tween.ShakeLocalPosition(_shakeTarget, shakeSettings);

            try
            {
                await _positionTween.ToUniTask().AttachExternalCancellation(cancellation);
            }
            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
            {
                if (shakeVersion == _shakeVersion) StopActiveShake(true);
            }
            catch (OperationCanceledException)
            {
                if (shakeVersion == _shakeVersion) StopActiveShake(true);
                throw;
            }
            finally
            {
                if (shakeVersion == _shakeVersion) StopActiveShake(true);
            }
        }

        private void Shake()
        {
            if (_config == null) return;
            ShakeAsync(_config.Bump).Forget();
        }

        private void StopActiveShake(bool restorePosition)
        {
            if (_positionTween.isAlive) _positionTween.Stop();

            if (restorePosition && _shakeTarget != null)
            {
                _shakeTarget.localPosition = _baseLocalPosition;
            }
        }
    }
}
