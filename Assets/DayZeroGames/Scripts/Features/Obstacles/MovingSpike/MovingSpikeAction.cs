using System.Threading;
using Cysharp.Threading.Tasks;

namespace DZ.Features
{
    public class MovingSpikeAction : MoveObstacleAction
    {
        public override UniTask ExecuteActionAsync(CancellationToken cancellation = default)
            => MoveToAsync(TargetLocalPosition, cancellation);
    }
}
