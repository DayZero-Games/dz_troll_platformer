using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class SetInvertControlsGameplayAction : LevelAction
    {
        [SerializeField] private bool _inverted = true;

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

            runtimeActions.SetInvertControls(_inverted);
            return UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        public override string Describe() => _inverted
            ? "Invert Controls -> on"
            : "Invert Controls -> off";
#endif
    }
}
