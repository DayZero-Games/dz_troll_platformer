using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;

namespace DZ.Core.Contracts
{
    public interface ICameraShaker
    {
        UniTask ShakeAsync(ShakeSettings shakeSettings, CancellationToken cancellation = default);
    }
}
