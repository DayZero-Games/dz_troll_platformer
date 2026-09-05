using UnityEngine;

namespace DZ.Features
{
    public class PlayerJumpState : BaseState
    {
        private bool _hasLeftGround;
        private bool _isFalling;
        private bool _jumpOnEnter;

        public PlayerJumpState(PlayerContext ctx) : base(ctx)
        {
        }

        public override void Enter()
        {
           
            _hasLeftGround = false;
            _isFalling = false;
            inputReader.OnJumpPerformed += HandleAirJump;

            if (_jumpOnEnter)
            {
                HandlePlayerJump();
                playerAnimationController.PlayJumpUpAnimation();
            }
            else
            {
                _isFalling = true;
                playerAnimationController.PlayJumpDownAnimation();
            }

            _jumpOnEnter = false;
            
        }

        public void JumpOnEnter()
        {
            _jumpOnEnter = true;
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
            inputReader.OnJumpPerformed -= HandleAirJump;
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

        /// <summary>
        /// A jump pressed while already airborne. Deliberately does not change state - we are
        /// already in the jump state - it just re-fires the impulse if the level's air-jump
        /// budget allows, and rewinds the fall animation so the next descent plays correctly.
        /// </summary>
        private void HandleAirJump()
        {
            if (!playerController.Jump()) return;
            _isFalling = false;
            playerAnimationController.PlayJumpUpAnimation();
        }

    }
}
