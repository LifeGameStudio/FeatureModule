namespace GameModule.StateMachine.Scripts
{
    using System;

    public abstract class BaseState : IState
    {
        protected StateMachine StateMachine;

        public void Setup(StateMachine stateMachine)
        {
            this.StateMachine = stateMachine;
        }
        protected BaseState() {  }

        protected void ChangeState(Type newState) { this.StateMachine.SetCurrentState(newState); }
        protected void ChangeState<T>() { this.StateMachine.SetCurrentState(typeof(T)); }

        public abstract void Enter();
        public abstract void Exit();
    }
}