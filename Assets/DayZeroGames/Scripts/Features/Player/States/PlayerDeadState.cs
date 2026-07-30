using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    public class PlayerDeadState:BaseState
    {
        public PlayerDeadState(PlayerController playerController, PlayerAnimationController playerAnimationController, PlayerStateMachine playerStateMachine, IInputReader inputReader) :
         base(playerController, playerAnimationController, playerStateMachine, inputReader)
        {
        }

        public override void Enter()
        {
            Debug.Log("Entered PlayerDeadState");
            playerAnimationController.PlayDeadAnimation();
            playerController.PlayerRb.linearVelocityX = 0f;
            playerController.Die();
            

        }

        public override void Exit()
        {
            Debug.Log("Exited PlayerDeadState");
        }
    }
}
