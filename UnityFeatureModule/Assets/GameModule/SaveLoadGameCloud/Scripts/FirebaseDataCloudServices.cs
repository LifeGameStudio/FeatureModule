#if FIREBASE_DATA_CLOUD

namespace GameModule.SaveLoadGameCloud.Scripts
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using Firebase.Auth;
    using Firebase.Database;
    using Firebase.Extensions;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameModule.SaveLoadGameCloud.Scripts.Signal;
    using Newtonsoft.Json;
    using UnityEngine;

    public class FirebaseDataCloudServices : BaseHandleDataCloud
    {
        private       FirebaseUser currentUser;
        private const string       RootGameData = "gameData";
        public static int          RetryCount   = 2;
        private       int          CurrentRetryCount { get; set; }

        public override async UniTask Login()
        {
            if (!this.IsFirebaseReady)
            {
                await UniTask.WaitUntil(() => this.IsFirebaseReady);
            }

            this.CurrentRetryCount = 0;

            var token = await this.LoginServices.SignIn();

            this.currentUser = await this.SignInWithGoogle(token.Item1, token.Item2);

            this.LogMessage(this.currentUser?.Email);
        }

        public override async UniTask<Dictionary<string, string>> LoadData(bool forceOverrideToLocal = false)
        {
            if (this.currentUser == null)
                return new Dictionary<string, string>();

            var result            = new Dictionary<string, string>();
            var databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

            databaseReference.Child("users").Child(this.currentUser.UserId).Child(RootGameData).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    this.LogMessage("Error: " + task.Exception);

                    this.SignalBus.Fire(new UserCloudDataLoadCompletedSignal()
                    {
                        IsSuccess = false
                    });
                }
                else if (task.IsCompleted)
                {
                    var snapshot = task.Result;
                    var data     = snapshot.GetValue(true);

                    if (data == null) return;

                    var dataDic = (Dictionary<string, object>)data;

                    foreach (var item in dataDic)
                    {
                        var key = item.Key.Replace("LD-", "");
                        result.Add(key, item.Value.ToString());
                    }

                    if (forceOverrideToLocal)
                    {
                        foreach (var item in result)
                        {
                            if (!this.UserDataCache.TryGetValue(item.Key, out var value1) || this.ListIgnoreCloudData.Contains(item.Key)) continue;

                            var value = JsonConvert.DeserializeObject(item.Value.ToString(), value1.GetType());

                            value.CopyTo(this.UserDataCache[item.Key]);
                        }
                    }

                    this.SignalBus.Fire(new UserCloudDataLoadCompletedSignal()
                    {
                        IsSuccess = true
                    });
                }
            });

            return result;
        }

        private async UniTask<FirebaseUser> SignInWithGoogle(string idToken, string acessToken)
        {
            this.CurrentRetryCount++;

            var          auth   = FirebaseAuth.DefaultInstance;
            FirebaseUser user   = null;
            AuthResult   result = null;

            var credential =
                GoogleAuthProvider.GetCredential(idToken, acessToken);

            var isComplete = false;

            await auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    isComplete = true;

                    return;
                }

                if (task.IsFaulted)
                {
                    this.LogMessage("Error: " + task.Exception, Color.red);
                    isComplete = true;

                    return;
                }

                isComplete = true;
                result     = task.Result;
            });

            await UniTask.WaitUntil(() => isComplete);

            if (result is not { User: not null })
            {
                if (this.CurrentRetryCount < RetryCount)
                {
                    user = await this.SignInWithGoogle(idToken, acessToken);

                    return user;
                }

                return null;
            }

            user = result.User;

            return user;
        }

        protected override UniTask SaveData()
        {
            this.SaveDataToCloud(this.currentUser);

            return UniTask.CompletedTask;
        }

        private void SaveDataToCloud(FirebaseUser user)
        {
            if (this.currentUser == null) return;
            var databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

            var gameData = new Dictionary<string, object>();

            foreach (var kvp in this.UserDataCache)
            {
                if (this.ListIgnoreCloudData.Contains(kvp.Value.GetType().Name)) continue;

                gameData[kvp.Key] = JsonConvert.SerializeObject(kvp.Value);
            }

            databaseReference.Child("users").Child(user.UserId).Child(RootGameData).SetValueAsync(gameData).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    this.LogMessage("Game data saved successfully!");
                }
                else
                {
                    this.LogMessage("Failed to save game data: " + task.Exception, Color.red);
                }
            });
        }
    }
}

#endif