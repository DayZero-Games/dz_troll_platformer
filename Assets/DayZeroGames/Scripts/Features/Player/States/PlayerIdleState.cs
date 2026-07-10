using System;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    public class PlayerIdleState : BaseState
    {
        public PlayerIdleState(PlayerController playerController, PlayerStateMachine playerStateMachine, IInputReader inputReader) :
        base(playerController, playerStateMachine, inputReader)
        { }

        public override void Enter()
        {
            Debug.Log("IdleState Enter");
            inputReader.OnJumpPerformed += HandlePlayerJump;
            playerController.StopMovingPlayer();
        }

        public override void Update()
        {
            Debug.Log("IdleState Update");
            HandlePlayerIdle();
            CheckForStateChange();
        }

        private void HandlePlayerIdle()
        {
           // Player is idle, no movement logic needed here
        }
        private void HandlePlayerJump()
        {
            if(playerController.IsGrounded)
            {
                playerStateMachine.ChangeState(playerController.JumpState);
            }
        }
        private void CheckForStateChange()
        {
            if (Mathf.Abs(inputReader.moveInput) > 0)
            {
                playerStateMachine.ChangeState(playerController.RunState);
            }
        }

        public override void Exit()
        {
            inputReader.OnJumpPerformed -= HandlePlayerJump;
            Debug.Log("IdleState Exit");
        }


    }
}
