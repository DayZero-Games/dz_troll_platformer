using DZ.Core.Contracts;

namespace DZ.Core.Runtime
{
    public interface IAudioLibrary
    {
        bool TryGet(AudioId audioId, out AudioEntry audioEntry);
    }
}
