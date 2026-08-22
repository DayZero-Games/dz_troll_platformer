using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class SwitchControlGameplayAction : LevelAction
    {
        [SerializeField] private LevelControlTarget _target = LevelControlTarget.Puppet;

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

            if (!runtimeActions.SwitchControl(_target))
            {
                Debug.LogWarning(
                    $"{context.OwnerName}: could not switch control to {_target}.",
                    context.Owner);
            }

            return UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        public override string Describe() => $"Switch Control -> {_target}";
#endif
    }
}
