using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public class MoveToPointAction : ObstacleAction
    {
        private const float ArrivalDistance = 0.001f;

        [Tooltip("Empty GameObjects to travel through, in order. The object stops at the last one.")]
        [SerializeField] private Transform[] _targetPoints;

        [SerializeField, Min(0.01f)] private float _moveSpeed = 10f;
        [SerializeField, Min(0f)] private float _startDelaySeconds;

        private void Awake()
        {
            if (_targetPoints == null || _targetPoints.Length == 0)
            {
                Debug.LogError($"{name}: no target points assigned.", this);
                return;
            }

            foreach (var targetPoint in _targetPoints)
            {
                if (targetPoint == null)
                {
                    Debug.LogError($"{name}: a target point is missing.", this);
                    continue;
                }

                if (targetPoint.IsChildOf(transform))
                {
                    Debug.LogError(
                        $"{name}: target point '{targetPoint.name}' is a child of this action object.", this);
                }
            }
        }

        public override async UniTask ExecuteActionAsync(CancellationToken cancellation = default)
        {
            if (_targetPoints == null || _targetPoints.Length == 0) return;

            if (_startDelaySeconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(_startDelaySeconds), cancellationToken: cancellation);

            foreach (var targetPoint in _targetPoints)
            {
                if (targetPoint == null) continue;
                
                await MoveToAsync(WorldToAttachedParentLocal(targetPoint.position), cancellation);
            }
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
            if (_targetPoints == null || _targetPoints.Length == 0) return;

            Gizmos.color = Color.cyan;
            var legStart = transform.position;

            foreach (var targetPoint in _targetPoints)
            {
                if (targetPoint == null) continue;

                Gizmos.DrawLine(legStart, targetPoint.position);
                Gizmos.DrawWireCube(targetPoint.position, transform.lossyScale);
                legStart = targetPoint.position;
            }
        }
#endif
    }
}
