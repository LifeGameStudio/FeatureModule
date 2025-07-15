namespace GameModule.LeaderBoard.Scripts
{
    using FirebaseAuthentication.FirebaseApp;
    using Zenject;

    public class LeaderboardServicesInstaller : Installer<LeaderboardServicesInstaller>
    {
        public override void InstallBindings()
        {
#if FIREBASE_LEADERBOARD
            this.Container.Bind<ILeaderboardsService>().To<FirebaseLeaderboardsWrapper>().AsCached().NonLazy();
            FirebaseAuthenticationInstaller.Install(this.Container);
#elif UNITY_LEADERBOARD
            this.Container.Bind<ILeaderboardsService>().To<UnityLeaderboardsWrapper>().AsCached().NonLazy();
#endif
        }
    }
}
