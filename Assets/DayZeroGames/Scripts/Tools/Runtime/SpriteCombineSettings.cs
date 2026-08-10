using System;
using UnityEngine;

namespace DZ.Tools
{
    public enum SpriteCombineCompression
    {
        None,
        LowQuality,
        NormalQuality,
        HighQuality,
    }

    [Serializable]
    public class SpriteCombineSettings
    {
        public const string DefaultOutputFolder = "Assets/DayZeroGames/Art/Combined";
        public const int DefaultMaxSize = 2048;
        public bool IncludeInactiveChildren = false;
        public bool DeactivateCombinedChildren = true;
        public bool DestroyCombinedChildren = false;
        public string OutputFolder = DefaultOutputFolder;
        public string FileName = string.Empty;
        public FilterMode Filter = FilterMode.Point;
        public int MaxSize = DefaultMaxSize;
        public SpriteCombineCompression Compression = SpriteCombineCompression.None;
        public string ResolveFileName(string objectName) =>
            string.IsNullOrWhiteSpace(FileName) ? (objectName ?? string.Empty).Trim() : FileName.Trim();

        public void CopyFrom(SpriteCombineSettings other)
        {
            if (other == null) return;

            IncludeInactiveChildren = other.IncludeInactiveChildren;
            DeactivateCombinedChildren = other.DeactivateCombinedChildren;
            DestroyCombinedChildren = other.DestroyCombinedChildren;
            OutputFolder = other.OutputFolder;
            FileName = other.FileName;
            Filter = other.Filter;
            MaxSize = other.MaxSize;
            Compression = other.Compression;
        }
    }
}
