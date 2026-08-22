using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    public enum LevelActionExecutionMode
    {
        Sequential,
        Parallel
    }

    public readonly struct LevelActionContext
    {
        public LevelActionContext(
            GameObject owner,
            GameObject target,
            Vector3 initialLocalPosition,
            ICameraShaker cameraShaker,
            ILevelRuntimeActions levelRuntimeActions)
        {
            Owner = owner;
            Target = target;
            InitialLocalPosition = initialLocalPosition;
            CameraShaker = cameraShaker;
            LevelRuntimeActions = levelRuntimeActions;
        }

        public GameObject Owner { get; }
        public GameObject Target { get; }
        public Vector3 InitialLocalPosition { get; }
        public ICameraShaker CameraShaker { get; }
        public ILevelRuntimeActions LevelRuntimeActions { get; }

        public bool HasTarget => Target != null;
        public Transform Transform => Target != null ? Target.transform : null;
        public string TargetName => Target != null ? Target.name : OwnerName;
        public string OwnerName => Owner != null ? Owner.name : "Level action";
    }

    [Serializable]
    public abstract class LevelAction
    {
        public virtual bool RequiresTarget => false;

        public abstract UniTask ExecuteActionAsync(
            LevelActionContext context,
            CancellationToken cancellation = default);

#if UNITY_EDITOR
        public virtual string Describe() => GetType().Name;
        public virtual void DrawGizmos(LevelActionContext context) { }
#endif
    }
}
