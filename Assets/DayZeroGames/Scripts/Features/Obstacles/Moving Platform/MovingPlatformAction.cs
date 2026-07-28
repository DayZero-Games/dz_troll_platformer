using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public class MovingPlatformAction : ObstacleAction
    {
        [SerializeField] private Vector3 _targetLocation;
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _waitAtTargetSeconds = 3f;

        private Vector3 _originalLocation;

        private void Start()
        {
            _originalLocation = transform.position;
        }

        public override async UniTask ExecuteActionAsync(CancellationToken cancellation = default)
        {
            await MoveToAsync(_targetLocation, cancellation);
            await UniTask.Delay(TimeSpan.FromSeconds(_waitAtTargetSeconds), cancellationToken: cancellation);
            await MoveToAsync(_originalLocation, cancellation);
        }

        public async UniTask MoveToAsync(Vector3 destination, CancellationToken cancellation)
        {
            while ((transform.position - destination).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, _moveSpeed * Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
            }
            transform.position = destination;
        }




    }
}
