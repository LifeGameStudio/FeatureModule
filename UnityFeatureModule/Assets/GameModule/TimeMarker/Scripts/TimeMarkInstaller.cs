namespace GameModule.TimeMarker.Scripts
{
    using Zenject;

    public class TimeMarkInstaller : Installer<TimeMarkInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<TimeMarkService>().AsCached().NonLazy();
        }
    }
}