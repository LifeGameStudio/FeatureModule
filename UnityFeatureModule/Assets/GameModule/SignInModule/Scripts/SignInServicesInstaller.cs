namespace GameModule.SignInModule.Scripts
{
    using Zenject;

    public class SignInServicesInstaller: Installer<SignInServicesInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<ILoginServices>().To<GoogleLoginServices>().AsCached().NonLazy();
        }
    }
}