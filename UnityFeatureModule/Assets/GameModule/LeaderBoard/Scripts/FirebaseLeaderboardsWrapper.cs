#if FIREBASE_LEADERBOARD
namespace GameModule.LeaderBoard.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using Firebase;
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
            FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);

            this.dbReference   = FirebaseDatabase.DefaultInstance.RootReference;
            this.LeaderboardId = leaderboardId;

            if (this.loginServices.IsSignedIn)
            {
                var loginResult = await this.firebaseAuth.SignInWithGoogle(idToken, accessToken);

                this.UserId = loginResult.uid;
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

        public async UniTask FetchLimitEntries(int limit = 10)
        {
            this.LogMessage($"Starting to fetch limit entries for leaderboard: {this.LeaderboardId}", Color.cyan);
            this.limitEntries.Clear();
            var          dbRef = FirebaseDatabase.DefaultInstance.GetReference(this.LeaderboardId);
            var          query = dbRef.OrderByChild(nameof(LeaderBoardEntry.score)).LimitToLast(limit);
            DataSnapshot snapshot = null;
                
            bool fetched = false;
            int  retry   = 0;
            while (!fetched && retry < 3)
            {
                try
                {
                    this.LogMessage($"⏳ Fetch attempt {retry + 1}...", Color.coral);
                    snapshot = await query.GetValueAsync().AsUniTask().Timeout(System.TimeSpan.FromSeconds(10));
                    this.LogMessage($"✅ Fetched {snapshot.ChildrenCount} entries.", Color.aquamarine);
                    fetched = true;
                }
                catch (TimeoutException)
                {
                    this.LogMessage($"⚠️ Timeout, retrying ({retry + 1}/3)...", Color.yellow);
                }
                catch (System.Exception ex)
                {
                    this.LogMessage($"❌ Fetch error: {ex}", Color.red);
                    break;
                }
                retry++;
            }

            if (!fetched)
            {
                this.LogMessage("❌ Fetch failed after 3 retries.", Color.red);

                return;
            }

            foreach (var child in snapshot.Children)
            {
                var json  = child.GetRawJsonValue();
                var entry = JsonUtility.FromJson<LeaderBoardEntry>(json);
                this.limitEntries.Add(entry);
            }

            this.limitEntries.Sort((a, b) => b.score.CompareTo(a.score));
            this.LogMessage($"Fetched {this.limitEntries.Count} limit entries from leaderboard.", Color.chartreuse);
        }

        public async UniTask FetchAllEntries()
        {
            this.LogMessage($"Starting to fetch all entries for leaderboard: {this.LeaderboardId}", Color.cyan);

            this.allEntries.Clear();
            var dbRef = FirebaseDatabase.DefaultInstance.GetReference(this.LeaderboardId);

            await this.FetchRecursive(dbRef, 200, null);
        }

        private async UniTask FetchRecursive(DatabaseReference dbRef, int batchSize, string lastKey)
        {
            while (true)
            {
                if (this.allEntries.Count >= LeaderboardSaver.TotalEntries)
                {
                    this.LogMessage($"⚡️ Reached maxFetch: {LeaderboardSaver.TotalEntries}", Color.aquamarine);

                    return;
                }

                var query = dbRef.OrderByKey().LimitToFirst(batchSize + 1);

                if (!string.IsNullOrEmpty(lastKey))
                    query = query.StartAt(lastKey);

                DataSnapshot snapshot = null;
                
                bool fetched = false;
                int  retry   = 0;
                while (!fetched && retry < 3)
                {
                    try
                    {
                        this.LogMessage($"⏳ Fetch attempt {retry + 1}...", Color.coral);
                        snapshot = await query.GetValueAsync().AsUniTask().Timeout(System.TimeSpan.FromSeconds(10));
                        this.LogMessage($"✅ Fetched {snapshot.ChildrenCount} entries.", Color.aquamarine);
                        fetched = true;
                    }
                    catch (TimeoutException)
                    {
                        this.LogMessage($"⚠️ Timeout, retrying ({retry + 1}/3)...", Color.yellow);
                    }
                    catch (System.Exception ex)
                    {
                        this.LogMessage($"❌ Fetch error: {ex}", Color.red);
                        break;
                    }
                    retry++;
                }

                if (!fetched)
                {
                    this.LogMessage("❌ Fetch failed after 3 retries.", Color.red);

                    break;
                }

                var    countThisBatch = 0;
                string newLastKey     = null;

                foreach (var child in snapshot.Children)
                {
                    newLastKey = child.Key;

                    if (child.Key == lastKey) continue; // skip duplicate

                    var entry = JsonUtility.FromJson<LeaderBoardEntry>(child.GetRawJsonValue());
                    this.allEntries.Add(entry);
                    countThisBatch++;

                    if (this.allEntries.Count >= LeaderboardSaver.TotalEntries)
                    {
                        this.LogMessage($"⚡️ Reached maxFetch: {LeaderboardSaver.TotalEntries}", Color.aquamarine);

                        return;
                    }
                }

                this.LogMessage($"✅ Batch fetched: {countThisBatch}, total: {this.allEntries.Count}", Color.chocolate);

                if (countThisBatch < batchSize)
                {
                    this.LogMessage($"🎯 Completed fetch, total entries: {this.allEntries.Count}", Color.aquamarine);

                    return;
                }

                await UniTask.Yield(); // tránh treo editor
                lastKey = newLastKey;
            }
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
