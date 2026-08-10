using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public class MoveToPointAction : ObstacleAction
    {
        private const float ArrivalDistance = 0.001f;

        [Tooltip("Empty GameObject placed where this object should end up.")]
        [SerializeField] private Transform _targetPoint;

        [SerializeField, Min(0.01f)] private float _moveSpeed = 10f;
        [SerializeField, Min(0f)] private float _startDelaySeconds;

        private void Awake()
        {
            if (_targetPoint == null)
            {
                Debug.LogError($"{name}: no target point assigned.", this);
                return;
            }

            if (_targetPoint.IsChildOf(transform))
            {
                Debug.LogError($"{name}: target point is a child of this action object.", this);
            }
        }

        public override async UniTask ExecuteActionAsync(CancellationToken cancellation = default)
        {
            if (_targetPoint == null) return;

            if (_startDelaySeconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(_startDelaySeconds), cancellationToken: cancellation);

            await MoveToAsync(WorldToAttachedParentLocal(_targetPoint.position), cancellation);
        }

        private Vector3 WorldToAttachedParentLocal(Vector3 worldPosition)
        {
            var attachedParent = transform.parent;
            return attachedParent != null ? attachedParent.InverseTransformPoint(worldPosition) : worldPosition;
        }

        private async UniTask MoveToAsync(Vector3 localDestination, CancellationToken cancellation)
        {
            var speed = Mathf.Max(0.01f, _moveSpeed);
            var sqrArrivalDistance = ArrivalDistance * ArrivalDistance;

            while ((transform.localPosition - localDestination).sqrMagnitude > sqrArrivalDistance)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    localDestination,
                    speed * Time.deltaTime);

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
