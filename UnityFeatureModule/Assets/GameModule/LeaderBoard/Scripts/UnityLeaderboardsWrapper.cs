#if UNITY_LEADERBOARD

namespace GameModule.LeaderBoard.Scripts
{
    using System;
    using System.Collections.Generic;
    using Assets.SimpleGoogleSignIn.Scripts;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using GameModule.SignInModule.Scripts;
    using Unity.Services.Authentication;
    using Unity.Services.Core;
    using Unity.Services.Leaderboards;
    using UnityEngine;

    public class UnityLeaderboardsWrapper : ILeaderboardsService
    {
        private readonly ILoginServices loginServices;

        public UnityLeaderboardsWrapper(ILoginServices loginServices) { this.loginServices = loginServices; }
        public           string                 LeaderboardId { get; set; }
        public           string                 UserId        { get; set; }
        public           string                 UserName      { get; set; }
        private readonly List<LeaderBoardEntry> limitEntries = new();
        private readonly List<LeaderBoardEntry> allEntries   = new();

        public async void Init(string idToken, string accessToken, string leaderboardId = "leaderboard")
        {
            await UnityServices.InitializeAsync();

            await AuthenticationService.Instance.SignInWithGoogleAsync(idToken);

            if (AuthenticationService.Instance.IsSignedIn)
            {
                this.LeaderboardId = leaderboardId;
                this.UserId        = AuthenticationService.Instance.PlayerId;
                this.UserName      = AuthenticationService.Instance.PlayerName;
                this.LogMessage($"UGS sign in with google success! UserId: {this.UserId} - UserName: {AuthenticationService.Instance.PlayerName}", Color.chartreuse);
            }
            else
            {
                this.InitUGS().Forget();
            }
        }

        public async UniTask FetchLimitEntries(int limit = 10)
        {
            try
            {
                this.limitEntries.Clear(); // Fix: Clear list trước khi thêm mới

                var scores = await LeaderboardsService.Instance.GetScoresAsync(this.LeaderboardId, new GetScoresOptions
                {
                    Limit = limit
                });

                this.LogMessage($"Fetched {scores.Results.Count} scores from leaderboard.", Color.chartreuse);

                foreach (var entry in scores.Results)
                {
                    this.limitEntries.Add(new LeaderBoardEntry
                    {
                        userName = entry.PlayerName,
                        score    = (int)entry.Score
                    });

                    this.LogMessage($"Limit Leaderboard has User: {entry.PlayerName}, Score: {entry.Score}", Color.hotPink);
                }
            }
            catch (Exception ex)
            {
                this.LogMessage("Failed to fetch leaderboard: " + ex.Message, Color.red);
            }
        }

        public async UniTask FetchAllEntries()
        {
            this.allEntries.Clear();
            var pageSize      = 50;
            var currentOffset = 0;
            var hasMore       = true;

            while (hasMore)
            {
                var options = new GetScoresOptions
                {
                    Limit  = pageSize,
                    Offset = currentOffset
                };

                var response = await LeaderboardsService.Instance.GetScoresAsync(this.LeaderboardId, options);

                foreach (var leaderboardEntry in response.Results)
                {
                    this.allEntries.Add(new LeaderBoardEntry()
                    {
                        userName = leaderboardEntry.PlayerName,
                        score    = (int)leaderboardEntry.Score
                    });

                    this.LogMessage($"Leaderboard has User: {leaderboardEntry.PlayerName}, Score: {leaderboardEntry.Score}", Color.cyan);
                }

                if (response.Results.Count < pageSize)
                {
                    hasMore = false; // không còn trang tiếp theo
                }
                else
                {
                    currentOffset += pageSize;
                }
            }
        }

        public List<LeaderBoardEntry> LimitEntries() => this.limitEntries;

        public List<LeaderBoardEntry> AllEntries() => this.allEntries;

        private async UniTask InitUGS()
        {
            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                // Fix: Luôn cập nhật UserId và fetch leaderboard
                this.UserId   = AuthenticationService.Instance.PlayerId;
                this.UserName = AuthenticationService.Instance.PlayerName;
                this.LogMessage("Signed in as: " + this.UserId, Color.chartreuse);
            }
            catch (Exception ex)
            {
                this.LogMessage("Failed to initialize UGS: " + ex.Message, Color.red);
            }
        }

        public async UniTask SubmitScore(int score)
        {
            if (score <= LeaderboardSaver.UserScore)
            {
                this.LogMessage($"Score {score} is not higher than current score {LeaderboardSaver.UserScore}. Not submitting.", Color.yellow);

                return;
            }

            try
            {
                var submitResponse = await LeaderboardsService.Instance.AddPlayerScoreAsync(this.LeaderboardId, score);
                this.LogMessage($"Submit thành công: Rank {submitResponse.Rank}, Score {submitResponse.Score}", Color.chartreuse);
                LeaderboardSaver.UserScore = score; // Cập nhật điểm số mới
                // Refresh leaderboard sau khi submit
                // await this.FetchTopScores();
            }
            catch (Exception e)
            {
                this.LogMessage($"Submit score failed: {e}", Color.red);
            }
        }

        public async UniTask UpdateUserName(string userName)
        {
            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(userName);
                this.UserName = userName; // Fix: Cập nhật local userName
                this.LogMessage($"Đã cập nhật userName thành: {userName}", Color.chartreuse);
            }
            catch (Exception e)
            {
                this.LogMessage($"Update userName failed: {e}", Color.red);
            }
        }
    }
}
#endif