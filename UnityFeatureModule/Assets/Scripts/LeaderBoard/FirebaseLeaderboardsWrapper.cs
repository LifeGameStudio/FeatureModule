#if FIREBASE_LEADERBOARD
namespace Game.Scripts.LeaderBoard
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Firebase;
    using Firebase.Extensions;
    using UnityEngine;
    using Zenject;

    public class FirebaseLeaderboardsWrapper : ILeaderboardsService, IInitializable
    {
        private DatabaseReference      dbReference;
        private List<LeaderBoardEntry> entries;

        public void Initialize() { this.Init(); }

        public List<LeaderBoardEntry> Entries() => this.entries;

        public UniTask SubmitScore(LeaderBoardEntry config)
        {
            var json = JsonUtility.ToJson(config);

            this.dbReference.Child("leaderboard").Child(config.userName).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error submitting score: " + task.Exception);
                }
                else
                {
                    Debug.Log("Score submitted successfully for " + config.userName);
                }
            });
        }

        private void Init()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;

                if (dependencyStatus == DependencyStatus.Available)
                {
                    // FirebaseApp app = FirebaseApp.DefaultInstance;
                    dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                    this.FetchLeaderboard();
                }
                else
                {
                    Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                }
            });
        }

        private void FetchLeaderboard()
        {
            FirebaseDatabase.DefaultInstance
                .GetReference("leaderboard")
                .OrderByChild("score")
                .LimitToLast(10) // Lấy top 10
                .GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Error fetching leaderboard: " + task.Exception);
                    }
                    else if (task.IsCompleted)
                    {
                        DataSnapshot snapshot = task.Result;

                        foreach (DataSnapshot child in snapshot.Children)
                        {
                            var json  = child.GetRawJsonValue();
                            var entry = JsonUtility.FromJson<LeaderBoardEntry>(json);
                            this.entries.Add(entry);
                        }

                        // Sắp xếp điểm giảm dần vì LimitToLast() + OrderByChild() trả về tăng dần
                        this.entries.Sort((a, b) => b.score.CompareTo(a.score));
                    }
                });
        }
    }
}
#endif