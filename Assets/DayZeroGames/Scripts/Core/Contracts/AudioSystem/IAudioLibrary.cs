using DZ.Core.Runtime;
using UnityEngine;

namespace DZ.Core
{
    public interface IAudioLibrary
    {
        bool TryGet(AudioId audioId, out AudioEntry audioEntry);
    }
}
