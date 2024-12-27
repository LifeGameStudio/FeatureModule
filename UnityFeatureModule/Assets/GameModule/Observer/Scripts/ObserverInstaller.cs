namespace GameModule.Observer.Scripts
{
    using Zenject;

    public class ObserverInstaller : Installer<ObserverInstaller>
    {
        public override void InstallBindings()
        {
            var item = this.Container.Resolve<ISignalBus>();

            if (item == null)
            {
                this.Container.Unbind<ISignalBus>();
            }

            this.Container.Bind<ISignalBus>().To<Observer>().AsSingle().NonLazy();
        }
    }
}