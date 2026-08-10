using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public class DisableObjectAction : ObstacleAction
    {
        public override UniTask ExecuteActionAsync(CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }
    }
}
