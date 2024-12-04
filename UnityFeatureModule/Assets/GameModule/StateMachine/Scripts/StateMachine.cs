namespace GameModule.StateMachine.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FeatureTemplate.Scripts.Services;
    using Sirenix.Utilities;
    using Zenject;

    public class StateMachine : ITickable
    {
        private IState                   currentState;
        protected Dictionary<Type, IState> TypeToState;

        protected StateMachine(List<IState> listState)
        {
            this.TypeToState = listState.ToDictionary(state => state.GetType(), state => state);

            this.TypeToState.Values.ForEach(x =>
            {
                if (x is BaseState baseState)
                {
                    baseState.Setup(this);
                }
            });
        }

        internal void SetCurrentState(Type stateType)
        {
            if (!this.TypeToState.TryGetValue(stateType, out var newState)) return;
            
            if (this.currentState != null)
            {
                this.LogMessage("HLQ Exit state: " + this.currentState.GetType());
                this.currentState.Exit();
            }

            this.currentState = newState;

            if (this.currentState != null)
            {
                this.LogMessage("HLQ Enter state: " + this.currentState.GetType());
                this.currentState.Enter();
            }
        }

        public void Tick()
        {
            if (this.currentState is ITickable updateableState)
            {
                updateableState.Tick();
            }
        }

        public void Initialize(Type beginState)
        {
            this.SetCurrentState(beginState);
            this.currentState?.Enter();
        }

        public Type GetCurrentState()
        {
            return this.currentState.GetType();
        }
    }
}