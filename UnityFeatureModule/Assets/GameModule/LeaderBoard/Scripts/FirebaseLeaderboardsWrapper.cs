#if FIREBASE_LEADERBOARD
namespace GameModule.LeaderBoard.Scripts
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using Firebase.Auth;
    using Firebase.Database;
    using Firebase.Extensions;
    using FirebaseAuthentication;
    using GameModule.SignInModule.Scripts;
    using UnityEngine;

    public class FirebaseLeaderboardsWrapper : ILeaderboardsService
    {
        private readonly IFirebaseAuth  firebaseAuth;
        private readonly ILoginServices loginServices;

        public FirebaseLeaderboardsWrapper(IFirebaseAuth firebaseAuth, ILoginServices loginServices)
        {
            this.firebaseAuth  = firebaseAuth;
            this.loginServices = loginServices;
        }

        private          DatabaseReference      dbReference;
        private readonly List<LeaderBoardEntry> limitEntries = new();
        private readonly List<LeaderBoardEntry> allEntries   = new();

        public string LeaderboardId { get; set; } = "leaderboard";
        public string UserId        { get; set; }
        public string UserName      { get; set; }

        public List<LeaderBoardEntry> LimitEntries() => this.limitEntries;
        public List<LeaderBoardEntry> AllEntries()   => this.allEntries;

        public async void Init(string idToken, string accessToken, string leaderboardId = "leaderboard")
        {
            this.LogMessage("Initializing Firebase Leaderboards...", Color.chartreuse);

            {
                this.dbReference   = FirebaseDatabase.DefaultInstance.RootReference;
                this.LeaderboardId = leaderboardId;

                if (this.loginServices.IsSignedIn)
                {
                    var loginResult = await this.firebaseAuth.SignInWithGoogle(idToken, accessToken);

                    this.UserId   = loginResult.uid;
                    this.ConfigUserName(loginResult.username);
                    this.LogMessage($"google login success! UserName: {this.UserName} - UserId: {this.UserId}", Color.chartreuse);
                }
                else
                {
                    var auth = FirebaseAuth.DefaultInstance;

                    if (auth.CurrentUser == null)
                    {
                        await auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask =>
                        {
                            if (authTask.IsCompleted && !authTask.IsFaulted)
                            {
                                if (auth.CurrentUser != null)
                                {
                                    this.UserId = auth.CurrentUser.UserId;
                                    this.ConfigUserName(auth.CurrentUser.DisplayName);
                                }

                                this.LogMessage($"Login anonymous success! UserName: {this.UserName} - UserId: {this.UserId}", Color.chartreuse);
                            }
                            else
                            {
                                this.LogMessage("Login failed: " + authTask.Exception, Color.red);
                            }
                        });
                    }
                    else
                    {
                        this.UserId = auth.CurrentUser.UserId;
                        this.ConfigUserName(auth.CurrentUser.DisplayName);
                        this.LogMessage($"Using existing UID: {auth.CurrentUser.UserId} - UserName: {this.UserName}", Color.chartreuse);
                    }
                }
            }
        }

        public void FetchLimitEntries(int limit = 10)
        {
            this.limitEntries.Clear();

            FirebaseDatabase.DefaultInstance
                .GetReference(this.LeaderboardId)
                .OrderByChild(nameof(LeaderBoardEntry.score))
                .LimitToLast(limit)
                .GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        this.LogMessage("Error fetching leaderboard: " + task.Exception, Color.red);
                    }
                    else if (task.IsCompleted)
                    {
                        var snapshot = task.Result;
                        this.LogMessage($"Limit Leaderboard fetched successfully. Total entries: {snapshot.ChildrenCount}", Color.chartreuse);

                        foreach (var child in snapshot.Children)
                        {
                            var json  = child.GetRawJsonValue();
                            var entry = JsonUtility.FromJson<LeaderBoardEntry>(json);
                            this.limitEntries.Add(entry);
                        }

                        this.limitEntries.Sort((a, b) => b.score.CompareTo(a.score));
                    }
                });
        }

        public async void FetchAllEntries()
        {
            this.allEntries.Clear();
            var dbRef = FirebaseDatabase.DefaultInstance.GetReference(this.LeaderboardId);

            var snapshot = await dbRef.OrderByChild(nameof(LeaderBoardEntry.score)).GetValueAsync();
            
            this.LogMessage($"All Leaderboard fetched successfully. Total entries: {snapshot.ChildrenCount}", Color.chartreuse);

            foreach (var child in snapshot.Children)
            {
                var json  = child.GetRawJsonValue();
                var entry = JsonUtility.FromJson<LeaderBoardEntry>(json);
                this.allEntries.Add(entry);
            }

            // Sắp xếp descending vì Realtime Database trả ascending
            this.allEntries.Sort((a, b) => b.score.CompareTo(a.score));
        }

        private void ConfigUserName(string userName)
        {
            this.UserName = string.IsNullOrEmpty(LeaderboardSaver.UserName) ? userName : LeaderboardSaver.UserName;
            this.LogMessage($"Configured UserName: {this.UserName}", Color.cyan);
        }

        public UniTask SubmitScore(int score)
        {
            this.dbReference.Child(this.LeaderboardId).Child(this.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    if (score > LeaderboardSaver.UserScore)
                    {
                        LeaderboardSaver.UserScore = score;

                        var entry = new LeaderBoardEntry
                        {
                            userName = this.UserName,
                            score    = score,
                        };

                        this.dbReference.Child(this.LeaderboardId).Child(this.UserId).SetRawJsonValueAsync(JsonUtility.ToJson(entry));
                        this.LogMessage("Score submitted: " + score, Color.chartreuse);
                    }
                    else
                    {
                        this.LogMessage($"New score {score} is not higher than old score {LeaderboardSaver.UserScore}; submission skipped.", Color.yellow);
                    }
                }
                else
                {
                    this.LogMessage("Failed to read old score: " + task.Exception, Color.red);
                }
            });

            return UniTask.CompletedTask;
        }

        public UniTask UpdateUserName(string userName)
        {
            var entry = new LeaderBoardEntry
            {
                userName = userName,
                score    = LeaderboardSaver.UserScore
            };

            this.dbReference.Child(this.LeaderboardId).Child(this.UserId).SetRawJsonValueAsync(JsonUtility.ToJson(entry))
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        this.UserName             = userName;
                        LeaderboardSaver.UserName = userName;
                        this.LogMessage($"User name {userName} updated successfully!", Color.chartreuse);
                    }
                    else
                    {
                        this.LogMessage("Failed to update user name: " + task.Exception, Color.red);
                    }
                });

            return UniTask.CompletedTask;
        }
    }
}
#endif
