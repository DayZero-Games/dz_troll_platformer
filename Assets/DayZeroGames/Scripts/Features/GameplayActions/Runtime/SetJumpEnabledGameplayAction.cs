using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class SetJumpEnabledGameplayAction : LevelAction
    {
        [SerializeField] private bool _enabled;

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

            runtimeActions.SetJumpEnabled(_enabled);
            return UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        public override string Describe() => _enabled
            ? "Jump -> enabled"
            : "Jump -> disabled";
#endif
    }
}
