using DZ.Core;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    public abstract class BaseState : IState
    {
        protected PlayerController playerController;
        protected PlayerStateMachine playerStateMachine;
        protected IInputReader inputReader;
        public BaseState(PlayerController playerController, PlayerStateMachine playerStateMachine, IInputReader inputReader)
        {
            this.playerController = playerController;
            this.playerStateMachine = playerStateMachine;
            this.inputReader = inputReader;

        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void FixedUpdate() { }
        public virtual void Update() { }
    }
}
