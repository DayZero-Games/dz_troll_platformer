using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public class ObstacleController : MonoBehaviour
    {
        private enum ActionExecutionMode
        {
            Sequential,
            Parallel
        }

        [SerializeField] private ActionExecutionMode _executionMode = ActionExecutionMode.Sequential;
        [SerializeField] private ObstacleAction[] _obstacleActions;

        private bool _isRunning;
        private bool _hasActivated;

        public bool IsRunning => _isRunning;
        public bool HasActivated => _hasActivated;

        private void Awake()
        {
            if (_obstacleActions == null || _obstacleActions.Length == 0)
                Debug.LogWarning($"{name}: no obstacle actions assigned.", this);
        }

        public bool TryActivate(CancellationToken cancellation = default)
        {
            if (_isRunning || _hasActivated) return false;

            _hasActivated = true;
            var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellation,
                this.GetCancellationTokenOnDestroy());

            ExecuteActionsAsync(linkedCancellation).Forget();
            return true;
        }

        private async UniTask ExecuteActionsAsync(CancellationTokenSource cancellationSource)
        {
            _isRunning = true;

            try
            {
                var cancellation = cancellationSource.Token;
                if (_executionMode == ActionExecutionMode.Sequential)
                    await ExecuteSequentiallyAsync(cancellation);
                else
                    await ExecuteInParallelAsync(cancellation);
            }
            catch (OperationCanceledException)
            {
                
            }
            finally
            {
                cancellationSource.Dispose();

                if (this != null)
                {
                    _isRunning = false;
                }
            }
        }

        private async UniTask ExecuteSequentiallyAsync(CancellationToken cancellation)
        {
            if (_obstacleActions == null) return;

            foreach (var action in _obstacleActions)
            {
                if (action != null) await action.ExecuteActionAsync(cancellation);
            }
        }

        private async UniTask ExecuteInParallelAsync(CancellationToken cancellation)
        {
            if (_obstacleActions == null) return;

            var actionTasks = new List<UniTask>(_obstacleActions.Length);
            foreach (var action in _obstacleActions)
            {
                if (action != null) actionTasks.Add(action.ExecuteActionAsync(cancellation));
            }

            await UniTask.WhenAll(actionTasks);
        }
    }
}
