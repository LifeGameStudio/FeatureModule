#if UNITY_LEADERBOARD
namespace Game.Scripts.LeaderBoard
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using Zenject;

    public class UnityLeaderboardsWrapper : ILeaderboardsService, IInitializable
    {
        private List<LeaderBoardEntry> entries;

        public List<LeaderBoardEntry> Entries() => this.entries;

        public async UniTask SubmitScore(LeaderBoardEntry config)
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
        }

        public void Initialize()
        {
            this.InitUGS().Forget();
        }

        private async UniTask InitUGS()
        {
            await UnityService.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
                this.FetchTopScores().Forget();
            }
        }
        
        private async UniTask FetchTopScores()
        {
            try
            {
                var scores = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions
                {
                    Limit = 10 // lấy top 10
                });

                foreach (var entry in scores.Results)
                {
                    this.entries.Add(new LeaderBoardEntry
                    {
                        userName = entry.Player.PlayerId,
                        score    = entry.Score
                    });
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to fetch leaderboard: " + ex.Message);
            }
        }
    }
}
#endif