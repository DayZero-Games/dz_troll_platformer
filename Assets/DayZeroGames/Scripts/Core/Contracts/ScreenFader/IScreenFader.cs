using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Core
{
    public interface IScreenFader
    {
        bool IsCovered{get;}
        UniTask FadeToBlackAsync(CancellationToken cancellationToken=default);
        UniTask FadeFromBlackAsync(CancellationToken cancellationToken=default);
        void SetCoveredImmediately(bool covered);
    }
}
