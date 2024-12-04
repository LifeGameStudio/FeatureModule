namespace GameModule.StateMachine.Scripts.Examples
{
    using UnityEngine;
    using Zenject;

    public class ExamplePlayingState : BaseState, ITickable
    {
        public override void Enter()
        {
            
        }

        public override void Exit()
        {
            
        }

        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                this.ChangeState<ExampleEndState>();
            }
        }
    }
}