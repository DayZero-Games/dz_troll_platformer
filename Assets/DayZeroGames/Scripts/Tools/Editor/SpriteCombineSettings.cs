using System;
using UnityEditor;
using UnityEngine;

namespace DZ.Tools
{
    [Serializable]
    public class SpriteCombineSettings
    {
        public const string DefaultOutputFolder = "Assets/DayZeroGames/Art/Combined";
        public const int DefaultMaxSize = 2048;

        public string OutputFolder = DefaultOutputFolder;
        public string FileName = string.Empty;
        public FilterMode Filter = FilterMode.Point;
        public int MaxSize = DefaultMaxSize;
        public TextureImporterCompression Compression = TextureImporterCompression.Uncompressed;
        public bool IncludeInactiveChildren = false;
        public bool GenerateCollider = false;
        public bool DeactivateCombinedChildren = true;
        public bool DestroyCombinedChildren = false;

        public string ResolveFileName(string objectName) =>
            string.IsNullOrWhiteSpace(FileName) ? (objectName ?? string.Empty).Trim() : FileName.Trim();
    }
}
