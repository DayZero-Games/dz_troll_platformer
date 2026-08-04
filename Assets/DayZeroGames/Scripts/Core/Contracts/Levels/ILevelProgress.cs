using UnityEngine;

namespace DZ.Core
{
    public interface ILevelProgress
    {
        int HighestUnlockedIndex { get; }
        bool IsUnlocked(int index);
        void ResetProgress();
    }
}
