using DZ.Core;

namespace DZ.Features
{
    public class PlayerStateMachine
    {
        private IState currentState;
        public IState CurrentState => currentState;

        public void Initialize(IState startingState)
        {
            currentState = startingState;
            currentState?.Enter();
        }

        public void ChangeState(IState newState)
        {
            if(currentState == newState||newState == null) return;
            currentState?.Exit();
            currentState = newState;
            currentState?.Enter();
        }

        public void Update()
        {
            currentState?.Update();
        }
        public void FixedUpdate()
        {
            currentState?.FixedUpdate();
        }

        public void ShutDown()
        {
            currentState?.Exit();
            currentState = null;
        }

    }
}
