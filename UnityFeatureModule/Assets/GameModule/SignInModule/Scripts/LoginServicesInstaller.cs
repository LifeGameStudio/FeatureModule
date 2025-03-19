namespace GameModule.SignInModule.Scripts
{
    using Zenject;

    public class LoginServicesInstaller : Installer<LoginServicesInstaller>
    {
        public override void InstallBindings()
        {
#if GOOGLE_LOGIN
            this.Container.Bind<ILoginServices>().To<GoogleLoginServices>().AsCached().NonLazy();

#elif FACEBOOK_LOGIN
#else
            this.Container.Bind<ILoginServices>().To<DummyLoginServices>().AsCached().NonLazy();
#endif
        }
    }
}