using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
	public class PlayerRunState : BaseState
	{
		public PlayerRunState(PlayerController playerController, PlayerAnimationController playerAnimationController,
			PlayerStateMachine playerStateMachine, IInputReader inputReader) :
			base(playerController, playerAnimationController, playerStateMachine, inputReader)
		{
		}

		public override void Enter()
		{
			Debug.Log("RunState Enter");
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
			Debug.Log("RunState Exit");
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
				playerStateMachine.ChangeState(playerController.JumpState);
			}
		}

		private void CheckForStateChange()
		{
			if (Mathf.Abs(inputReader.moveInput) <= 0.01f && playerController.IsGrounded)
			{
				playerStateMachine.ChangeState(playerController.IdleState);
			}

			if (!playerController.IsGrounded) playerStateMachine.ChangeState(playerController.JumpState);
		}
	}
}