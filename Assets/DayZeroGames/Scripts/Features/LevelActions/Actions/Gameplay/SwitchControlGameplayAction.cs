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
        public const string TargetFieldName = "_target";
        public const string PuppetIdFieldName = "_puppetId";

        [SerializeField] private LevelControlTarget _target = LevelControlTarget.Puppet;
        [SerializeField] private string _puppetId;

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

            if (!runtimeActions.SwitchControl(_target, _puppetId))
            {
                var targetDescription = _target == LevelControlTarget.Puppet &&
                                        !string.IsNullOrWhiteSpace(_puppetId)
                    ? $"{_target} '{_puppetId}'"
                    : _target.ToString();

                Debug.LogWarning(
                    $"{context.OwnerName}: could not switch control to {targetDescription}.",
                    context.Owner);
            }

            return UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        public override string Describe()
        {
            if (_target != LevelControlTarget.Puppet || string.IsNullOrWhiteSpace(_puppetId))
                return $"Switch Control -> {_target}";

            return $"Switch Control -> Puppet ({_puppetId})";
        }
#endif
    }
}
