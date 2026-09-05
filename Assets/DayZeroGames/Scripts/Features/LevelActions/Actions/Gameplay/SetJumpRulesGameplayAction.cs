using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class SetJumpRulesGameplayAction : LevelAction
    {
        [Tooltip("Extra jumps allowed while airborne. 0 = normal, 1 = double jump, -1 = unlimited.")]
        [SerializeField] private int _maxAirJumps;

        [Tooltip("0 disables jump impulses. 1 = normal jump force.")]
        [SerializeField] private float _jumpForceMultiplier = 1f;

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

            runtimeActions.SetJumpRules(_maxAirJumps, _jumpForceMultiplier);
            return UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        public override string Describe() =>
            $"Jump Rules -> air {_maxAirJumps}, force {_jumpForceMultiplier:0.##}x";
#endif
    }
}
