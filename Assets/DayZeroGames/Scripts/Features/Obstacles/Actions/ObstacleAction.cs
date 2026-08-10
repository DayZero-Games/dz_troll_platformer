using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public abstract class ObstacleAction : MonoBehaviour
    {
        public abstract UniTask ExecuteActionAsync(CancellationToken cancellation = default);
    }
        
}
