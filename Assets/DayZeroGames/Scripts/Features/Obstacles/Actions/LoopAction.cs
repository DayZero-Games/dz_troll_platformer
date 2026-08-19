using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    /// <summary>
    /// Replays a body of actions. Compose the round trip explicitly - typically
    /// MoveTo -> Wait -> ReturnBack -> Wait - rather than looping a single action.
    /// </summary>
    [Serializable]
    public class LoopAction : ObstacleAction
    {
        [Tooltip("Actions replayed in order on every iteration.")]
        [SerializeReference] private List<ObstacleAction> _actions = new();

        [Tooltip("How many times to run the body. 0 = forever.\n\n" +
                 "An endless loop never returns, so any action placed after it in the SAME performer's list " +
                 "will never run. Give a looping obstacle its own performer entry and set the controller's " +
                 "Execution Mode to Parallel so other performers keep going.")]
        [SerializeField, Min(0)] private int _iterations;

        public override async UniTask ExecuteActionAsync(
            ObstacleActionContext context,
            CancellationToken cancellation = default)
        {
            if (_actions == null || _actions.Count == 0) return;

            for (var iteration = 0; _iterations <= 0 || iteration < _iterations; iteration++)
            {
                foreach (var action in _actions)
                {
                    if (action == null) continue;
                    await action.ExecuteActionAsync(context, cancellation);
                }

                // A body that can complete without yielding - a 0s Wait, or a MoveTo whose performer is
                // already at its target - would otherwise spin forever inside a single frame and hang
                // the editor. This also gives an endless loop a cancellation point every iteration.
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
            }
        }

#if UNITY_EDITOR
        public override string Describe()
        {
            var count = _actions?.Count ?? 0;
            var times = _iterations <= 0 ? "forever" : $"x{_iterations}";

            return count == 0
                ? $"Loop {times} -> empty"
                : $"Loop {times} -> {count} action{(count == 1 ? string.Empty : "s")}";
        }

        public override void DrawGizmos(ObstacleActionContext context)
        {
            if (_actions == null) return;

            foreach (var action in _actions)
                action?.DrawGizmos(context);
        }
#endif
    }
}
