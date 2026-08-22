using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class ReturnBackAction : LevelAction
    {
        private const float ArrivalDistance = 0.001f;

        [SerializeField, Min(0.01f)] private float _moveSpeed = 10f;

        public override async UniTask ExecuteActionAsync(
            LevelActionContext context,
            CancellationToken cancellation = default)
        {
            await MoveToAsync(context.Transform, context.InitialLocalPosition, cancellation);
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
        public override string Describe() => $"ReturnTo -> start @ {_moveSpeed:0.##}";
#endif
    }
}
