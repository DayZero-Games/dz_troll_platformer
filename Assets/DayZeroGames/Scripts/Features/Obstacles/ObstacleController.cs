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

            var performerOrder = new List<GameObject>();
            var actionsByPerformer = new Dictionary<GameObject, List<ObstacleAction>>();

            foreach (var action in _obstacleActions)
            {
                if (action == null) continue;

                var performer = action.gameObject;
                if (performer == null) continue;

                if (!actionsByPerformer.TryGetValue(performer, out var performerActions))
                {
                    performerActions = new List<ObstacleAction>();
                    actionsByPerformer.Add(performer, performerActions);
                    performerOrder.Add(performer);
                }

                performerActions.Add(action);
            }

            var performerTasks = new List<UniTask>(performerOrder.Count);
            foreach (var performer in performerOrder)
            {
                performerTasks.Add(ExecutePerformerActionsSequentiallyAsync(
                    actionsByPerformer[performer],
                    cancellation));
            }

            if (performerTasks.Count > 0)
                await UniTask.WhenAll(performerTasks);
        }

        private static async UniTask ExecutePerformerActionsSequentiallyAsync(
            List<ObstacleAction> actions,
            CancellationToken cancellation)
        {
            foreach (var action in actions)
            {
                if (action != null) await action.ExecuteActionAsync(cancellation);
            }
        }
    }
}
