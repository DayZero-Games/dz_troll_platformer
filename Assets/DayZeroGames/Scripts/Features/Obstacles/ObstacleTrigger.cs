using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [RequireComponent(typeof(Collider2D))]
    public class ObstacleTrigger : MonoBehaviour
    {
        [SerializeField] private ObstacleAction[] _obstacleActions;
        [SerializeField] private int _maxTriggerCount = 1;
        [SerializeField] private string _playerTag = "Player";

        private int _triggerCount;
        private bool _isRunning;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isRunning || _triggerCount >= _maxTriggerCount || !other.CompareTag(_playerTag)) return;
            ExecuteObstacleActionsAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask ExecuteObstacleActionsAsync(CancellationToken cancellation)
        {
            _isRunning = true;
            _triggerCount++;

            try
            {
                foreach (var action in _obstacleActions)
                {
                    if (action != null) await action.ExecuteActionAsync(cancellation);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("Obstacle actions execution was canceled.");
            }
            finally
            {
                _isRunning = false;
                if (_triggerCount >= _maxTriggerCount)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
