namespace Game.Scripts.Installer.Scene.Main
{
    using Game.Scripts.Services;
    using Game.Scripts.StateMachine;
    using Game.Scripts.UnitTest;
    using GameModule.StateMachine.Scripts;
    using GameModule.StateMachine.Scripts.Examples;
    using GameModule.UnitTest;

    public class MainSceneInstaller : BaseSceneInstallerTemplate
    {
        public override void InstallBindings()
        {
            base.InstallBindings();
            this.Container.BindInterfacesAndSelfTo<MainScreenHandler>().AsCached().NonLazy();
            GameStateMachineInstaller.Install(this.Container);
            StateMachineInstaller<ExampleHomeState>.Install(this.Container); 
            UnitTestInstaller<StateMachineUnitTest>.Install(this.Container);
        }
    }
}