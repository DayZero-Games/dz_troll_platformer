using UnityEngine;

namespace DZ.Core
{
    public readonly struct LevelCompletedSignal
    {
        public readonly int LevelIndex;
        public LevelCompletedSignal(int levelIndex)
        {
            this.LevelIndex = levelIndex;
        }
    }
}
