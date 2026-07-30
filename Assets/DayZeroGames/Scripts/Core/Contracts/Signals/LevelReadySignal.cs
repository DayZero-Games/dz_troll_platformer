using UnityEngine;

namespace DZ.Core
{
    public readonly struct LevelReadySignal
    {
        public readonly int LevelIndex;
        public LevelReadySignal(int levelIndex)
        {
            this.LevelIndex = levelIndex;
        }
    }
}
