using DZ.Core;
using DZ.Core.Contracts;

namespace DZ.Features
{
    public abstract class BaseState : IState
    {
        protected readonly PlayerContext ctx;

        protected readonly PlayerController playerController;
        protected readonly PlayerAnimationController playerAnimationController;
        protected readonly PlayerStateMachine playerStateMachine;
        protected readonly IInputReader inputReader;
        protected readonly IAudioService audioService;
        protected readonly ISignalBus signalBus;

        protected BaseState(PlayerContext ctx)
        {
            this.ctx = ctx;
            playerController = ctx.Controller;
            playerAnimationController = ctx.Animation;
            playerStateMachine = ctx.StateMachine;
            inputReader = ctx.Input;
            audioService = ctx.AudioService;
            signalBus = ctx.SignalBus;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void FixedUpdate() { }
        public virtual void Update() { }
    }
}
