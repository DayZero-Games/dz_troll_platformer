using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
    public class CameraShakeAction : ObstacleAction
    {
        [SerializeField] private CameraShakeConfigSo _shakeConfig;

        [SerializeField]
        private ShakeSettings _fallbackShake = new ShakeSettings(
            strength: new Vector3(0.08f, 0.12f),
            duration: 0.18f,
            frequency: 22f);

        private ICameraShaker _cameraShaker;

        [Inject]
        private void Construct(ICameraShaker cameraShaker)
        {
            _cameraShaker = cameraShaker;
        }

        public override async UniTask ExecuteActionAsync(CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();

            if (_cameraShaker == null)
            {
                Debug.LogError($"{name}: no camera shaker injected.", this);
                return;
            }

            await _cameraShaker.ShakeAsync(ShakeSettings, cancellation);
        }

        private ShakeSettings ShakeSettings => _shakeConfig != null ? _shakeConfig.Bump : _fallbackShake;
    }
}
