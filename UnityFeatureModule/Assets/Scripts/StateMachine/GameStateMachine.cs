namespace Game.Scripts.StateMachine
{
    using System.Collections.Generic;
    using FeatureTemplate.Scripts.StateMachine.Controller;
    using FeatureTemplate.Scripts.StateMachine.Interface;
    using Game.Scripts.StateMachine.State;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class GameStateMachine : FeatureStateMachine, IInitializable
    {
        public GameStateMachine(List<IFeatureGameState> listState, ILogService logService, ISignalBus signalBus) : base(listState, logService, signalBus) { }

        public void Initialize() { this.TransitionTo<GameHomeState>(); }
    }
}