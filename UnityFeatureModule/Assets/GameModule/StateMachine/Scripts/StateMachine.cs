namespace GameModule.StateMachine.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GameFoundation.Scripts.Utilities.LogService;
    using Sirenix.Utilities;
    using Zenject;

    public class StateMachine : ITickable, IInitializable
    {
        private readonly ILogService              logger;
        private readonly TickableManager          tickableManager;
        private          IState                   currentState;
        protected        Dictionary<Type, IState> TypeToState;

        public StateMachine(List<IState> listState, ILogService logger, TickableManager tickableManager)
        {
            this.logger          = logger;
            this.tickableManager = tickableManager;
            this.TypeToState     = listState.ToDictionary(state => state.GetType(), state => state);

            this.TypeToState.Values.ForEach(x =>
            {
                if (x is BaseState baseState)
                {
                    baseState.Setup(this);
                }
            });
        }

        public void SetCurrentState(Type stateType) // TODO: Change back to internal void when someone fuck up or "i told you so"
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

        public void Initialize(Type beginState) { this.beginStateType = beginState; }

        public Type GetCurrentState() { return this.currentState.GetType(); }

        private Type beginStateType;

        public void Initialize()
        {
            this.SetCurrentState(beginStateType);

            foreach (var item in this.TypeToState.Values)
            {
                if (item is ITickable updateableState && this.tickableManager.Tickables.Contains(updateableState))
                    this.tickableManager.Remove(updateableState);
            }
        }
    }
}