using DZ.Core.Runtime;

namespace DZ.Core
{
    public interface IAudioLibrary
    {
        bool TryGet(AudioId audioId, out AudioEntry audioEntry);
    }
}
