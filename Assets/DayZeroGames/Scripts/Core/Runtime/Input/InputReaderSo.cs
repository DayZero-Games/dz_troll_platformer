using System;
using DZ.Core.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZ.Core.Runtime
{
    [CreateAssetMenu(fileName = "InputReaderSO", menuName = "DayZeroGames/InputReaderSO")]
    public class InputReaderSo : ScriptableObject, IInputReader, GameInputAction.IPlayerActions
    {
        private GameInputAction _inputAction;

        private float _rawMoveInput;
        private bool _inverted;

        public event Action OnJumpPerformed;
        public float moveInput => _inverted ? -_rawMoveInput : _rawMoveInput;
        public bool IsInverted => _inverted;

        private void OnEnable()
        {
            _inputAction ??= new GameInputAction();
            _inputAction.Enable();
            _inputAction.Player.SetCallbacks(this);
            _inverted = false;
        }

        private void OnDisable()
        {
            _inputAction.Disable();
        }

        public void SetInverted(bool inverted) => _inverted = inverted;

        public void OnMove(InputAction.CallbackContext context)
        {
            _rawMoveInput = context.ReadValue<float>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if(context.performed) OnJumpPerformed?.Invoke();
        }
    }
}
