using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class CameraShakeAction : LevelAction
    {
        [SerializeField] private CameraShakeConfigSo _shakeConfig;

        [SerializeField]
        private ShakeSettings _fallbackShake = new ShakeSettings(
            strength: new Vector3(0.08f, 0.12f),
            duration: 0.18f,
            frequency: 22f);

        public override async UniTask ExecuteActionAsync(
            LevelActionContext context,
            CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();

            if (context.CameraShaker == null)
            {
                Debug.LogError($"{context.TargetName}: no camera shaker available.", context.Target != null ? context.Target : context.Owner);
                return;
            }

            await context.CameraShaker.ShakeAsync(ShakeSettings, cancellation);
        }

        private ShakeSettings ShakeSettings => _shakeConfig != null ? _shakeConfig.Bump : _fallbackShake;

#if UNITY_EDITOR
        public override string Describe() => _shakeConfig != null
            ? $"Shake -> {_shakeConfig.name}"
            : $"Shake -> fallback {_fallbackShake.duration:0.##}s";
#endif
    }
}
