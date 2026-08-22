using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class ApplyLevelRulesGameplayAction : LevelAction
    {
        [SerializeField] private LevelRules _rules = new LevelRules();

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

            runtimeActions.ApplyRuntimeRules(_rules);
            return UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        public override string Describe()
        {
            var rules = _rules ?? LevelRules.Default;
            return $"Apply Rules -> gravity {rules.GravityScale:0.##}x, jump {rules.JumpForceMultiplier:0.##}x";
        }
#endif
    }
}
