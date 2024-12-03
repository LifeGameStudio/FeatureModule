namespace GameModule.ScreenQueue.Scripts
{
    using Zenject;

    public class ScreenQueueInstaller : Installer<ScreenQueueInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<ScreenQueueService>().AsCached().NonLazy();
        }
    }
}