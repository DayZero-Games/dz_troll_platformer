using System;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    public class PlayerJumpState : BaseState
    {
        public PlayerJumpState(PlayerController playerController, PlayerStateMachine playerStateMachine, IInputReader inputReader) : base(playerController, playerStateMachine, inputReader)
        {
        }

        public override void Enter()
        {
            Debug.Log("JumpState Enter");
            HandlePlayerJump();
        }
        public override void Update()
        {
            
            CheckForStateChange();
        }
        public override void FixedUpdate()
        {
            playerController.MovePlayer(inputReader.moveInput);
        }
        public override void Exit()
        {
            Debug.Log("JumpState Exit");
        }

        private void CheckForStateChange()
        {
            if (!playerController.IsGrounded) return;
            if (Mathf.Abs(inputReader.moveInput) > 0) playerStateMachine.ChangeState(playerController.RunState);
            else playerStateMachine.ChangeState(playerController.IdleState);
        }

        private void HandlePlayerJump()
        {
            playerController.Jump();
        }

    }
}
