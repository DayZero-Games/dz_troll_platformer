using PrimeTween;
using UnityEngine;

namespace DZ.Features
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "DayZeroGames/Camera Config")]
    public class CameraShakeConfigSo : ScriptableObject
    {
        [Header("Shake Presets")]
        [SerializeField]
        private ShakeSettings _bump = new ShakeSettings(
            strength: new Vector3(0.08f, 0.12f), duration: 0.18f, frequency: 22f);

        public ShakeSettings Bump => _bump;
    }
}
