using DZ.Core.Contracts;
using DZ.Core.Runtime;

namespace DZ.Features
{
    public sealed class PlayerContext
    {
        public PlayerController Controller { get; }
        public PlayerAnimationController Animation { get; }
        public PlayerStateMachine StateMachine { get; }
        public IInputReader Input { get; }
        public IAudioService AudioService {get; }
        public ISignalBus  SignalBus{get; }

        public PlayerContext(
            PlayerController controller,
            PlayerAnimationController animation,
            PlayerStateMachine stateMachine,
            IInputReader input,
            IAudioService audioService,
            ISignalBus signalBus)
        {
            Controller = controller;
            Animation = animation;
            StateMachine = stateMachine;
            Input = input;
            AudioService = audioService;
            SignalBus = signalBus;
        }
    }
}
