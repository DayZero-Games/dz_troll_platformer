using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public class MovingPlatfrom : MonoBehaviour
    {
        private Vector3 _originalLocation;
        [SerializeField] private Vector3 _targetLocation;
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _waitAtTargetSeconds = 3f;
        private bool _isMoving;

        private void Start()
        {
            _originalLocation = transform.position;
        }

        private void OnValidate()
        {
            _moveSpeed = Mathf.Max(0.01f, _moveSpeed);
            _waitAtTargetSeconds = Mathf.Max(0f, _waitAtTargetSeconds);
        }

        public async UniTask<bool> MoveToTargetLocationAsync(CancellationToken cancellationToken = default)
        {
            if (_isMoving)
            {
                return false;
            }

            _isMoving = true;

            try
            {
                await MoveToLocationAsync(_targetLocation, cancellationToken);
                await UniTask.Delay(TimeSpan.FromSeconds(_waitAtTargetSeconds), cancellationToken: cancellationToken);
                await MoveToLocationAsync(_originalLocation, cancellationToken);
                return true;
            }
            finally
            {
                _isMoving = false;
            }
        }

        private async UniTask MoveToLocationAsync(Vector3 destination, CancellationToken cancellationToken)
        {
            while ((transform.position - destination).sqrMagnitude > 0.0001f)
            {
                cancellationToken.ThrowIfCancellationRequested();
                transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * _moveSpeed);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            transform.position = destination;
        }
    }
}
