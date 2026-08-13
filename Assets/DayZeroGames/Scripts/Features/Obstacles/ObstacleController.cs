using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

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
        [SerializeField] private List<ObstaclePerformerActions> _performers = new();

        private ICameraShaker _cameraShaker;
        private bool _isRunning;
        private bool _hasActivated;

        public bool IsRunning => _isRunning;
        public bool HasActivated => _hasActivated;

        [Inject]
        private void Construct(ICameraShaker cameraShaker)
        {
            _cameraShaker = cameraShaker;
        }

        private void Awake()
        {
            CachePerformerInitialStates();

            if (!HasConfiguredActions())
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
                    _isRunning = false;
            }
        }

        private void CachePerformerInitialStates()
        {
            if (_performers == null) return;

            foreach (var performerActions in _performers)
            {
                performerActions?.CacheInitialState();
            }
        }

        private bool HasConfiguredActions()
        {
            if (_performers == null) return false;

            foreach (var performerActions in _performers)
            {
                if (CanExecute(performerActions))
                    return true;
            }

            return false;
        }

        private async UniTask ExecuteSequentiallyAsync(CancellationToken cancellation)
        {
            if (_performers == null) return;

            foreach (var performerActions in _performers)
            {
                await ExecutePerformerActionsSequentiallyAsync(performerActions, cancellation);
            }
        }

        private async UniTask ExecuteInParallelAsync(CancellationToken cancellation)
        {
            if (_performers == null) return;

            var performerTasks = new List<UniTask>(_performers.Count);
            foreach (var performerActions in _performers)
            {
                if (!CanExecute(performerActions)) continue;
                performerTasks.Add(ExecutePerformerActionsSequentiallyAsync(performerActions, cancellation));
            }

            if (performerTasks.Count > 0)
                await UniTask.WhenAll(performerTasks);
        }

        private async UniTask ExecutePerformerActionsSequentiallyAsync(
            ObstaclePerformerActions performerActions,
            CancellationToken cancellation)
        {
            if (!CanExecute(performerActions)) return;

            var context = performerActions.CreateContext(ResolveCameraShaker());
            foreach (var action in performerActions.Actions)
            {
                if (action != null)
                    await action.ExecuteActionAsync(context, cancellation);
            }
        }

        private static bool CanExecute(ObstaclePerformerActions performerActions)
        {
            return performerActions != null &&
                   performerActions.Performer != null &&
                   performerActions.HasActions;
        }

        private ICameraShaker ResolveCameraShaker()
        {
            if (_cameraShaker != null) return _cameraShaker;

            var lifetimeScope = GetComponentInParent<LifetimeScope>();
            if (lifetimeScope == null)
                lifetimeScope = LifetimeScope.Find<LifetimeScope>(gameObject.scene);
            if (lifetimeScope == null)
                lifetimeScope = LifetimeScope.Find<LifetimeScope>();

            if (lifetimeScope == null || lifetimeScope.Container == null) return null;

            if (lifetimeScope.Container.TryResolve(typeof(ICameraShaker), out var resolved))
                _cameraShaker = resolved as ICameraShaker;

            return _cameraShaker;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_performers == null) return;

            foreach (var performerActions in _performers)
            {
                if (!CanExecute(performerActions)) continue;

                var context = performerActions.CreateContext(_cameraShaker);
                foreach (var action in performerActions.Actions)
                {
                    action?.DrawGizmos(context);
                }
            }
        }
#endif
    }
}
