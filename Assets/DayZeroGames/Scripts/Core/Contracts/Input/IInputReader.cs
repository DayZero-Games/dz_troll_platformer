using System;

namespace DZ.Core.Contracts
{
    public interface IInputReader
    {
        public event Action OnJumpPerformed;
        public float moveInput { get;}

        /// <summary>
        /// When true, horizontal input is reversed: pressing left moves the player right.
        /// Owned by the level flow - set it on level load, never from gameplay states.
        /// </summary>
        public bool IsInverted { get; }
        public void SetInverted(bool inverted);
    }
}
