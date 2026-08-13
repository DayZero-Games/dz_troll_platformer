using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public sealed class ObstaclePerformerActions
    {
        public const string PerformerFieldName = "_performer";
        public const string ActionsFieldName = "_actions";

        [SerializeField] private GameObject _performer;
        [SerializeReference] private List<ObstacleAction> _actions = new();

        [NonSerialized] private Vector3 _initialLocalPosition;

        public GameObject Performer => _performer;
        public List<ObstacleAction> Actions => _actions ??= new List<ObstacleAction>();
        public bool HasActions => _actions != null && _actions.Count > 0;

        public void CacheInitialState()
        {
            if (_performer != null)
                _initialLocalPosition = _performer.transform.localPosition;
        }

        public ObstacleActionContext CreateContext(ICameraShaker cameraShaker)
        {
            var initialLocalPosition = _performer != null
                ? _initialLocalPosition
                : Vector3.zero;

            return new ObstacleActionContext(_performer, initialLocalPosition, cameraShaker);
        }
    }

    public readonly struct ObstacleActionContext
    {
        public ObstacleActionContext(
            GameObject performer,
            Vector3 initialLocalPosition,
            ICameraShaker cameraShaker)
        {
            Performer = performer;
            InitialLocalPosition = initialLocalPosition;
            CameraShaker = cameraShaker;
        }

        public GameObject Performer { get; }
        public Vector3 InitialLocalPosition { get; }
        public ICameraShaker CameraShaker { get; }

        public bool HasPerformer => Performer != null;
        public Transform Transform => Performer != null ? Performer.transform : null;
        public string PerformerName => Performer != null ? Performer.name : "Missing performer";
    }

    [Serializable]
    public abstract class ObstacleAction
    {
        public abstract UniTask ExecuteActionAsync(
            ObstacleActionContext context,
            CancellationToken cancellation = default);

#if UNITY_EDITOR
        public virtual string Describe() => GetType().Name;
        public virtual void DrawGizmos(ObstacleActionContext context) { }
#endif
    }
}
