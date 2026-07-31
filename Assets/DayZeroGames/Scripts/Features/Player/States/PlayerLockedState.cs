using UnityEngine;

namespace DZ.Features
{
    public class PlayerLockedState : BaseState
    {
        public PlayerLockedState(PlayerContext ctx) : base(ctx)
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
