using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public class ReturnBackAction : ObstacleAction
    {
        private const float ArrivalDistance = 0.001f;

        [SerializeField, Min(0.01f)] private float _moveSpeed = 10f;

        private Vector3 _originalLocalPosition;

        private void Awake()
        {
            _originalLocalPosition = transform.localPosition;
        }

        public override async UniTask ExecuteActionAsync(CancellationToken cancellation = default)
        {
            await MoveToAsync(_originalLocalPosition, cancellation);
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
        public override string Describe() => $"ReturnTo → start @ {_moveSpeed:0.##}";
#endif
    }
}
