using UnityEngine;

namespace DZ.Tools
{
    [AddComponentMenu("DayZero/Sprite Combiner")]
    [DisallowMultipleComponent]
    public class SpriteCombiner : MonoBehaviour
    {
        [SerializeField] private SpriteCombineSettings _settings = new SpriteCombineSettings();

        public SpriteCombineSettings Settings => _settings;
    }
}
