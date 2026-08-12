using UnityEngine;

namespace DZ.Features
{
    public class PlayerJumpState : BaseState
    {
        private bool _hasLeftGround;
        private bool _isFalling;

        public PlayerJumpState(PlayerContext ctx) : base(ctx)
        {
        }

        public override void Enter()
        {
           
            _hasLeftGround = false;
            _isFalling = false;
            HandlePlayerJump();
            playerAnimationController.PlayJumpUpAnimation();
            
        }
        public override void Update()
        {
            UpdateAnimation();
            CheckForStateChange();
           
        }

        private void UpdateAnimation()
        {
            if (!_isFalling && playerController.PlayerRb.linearVelocityY < 0)
            {
                _isFalling = true;
                playerAnimationController.PlayJumpDownAnimation();
            }
        }

        public override void FixedUpdate()
        {
            playerController.MovePlayer(inputReader.moveInput);
            playerController.ApplyFallMultiplier();
        }
        public override void Exit()
        {
           
        }

        private void CheckForStateChange()
        {
            if (!playerController.IsGrounded)
            {
                _hasLeftGround = true;
                return;
            }

            if (!_hasLeftGround || !_isFalling) return;

            if (Mathf.Abs(inputReader.moveInput) > 0.01f) playerStateMachine.ChangeState(playerController.RunState);
            else playerStateMachine.ChangeState(playerController.IdleState);
        }

        private void HandlePlayerJump()
        {
            playerController.Jump();
        }

    }
}
