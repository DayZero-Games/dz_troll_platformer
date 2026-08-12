using UnityEngine;

namespace DZ.Features
{
	public class PlayerIdleState : BaseState
	{
		public PlayerIdleState(PlayerContext ctx) : base(ctx)
		{
		}

		public override void Enter()
		{
			
			inputReader.OnJumpPerformed += HandlePlayerJump;
			playerController.StopMovingPlayer();
			playerAnimationController.PlayIdleAnimation();
		}

		public override void Update()
		{
			HandlePlayerIdle();
			CheckForStateChange();
		}
        public override void FixedUpdate()
        {
            playerController.StopMovingPlayer();
        }

		private void HandlePlayerIdle()
		{
			
		}

		private void HandlePlayerJump()
		{
			if (playerController.IsGrounded)
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
			
		}
	}
}