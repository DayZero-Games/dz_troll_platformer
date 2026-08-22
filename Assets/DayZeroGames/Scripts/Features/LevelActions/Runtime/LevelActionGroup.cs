using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public sealed class LevelActionGroup
    {
        public const string TargetFieldName = "_target";
        public const string ActionsFieldName = "_actions";

        [Tooltip("Optional. Assign for actions that operate on a specific object. Leave empty for global gameplay actions.")]
        [SerializeField] private GameObject _target;

        [SerializeReference] private List<LevelAction> _actions = new();

        [NonSerialized] private Vector3 _initialLocalPosition;

        public GameObject Target => _target;
        public List<LevelAction> Actions => _actions ??= new List<LevelAction>();
        public bool HasActions => _actions != null && _actions.Count > 0;
        public bool RequiresTarget => HasActionRequiringTarget();
        public bool IsMissingRequiredTarget => _target == null && RequiresTarget;

        public void CacheInitialState()
        {
            if (_target != null)
                _initialLocalPosition = _target.transform.localPosition;
        }

        public LevelActionContext CreateContext(
            GameObject owner,
            ICameraShaker cameraShaker,
            ILevelRuntimeActions levelRuntimeActions)
        {
            var initialLocalPosition = _target != null
                ? _initialLocalPosition
                : Vector3.zero;

            return new LevelActionContext(
                owner,
                _target,
                initialLocalPosition,
                cameraShaker,
                levelRuntimeActions);
        }

        public async UniTask ExecuteSequentiallyAsync(
            LevelActionContext context,
            System.Threading.CancellationToken cancellation)
        {
            foreach (var action in Actions)
            {
                if (action != null)
                    await action.ExecuteActionAsync(context, cancellation);
            }
        }

        public string DescribeTargetRequiredActions()
        {
            if (_actions == null) return string.Empty;

            var actionNames = new List<string>();
            foreach (var action in _actions)
            {
                if (action == null || !action.RequiresTarget) continue;
                actionNames.Add(action.GetType().Name);
            }

            return string.Join(", ", actionNames);
        }

        private bool HasActionRequiringTarget()
        {
            if (_actions == null) return false;

            foreach (var action in _actions)
            {
                if (action != null && action.RequiresTarget)
                    return true;
            }

            return false;
        }
    }
}
