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
    public class LevelActionSequenceController : MonoBehaviour
    {
        [SerializeField] private LevelActionExecutionMode _executionMode = LevelActionExecutionMode.Sequential;
        [SerializeField] private bool _autoStart;
        [SerializeField] private List<LevelActionGroup> _groups = new();

        private ICameraShaker _cameraShaker;
        private ILevelRuntimeActions _levelRuntimeActions;
        private bool _isRunning;
        private bool _hasActivated;

        public bool IsRunning => _isRunning;
        public bool HasActivated => _hasActivated;

        [Inject]
        private void Construct(ICameraShaker cameraShaker, ILevelRuntimeActions levelRuntimeActions)
        {
            _cameraShaker = cameraShaker;
            _levelRuntimeActions = levelRuntimeActions;
        }

        private void Awake()
        {
            CacheInitialStates();

            if (!HasConfiguredActions())
                Debug.LogWarning($"{name}: no level actions assigned.", this);

            WarnAboutMissingRequiredTargets();
        }

        private void Start()
        {
            if (_autoStart)
                TryActivate();
        }

        public bool TryActivate(CancellationToken cancellation = default)
        {
            if (_isRunning || _hasActivated || !HasConfiguredActions()) return false;

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
                if (_executionMode == LevelActionExecutionMode.Sequential)
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

        private void CacheInitialStates()
        {
            if (_groups == null) return;

            foreach (var group in _groups)
                group?.CacheInitialState();
        }

        private bool HasConfiguredActions()
        {
            if (_groups == null) return false;

            foreach (var group in _groups)
            {
                if (group != null && group.HasActions)
                    return true;
            }

            return false;
        }

        private void WarnAboutMissingRequiredTargets()
        {
            if (_groups == null) return;

            for (var i = 0; i < _groups.Count; i++)
            {
                var group = _groups[i];
                if (group == null || !group.IsMissingRequiredTarget) continue;

                var actionNames = group.DescribeTargetRequiredActions();
                Debug.LogWarning(
                    $"{name}: action group {i + 1} has target-required action(s) with no Target assigned: {actionNames}.",
                    this);
            }
        }

        private async UniTask ExecuteSequentiallyAsync(CancellationToken cancellation)
        {
            if (_groups == null) return;

            foreach (var group in _groups)
            {
                if (group == null || !group.HasActions) continue;
                await group.ExecuteSequentiallyAsync(CreateContext(group), cancellation);
            }
        }

        private async UniTask ExecuteInParallelAsync(CancellationToken cancellation)
        {
            if (_groups == null) return;

            var groupTasks = new List<UniTask>(_groups.Count);
            foreach (var group in _groups)
            {
                if (group == null || !group.HasActions) continue;
                groupTasks.Add(group.ExecuteSequentiallyAsync(CreateContext(group), cancellation));
            }

            if (groupTasks.Count > 0)
                await UniTask.WhenAll(groupTasks);
        }

        private LevelActionContext CreateContext(LevelActionGroup group) =>
            group.CreateContext(gameObject, ResolveCameraShaker(), ResolveLevelRuntimeActions());

        private ICameraShaker ResolveCameraShaker()
        {
            if (_cameraShaker != null) return _cameraShaker;

            var lifetimeScope = FindLifetimeScope();
            if (lifetimeScope == null || lifetimeScope.Container == null) return null;

            if (lifetimeScope.Container.TryResolve(typeof(ICameraShaker), out var resolved))
                _cameraShaker = resolved as ICameraShaker;

            return _cameraShaker;
        }

        private ILevelRuntimeActions ResolveLevelRuntimeActions()
        {
            if (_levelRuntimeActions != null) return _levelRuntimeActions;

            var lifetimeScope = FindLifetimeScope();
            if (lifetimeScope == null || lifetimeScope.Container == null) return null;

            if (lifetimeScope.Container.TryResolve(typeof(ILevelRuntimeActions), out var resolved))
                _levelRuntimeActions = resolved as ILevelRuntimeActions;

            return _levelRuntimeActions;
        }

        private LifetimeScope FindLifetimeScope()
        {
            var lifetimeScope = GetComponentInParent<LifetimeScope>();
            if (lifetimeScope == null)
                lifetimeScope = LifetimeScope.Find<LifetimeScope>(gameObject.scene);
            if (lifetimeScope == null)
                lifetimeScope = LifetimeScope.Find<LifetimeScope>();

            return lifetimeScope;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_groups == null) return;

            foreach (var group in _groups)
            {
                if (group == null || !group.HasActions) continue;

                var context = group.CreateContext(gameObject, _cameraShaker, _levelRuntimeActions);
                foreach (var action in group.Actions)
                    action?.DrawGizmos(context);
            }
        }
#endif
    }
}
