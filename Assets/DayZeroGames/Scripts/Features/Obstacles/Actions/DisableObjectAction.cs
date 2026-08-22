using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public class DisableObjectAction : LevelAction
    {
        public override UniTask ExecuteActionAsync(
            LevelActionContext context,
            CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();

            if (context.Target != null)
                context.Target.SetActive(false);

            return UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        public override string Describe() => "Disable -> target";
#endif
    }
}
