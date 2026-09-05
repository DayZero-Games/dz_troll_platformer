using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class SetGravityScaleGameplayAction : LevelAction
    {
        [SerializeField] private float _gravityScale = 1f;

        public override UniTask ExecuteActionAsync(
            LevelActionContext context,
            CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();

            var runtimeActions = context.LevelRuntimeActions;
            if (runtimeActions == null)
            {
                Debug.LogError($"{context.OwnerName}: no level runtime actions service available.", context.Owner);
                return UniTask.CompletedTask;
            }

            runtimeActions.SetGravityScale(_gravityScale);
            return UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        public override string Describe() => $"Gravity -> {_gravityScale:0.##}x";
#endif
    }
}
