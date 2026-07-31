using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    public class PlayerLockedState : BaseState
    {
        public PlayerLockedState(PlayerController playerController, PlayerAnimationController playerAnimationController,
            PlayerStateMachine playerStateMachine, IInputReader inputReader) :
            base(playerController, playerAnimationController, playerStateMachine, inputReader)
        {
        }

        public override void Enter()
        {
            var playerRb = playerController.PlayerRb;
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerAnimationController.PlayIdleAnimation();
        }

        public override void Exit()
        {
        }
    }
}
