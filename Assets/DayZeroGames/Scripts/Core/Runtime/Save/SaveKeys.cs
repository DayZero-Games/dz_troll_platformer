using UnityEngine;

namespace DZ.Core
{
    public static class SaveKeys
    {
        public const string MusicEnabled = "dz.musicOn";
        public const string SfxEnabled = "dz.sfxOn";
        public const string VibrationEnabled = "dz.vibrationOn";

        public const string CurrentLevel = "dz.currentLevel";
        public const string HighestUnlockedLevel = "dz.highestUnlockedLevel";

        // Local cache of the store entitlement. The store is authoritative; this only
        // exists so the first frame after launch already knows the answer.
        public const string NoAdsPurchased = "dz.noAdsPurchased";
    }
}
