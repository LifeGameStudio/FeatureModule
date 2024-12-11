namespace GameModule.StateMachine.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.Utilities.LogService;
    using Sirenix.Utilities;
    using Zenject;

    public class StateMachine : ITickable
    {
        private readonly ILogService              logger;
        private          IState                   currentState;
        protected        Dictionary<Type, IState> TypeToState;

        protected StateMachine(List<IState> listState, ILogService logger)
        {
            this.logger      = logger;
            this.TypeToState = listState.ToDictionary(state => state.GetType(), state => state);

            this.TypeToState.Values.ForEach(x =>
            {
                if (x is BaseState baseState)
                {
                    baseState.Setup(this);
                }
            });
        }

        public void SetCurrentState(Type stateType)// TODO: Change back to internal void when someone fuck up or "i told you so"
        {
            if (!this.TypeToState.TryGetValue(stateType, out var newState)) return;
            
            if (this.currentState != null)
            {
                this.logger.LogWithColor("H+ | Exit state: " + this.currentState.GetType());
                this.currentState.Exit();
            }

            this.currentState = newState;

            if (this.currentState != null)
            {
                this.logger.LogWithColor("H+ | Enter state: " + this.currentState.GetType());
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
        }

        public Type GetCurrentState()
        {
            return this.currentState.GetType();
        }
    }
}