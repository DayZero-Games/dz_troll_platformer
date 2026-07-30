using System;
using DZ.Core.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZ.Core.Runtime
{
    [CreateAssetMenu(fileName = "InputReaderSO", menuName = "DayZeroGames/InputReaderSO")]
    public class InputReaderSo : ScriptableObject, IInputReader,GameInputAction.IPlayerActions
    {
        private GameInputAction _inputAction;
        
        public event Action OnJumpPerformed;
        public float moveInput { get; private set; }

        private void OnEnable()
        {
            _inputAction ??= new GameInputAction();
            _inputAction.Enable();
            _inputAction.Player.SetCallbacks(this);
        }

        private void OnDisable()
        {
            _inputAction.Disable();
        }
        
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput =  context.ReadValue<float>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if(context.performed) OnJumpPerformed?.Invoke();
        }

        public void LockInput()
        {
            _inputAction.Disable();
        }



    }
}
