namespace GameModule.StateMachine.Scripts
{
    using GameFoundation.Scripts.Utilities.Extension;
    using Zenject;

    // T is the initialize state
    public class StateMachineInstaller<T> : Installer<StateMachineInstaller<T>> where T : BaseState
    {
        public override void InstallBindings()
        {
            // Bind StateMachine as a cached instance
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<IState>();
            this.Container.BindInterfacesAndSelfTo<StateMachine>().AsCached().NonLazy();
            // Resolve the StateMachine instance
            var stateMachine = this.Container.Resolve<StateMachine>();

            // Initialize the StateMachine with the initial state
            stateMachine.Initialize(typeof(T));
        }
    }

}