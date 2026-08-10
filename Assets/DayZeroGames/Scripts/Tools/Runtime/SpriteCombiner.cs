using System.Collections.Generic;
using UnityEngine;

namespace DZ.Tools
{
    /// <summary>
    /// Put this on the parent of a group of static sprites and press Combine in the inspector. Every
    /// SpriteRenderer underneath is flattened into one image, saved to the output folder, and drawn by a
    /// single SpriteRenderer on this object - so the group costs one draw call instead of one per sprite.
    /// The flattened children are switched off, and this object's origin moves to the centre of the image.
    ///
    /// Does nothing at runtime; it only holds the settings. The bake lives in DZ.Tools.Editor.
    /// </summary>
    [AddComponentMenu("DayZero/Sprite Combiner")]
    [DisallowMultipleComponent]
    public class SpriteCombiner : MonoBehaviour
    {
        // Drawn by SpriteCombinerEditor rather than the default inspector, so the fields can carry a folder
        // picker and a live file name.
        [SerializeField] private SpriteCombineSettings _settings = new SpriteCombineSettings();

        // The image produced by the last Combine. Kept so a re-bake knows which asset it already owns;
        // hidden because it is bookkeeping, not a setting.
        [HideInInspector]
        [SerializeField] private Sprite _bakedSprite;

        // The objects the last Combine switched off. They stay eligible for a re-bake even when inactive
        // children are otherwise excluded - without this, baking once would make the group un-bakeable.
        [HideInInspector]
        [SerializeField] private List<GameObject> _bakedSources = new List<GameObject>();

        public SpriteCombineSettings Settings => _settings;

        public IReadOnlyList<GameObject> BakedSources => _bakedSources;

        public void SetBakedSources(List<GameObject> sources)
        {
            _bakedSources.Clear();
            if (sources != null) _bakedSources.AddRange(sources);
        }

        /// <summary>Name of the image this object bakes to, without extension.</summary>
        public string ResolvedFileName => _settings.ResolveFileName(name);

        public Sprite BakedSprite
        {
            get => _bakedSprite;
            set => _bakedSprite = value;
        }
    }
}
