using System;

namespace DZ.Core.Contracts
{
    public interface IInputReader
    {
        public event Action OnJumpPerformed;
        public float moveInput { get;}
    }
}
