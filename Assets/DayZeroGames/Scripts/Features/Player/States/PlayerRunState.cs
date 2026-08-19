using UnityEngine;

namespace DZ.Features
{
	public class PlayerRunState : BaseState
	{
		public PlayerRunState(PlayerContext ctx) : base(ctx)
		{
		}

		public override void Enter()
		{
			
			inputReader.OnJumpPerformed += HandlePlayerJump;
			playerAnimationController.PlayRunAnimation();
		}

		public override void Update()
		{
			CheckForStateChange();
		}

		public override void FixedUpdate()
		{
			HandlePlayerRun();
		}

		public override void Exit()
		{
			
			inputReader.OnJumpPerformed -= HandlePlayerJump;
		}

		private void HandlePlayerRun()
		{
			playerController.MovePlayer(inputReader.moveInput);
		}

		private void HandlePlayerJump()
		{
			if (playerController.IsGrounded)
			{
				playerController.JumpState.JumpOnEnter();
				playerStateMachine.ChangeState(playerController.JumpState);
			}
		}

		private void CheckForStateChange()
		{
			if (Mathf.Abs(inputReader.moveInput) <= 0.01f && playerController.IsGrounded)
			{
				playerStateMachine.ChangeState(playerController.IdleState);
				return;
			}

			if (!playerController.IsGrounded) playerStateMachine.ChangeState(playerController.JumpState);
		}
	}
}
