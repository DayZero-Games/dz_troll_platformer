using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    /// <summary>
    /// Base for obstacles that move to a marker placed in the scene.
    /// The marker's world position is converted into the mover's parent space,
    /// so this works whether the obstacle is parented or a root object.
    /// </summary>
    public abstract class MoveObstacleAction : ObstacleAction
    {
        [Tooltip("Empty GameObject placed where this object should end up. " +
                 "Must NOT be a child of this object.")]
        [SerializeField] protected Transform _targetPoint;
        [SerializeField] protected float _moveSpeed = 10f;

        protected Vector3 OriginalLocalPosition { get; private set; }

        protected Vector3 TargetLocalPosition => _targetPoint != null ? WorldToParentLocal(_targetPoint.position) : OriginalLocalPosition;

        protected virtual void Awake()
        {
            OriginalLocalPosition = transform.localPosition;

            if (_targetPoint == null)
                Debug.LogError($"{name}: no target point assigned.", this);
            else if (_targetPoint.IsChildOf(transform))
                Debug.LogError($"{name}: target point is a child of the mover — it will move along " +
                               $"and the object will never arrive.", this);
        }
        protected Vector3 WorldToParentLocal(Vector3 worldPosition)
            => transform.parent != null ? transform.parent.InverseTransformPoint(worldPosition) : worldPosition;

        protected async UniTask MoveToAsync(Vector3 localDestination, CancellationToken cancellation)
        {
            while ((transform.localPosition - localDestination).sqrMagnitude > 0.0001f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition, localDestination, _moveSpeed * Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
            }
            transform.localPosition = localDestination;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_targetPoint == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _targetPoint.position);
            Gizmos.DrawWireCube(_targetPoint.position, transform.lossyScale);
        }
#endif
    }
}
