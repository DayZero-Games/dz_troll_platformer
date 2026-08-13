using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class WaitAction : ObstacleAction
    {
        [SerializeField, Min(0f)] private float _durationSeconds = 1f;

        public override UniTask ExecuteActionAsync(
            ObstacleActionContext context,
            CancellationToken cancellation = default)
        {
            if (_durationSeconds <= 0f) return UniTask.CompletedTask;
            return UniTask.Delay(TimeSpan.FromSeconds(_durationSeconds), cancellationToken: cancellation);
        }

#if UNITY_EDITOR
        public override string Describe() =>
            _durationSeconds <= 0f ? "Wait -> 0s (no-op)" : $"Wait -> {_durationSeconds:0.##}s";
#endif
    }
}
