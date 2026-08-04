using DZ.Core.Runtime;

namespace DZ.Core.Contracts
{
    public interface IAudioLibrary
    {
        bool TryGet(AudioId audioId, out AudioEntry audioEntry);
    }
}
