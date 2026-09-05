using DZ.Core;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    public class PlayerDeadState:BaseState
    {
        public PlayerDeadState(PlayerContext ctx) : base(ctx)
        {
        }

        public override void Enter()
        {
            playerAnimationController.PlayDeadAnimation();
            playerController.PlayerRb.linearVelocityX = 0f;
            audioService.PlaySfx(AudioId.Death);
            playerController.Die();
            signalBus.Publish(new PlayerDiedSignal());
        }

        public override void Exit()
        {
        }
    }
}
