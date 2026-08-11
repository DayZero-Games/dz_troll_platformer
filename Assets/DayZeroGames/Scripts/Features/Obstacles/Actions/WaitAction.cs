using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public class WaitAction : ObstacleAction
    {
        [SerializeField, Min(0f)] private float _durationSeconds = 1f;

        public override UniTask ExecuteActionAsync(CancellationToken cancellation = default)
        {
            if (_durationSeconds <= 0f) return UniTask.CompletedTask;
            return UniTask.Delay(TimeSpan.FromSeconds(_durationSeconds), cancellationToken: cancellation);
        }
    }
}
