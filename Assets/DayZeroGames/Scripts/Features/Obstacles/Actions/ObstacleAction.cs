using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    public abstract class ObstacleAction : MonoBehaviour
    {
        public abstract UniTask ExecuteActionAsync(CancellationToken cancellation = default);

#if UNITY_EDITOR
        /// <summary>
        /// One short line describing this step for the controller's inspector, built from live values.
        /// Prefix with ⚠ when the action cannot run as configured.
        /// </summary>
        public virtual string Describe() => GetType().Name;
#endif
    }

}
