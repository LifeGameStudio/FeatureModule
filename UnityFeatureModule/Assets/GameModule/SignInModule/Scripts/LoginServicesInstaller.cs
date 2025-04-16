namespace GameModule.SignInModule.Scripts
{
    using UnityEngine.Scripting;
    using Zenject;

    public class LoginServicesInstaller : Installer<LoginServicesInstaller>
    {
        [Preserve]
        public LoginServicesInstaller() {
        }

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