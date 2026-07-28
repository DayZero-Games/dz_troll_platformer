using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [RequireComponent(typeof(Collider2D))]
    public class MovingPlatformTrigger : MonoBehaviour
    {
        [SerializeField] private MovingPlatfrom _movingPlatfrom;
        [SerializeField] private int _maxTriggerCount = 2;
        [SerializeField] private string _playerTag = "Player";

        private Collider2D _triggerCollider;
        private int _triggerCount;
        private bool _isWaitingForPlatform;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            _triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isWaitingForPlatform || _triggerCount >= _maxTriggerCount || !other.CompareTag(_playerTag))
            {
                return;
            }

            if (_movingPlatfrom == null)
            {
                return;
            }

            TriggerPlatformAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask TriggerPlatformAsync(CancellationToken cancellationToken)
        {
            _triggerCount++;
            SetTriggerEnabled(false);
            _isWaitingForPlatform = true;

            try
            {
                if (!await _movingPlatfrom.MoveToTargetLocationAsync(cancellationToken))
                {
                    _triggerCount--;
                    SetTriggerEnabled(true);
                    return;
                }

                if (_triggerCount >= _maxTriggerCount)
                {
                    gameObject.SetActive(false);
                    return;
                }

                SetTriggerEnabled(true);
            }
            catch (OperationCanceledException)
            {
                // The trigger was destroyed while the platform was moving.
            }
            finally
            {
                _isWaitingForPlatform = false;
            }
        }

        private void SetTriggerEnabled(bool isEnabled)
        {
            if (_triggerCollider == null)
            {
                return;
            }

            _triggerCollider.enabled = isEnabled;
        }
    }
}
