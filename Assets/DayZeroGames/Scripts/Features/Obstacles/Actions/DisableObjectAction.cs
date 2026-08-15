using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class DisableObjectAction : ObstacleAction
    {
        public override UniTask ExecuteActionAsync(
            ObstacleActionContext context,
            CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();

            if (context.Performer != null)
            {
                context.Performer.SetActive(false);
            }
                

            return UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        public override string Describe() => "Disable -> performer";
#endif
    }
}
