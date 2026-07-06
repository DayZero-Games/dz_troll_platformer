using UnityEngine;

namespace DZ.Features
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Scriptable Objects/PlayerConfig")]
    public class PlayerConfigSo : ScriptableObject
    {
        public float speed;
        public float jumpForce;
    }
}
