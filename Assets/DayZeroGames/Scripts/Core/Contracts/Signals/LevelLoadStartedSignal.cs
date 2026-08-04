using UnityEngine;

namespace DZ.Core
{
    public readonly struct LevelLoadStartedSignal
    {
        public readonly int LevelIndex;
        public LevelLoadStartedSignal(int levelIndex)
        {
            this.LevelIndex = levelIndex;
        }
    }
}
