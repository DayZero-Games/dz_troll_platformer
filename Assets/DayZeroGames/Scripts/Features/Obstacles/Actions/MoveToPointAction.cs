using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class MoveToPointAction : LevelAction
    {
        private const float ArrivalDistance = 0.001f;

        [Tooltip("Empty GameObjects to travel through, in order. The performer stops at the last one.")]
        [SerializeField] private Transform[] _targetPoints;

        [SerializeField, Min(0.01f)] private float _moveSpeed = 10f;

        public override async UniTask ExecuteActionAsync(
            LevelActionContext context,
            CancellationToken cancellation = default)
        {
            if (!context.HasTarget) return;
            if (_targetPoints == null || _targetPoints.Length == 0)
            {
                Debug.LogError($"{context.TargetName}: no target points assigned.", context.Target);
                return;
            }

            ValidateTargetPoints(context);

            foreach (var targetPoint in _targetPoints)
            {
                if (targetPoint == null) continue;

                await MoveToAsync(
                    context.Transform,
                    WorldToAttachedParentLocal(context.Transform, targetPoint.position),
                    cancellation);
            }
        }

        private void ValidateTargetPoints(LevelActionContext context)
        {
            var performerTransform = context.Transform;
            foreach (var targetPoint in _targetPoints)
            {
                if (targetPoint == null)
                {
                    Debug.LogError($"{context.TargetName}: a target point is missing.", context.Target);
                    continue;
                }

                if (performerTransform != null && targetPoint.IsChildOf(performerTransform))
                {
                    Debug.LogError(
                        $"{context.TargetName}: target point '{targetPoint.name}' is a child of the action target.",
                        context.Target);
                }
            }
        }

        private static Vector3 WorldToAttachedParentLocal(Transform performerTransform, Vector3 worldPosition)
        {
            var attachedParent = performerTransform != null ? performerTransform.parent : null;
            return attachedParent != null ? attachedParent.InverseTransformPoint(worldPosition) : worldPosition;
        }

        private async UniTask MoveToAsync(
            Transform performerTransform,
            Vector3 localDestination,
            CancellationToken cancellation)
        {
            if (performerTransform == null) return;

            var speed = Mathf.Max(0.01f, _moveSpeed);
            var sqrArrivalDistance = ArrivalDistance * ArrivalDistance;

            while (performerTransform != null &&
                   (performerTransform.localPosition - localDestination).sqrMagnitude > sqrArrivalDistance)
            {
                performerTransform.localPosition = Vector3.MoveTowards(
                    performerTransform.localPosition,
                    localDestination,
                    speed * Time.deltaTime);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
            }

            if (performerTransform != null)
                performerTransform.localPosition = localDestination;
        }

#if UNITY_EDITOR
        public override string Describe()
        {
            if (_targetPoints == null || _targetPoints.Length == 0) return "MoveTo -> no points";

            var route = DescribeRoute(out var hasMissingPoint);
            var warningPrefix = hasMissingPoint ? "Missing: " : string.Empty;

            return $"{warningPrefix}MoveTo -> {route} @ {_moveSpeed:0.##}";
        }

        public override void DrawGizmos(LevelActionContext context)
        {
            if (!context.HasTarget || _targetPoints == null || _targetPoints.Length == 0) return;

            var performerTransform = context.Transform;
            if (performerTransform == null) return;

            Gizmos.color = Color.cyan;
            var legStart = performerTransform.position;

            foreach (var targetPoint in _targetPoints)
            {
                if (targetPoint == null) continue;

                Gizmos.DrawLine(legStart, targetPoint.position);
                Gizmos.DrawWireCube(targetPoint.position, performerTransform.lossyScale);
                legStart = targetPoint.position;
            }
        }

        private string DescribeRoute(out bool hasMissingPoint)
        {
            hasMissingPoint = false;
            foreach (var targetPoint in _targetPoints)
            {
                if (targetPoint == null) hasMissingPoint = true;
            }

            var firstName = NameOf(_targetPoints[0]);
            if (_targetPoints.Length == 1) return firstName;

            var lastName = NameOf(_targetPoints[^1]);
            return _targetPoints.Length == 2
                ? $"{firstName} -> {lastName}"
                : $"{firstName} ... {lastName} ({_targetPoints.Length} pts)";
        }

        private static string NameOf(Transform targetPoint) => targetPoint != null ? targetPoint.name : "missing";
#endif
    }
}
