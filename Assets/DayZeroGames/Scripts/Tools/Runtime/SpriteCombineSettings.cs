using System;
using UnityEngine;

namespace DZ.Tools
{
    /// <summary>
    /// Everything about a bake that a user can change. Kept out of the component so the Sprite Combiner
    /// window can hold a set of its own and both can be drawn by the same GUI.
    /// </summary>
    [Serializable]
    public class SpriteCombineSettings
    {
        public const string DefaultOutputFolder = "Assets/DayZeroGames/Art/Combined";
        public const int DefaultMaxSize = 2048;

        /// <summary>
        /// Whether children that are switched off count as sources. Objects a previous bake switched off
        /// are baked either way - see SpriteCombiner.BakedSources.
        /// </summary>
        public bool IncludeInactiveChildren = true;

        /// <summary>Project folder the combined image is saved to. Must be inside Assets.</summary>
        public string OutputFolder = DefaultOutputFolder;

        /// <summary>Without extension. Empty means "follow the GameObject's name" - see ResolveFileName.</summary>
        public string FileName = string.Empty;

        /// <summary>Point by default: the bake is a 1:1 copy of the sources, so filtering it only blurs it.</summary>
        public FilterMode Filter = FilterMode.Point;

        /// <summary>Largest the image may get. The bake drops its pixel density to fit rather than overrun.</summary>
        public int MaxSize = DefaultMaxSize;

        /// <summary>
        /// The name to write. Nothing typed means the object's own name, so renaming the group renames its
        /// image and the common case needs no input at all.
        /// </summary>
        public string ResolveFileName(string objectName) =>
            string.IsNullOrWhiteSpace(FileName) ? (objectName ?? string.Empty).Trim() : FileName.Trim();

        public void CopyFrom(SpriteCombineSettings other)
        {
            if (other == null) return;

            IncludeInactiveChildren = other.IncludeInactiveChildren;
            OutputFolder = other.OutputFolder;
            FileName = other.FileName;
            Filter = other.Filter;
            MaxSize = other.MaxSize;
        }
    }
}
