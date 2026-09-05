using UnityEngine;

namespace DZ.Core
{
//per level rules so that every level feels unique and separate.
    [System.Serializable]
    public sealed class LevelRules
    {
        [Tooltip("Reverses horizontal input: pressing left moves the player right.")] [SerializeField]
        private bool _invertControls;

        [Tooltip("Multiplies the player's Rigidbody2D gravity. 1 = normal, 0.35 = moon.")] [SerializeField]
        private float _gravityScale = 1f;

        [Tooltip("Extra jumps allowed while airborne.\n0 = normal, 1 = double jump, -1 = unlimited (flappy bird).")]
        [SerializeField]
        private int _maxAirJumps;

        [Tooltip("Use it to cancel out the extra jump height low gravity gives.")] [SerializeField]
        private float _jumpForceMultiplier = 1f;

        public bool InvertControls => _invertControls;
        public float GravityScale => _gravityScale;
        public int MaxAirJumps => _maxAirJumps;
        public float JumpForceMultiplier => _jumpForceMultiplier;

        public static LevelRules Default => new LevelRules();
    }
}