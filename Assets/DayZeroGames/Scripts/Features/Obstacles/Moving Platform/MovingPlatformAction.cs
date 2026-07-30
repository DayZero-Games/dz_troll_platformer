using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public class MovingPlatformAction : MoveObstacleAction
    {
        [SerializeField] private float _waitAtTargetSeconds = 3f;
        [SerializeField] private bool _shouldReturn = true;

        public override async UniTask ExecuteActionAsync(CancellationToken cancellation = default)
        {
            await MoveToAsync(TargetLocalPosition, cancellation);
            await UniTask.Delay(TimeSpan.FromSeconds(_waitAtTargetSeconds), cancellationToken: cancellation);
            if(_shouldReturn) await MoveToAsync(OriginalLocalPosition, cancellation);
        }
    }
}
