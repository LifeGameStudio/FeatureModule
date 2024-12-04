namespace GameModule.StateMachine.Scripts.Examples
{
    public class ExampleHomeState : BaseState
    {
        public override void Enter()
        {
            this.ChangeState<ExamplePlayingState>();
        }

        public override void Exit()
        {
            
        }
    }
}