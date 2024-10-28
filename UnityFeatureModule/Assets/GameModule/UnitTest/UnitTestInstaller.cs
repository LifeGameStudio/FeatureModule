namespace GameModule.UnitTest
{
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using Zenject;

    public class UnitTestInstaller<T> : Installer<UnitTestInstaller<T>> where T : IUnitTest
    {
        public override async void InstallBindings()
        {
            var screenManager = this.Container.Resolve<ScreenManager>();
            await screenManager.CloseAllScreenAsync();
            this.Container.Bind<T>().AsSingle().NonLazy();
            var unitTest = this.Container.Resolve<T>();
            await unitTest.PreConditionAsync();
            await unitTest.RunAsync();
            await unitTest.PostConditionAsync();
        }
    }
}