using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class LevelLoopAction : LevelAction
    {
        [Tooltip("Actions replayed in order on every iteration.")]
        [SerializeReference] private List<LevelAction> _actions = new();

        [Tooltip("How many times to run the body. 0 = forever.")]
        [SerializeField, Min(0)] private int _iterations;

        public override bool RequiresTarget
        {
            get
            {
                if (_actions == null) return false;

                foreach (var action in _actions)
                {
                    if (action != null && action.RequiresTarget)
                        return true;
                }

                return false;
            }
        }

        public override async UniTask ExecuteActionAsync(
            LevelActionContext context,
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

        public override void DrawGizmos(LevelActionContext context)
        {
            if (_actions == null) return;

            foreach (var action in _actions)
                action?.DrawGizmos(context);
        }
#endif
    }
}
