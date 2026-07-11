using UnityEngine;

namespace DZ.Core
{
    public interface IPlayerController
    {
        public bool IsGrounded { get; }
        public bool IsDead { get; }
    }
}
