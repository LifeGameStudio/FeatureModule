namespace GameModule.LeaderBoard.Scripts
{
    using Assets.SimpleGoogleSignIn.Scripts;
    using FeatureTemplate.Scripts.Services;
    using GameModule.SignInModule.Scripts;
    using ServiceImplementation.FireBaseRemoteConfig;
    using UnityEngine;
    using Zenject;

    public class LeaderboardTesting : IInitializable, ITickable
    {
        private readonly ISignalBus           signalBus;
        private readonly ILoginServices       loginServices;
        private readonly ILeaderboardsService leaderboardsService;

        public LeaderboardTesting(ISignalBus signalBus, ILoginServices loginServices, ILeaderboardsService leaderboardsService)
        {
            this.signalBus           = signalBus;
            this.loginServices       = loginServices;
            this.leaderboardsService = leaderboardsService;
        }

        public void Initialize() { this.signalBus.Subscribe<RemoteConfigFetchedSucceededSignal>(this.Callback); }

        private async void Callback(RemoteConfigFetchedSucceededSignal obj)
        {
            await this.loginServices.SignIn();
            this.leaderboardsService.Init(this.loginServices.GetToken().Item1, this.loginServices.GetToken().Item2);
        }

        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                this.leaderboardsService.SubmitScore(Random.Range(1, 10000));
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                foreach (var entry in this.leaderboardsService.LimitEntries())
                {
                    this.LogMessage($"Limit leaderboard has userName: {entry.userName}, score: {entry.score}", Color.hotPink);
                }

                foreach (var entry in this.leaderboardsService.AllEntries())
                {
                    this.LogMessage($"All leaderboard has userName: {entry.userName}, score: {entry.score}", Color.cyan);
                }
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                this.leaderboardsService.FetchLimitEntries();
                this.leaderboardsService.FetchAllEntries();
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                this.leaderboardsService.UpdateUserName("DUT");
            }
        }
    }
}
