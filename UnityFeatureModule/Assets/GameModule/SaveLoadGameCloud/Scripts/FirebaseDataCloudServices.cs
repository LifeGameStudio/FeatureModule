#if FIREBASE_DATA_CLOUD
namespace GameModule.SaveLoadGameCloud.Scripts
{
    using System.Collections.Generic;
    using System.Reflection;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using Firebase.Auth;
    using Firebase.Database;
    using Firebase.Extensions;
    using GameFoundation.Scripts.Interfaces;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameModule.SaveLoadGameCloud.Scripts.Interfaces;
    using GameModule.SaveLoadGameCloud.Scripts.Signal;
    using GameModule.SignInModule.Scripts;
    using Newtonsoft.Json;
    using ServiceImplementation.FireBaseRemoteConfig;
    using UnityEngine;
    using Zenject;

    public class FirebaseDataCloudServices : MonoBehaviour, IHandleDataCloud
    {
        [Inject] private DiContainer                    container;
        [Inject] private IHandleUserDataServices        handleUserDataServices;
        [Inject] private ILoginServices                 loginServices;
        [Inject] private ISignalBus                     signalBus;
        [Inject] private List<string>                   ListIgnoreCloudData { get; set; }
        private          bool                           IsFirebaseReady     { get; set; }
        private          Dictionary<string, ILocalData> userDataCache = new();
        private          FirebaseUser                   currentUser;
        private const    string                         RootGameData = "gameData";
        public static    int                            RetryCount   = 2;
        private          int                            CurrentRetryCount { get; set; }

        private void Start()
        {
            this.signalBus.Subscribe<RemoteConfigFetchedSucceededSignal>(this.OnRemoteConfigFetchedSucceeded);
            this.signalBus.Subscribe<UserDataLoadedSignal>(this.OnUserDataLoaded);
        }

        private void OnUserDataLoaded()
        {
            this.userDataCache =
                (Dictionary<string, ILocalData>)typeof(BaseHandleUserDataServices).GetField("userDataCache", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this.handleUserDataServices);
        }

        private void OnRemoteConfigFetchedSucceeded() { this.IsFirebaseReady = true; }

        public async void Login()
        {
            if (!this.IsFirebaseReady)
            {
                await UniTask.WaitUntil(() => this.IsFirebaseReady);
            }

            this.CurrentRetryCount = 0;

            var token = await this.loginServices.SignIn();

            this.currentUser = await this.SignInWithGoogle(token.Item1, token.Item2);
            this.LoadDataFromDatabase(this.currentUser);
            this.LogMessage(this.currentUser?.Email);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                this.Login();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                this.SaveDataToCloud(this.currentUser);
            }
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

        private void LoadDataFromDatabase(FirebaseUser user)
        {
            if (this.currentUser == null) return;
            var databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

            databaseReference.Child("users").Child(user.UserId).Child(RootGameData).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    this.LogMessage("Error: " + task.Exception);

                    this.signalBus.Fire(new UserCloudDataLoadCompletedSignal()
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

                        if (!this.userDataCache.TryGetValue(item.Key, out var value1) || this.ListIgnoreCloudData.Contains(key)) continue;

                        var value = JsonConvert.DeserializeObject(item.Value.ToString(), value1.GetType());

                        value.CopyTo(this.userDataCache[item.Key]);
                    }

                    this.signalBus.Fire(new UserCloudDataLoadCompletedSignal()
                    {
                        IsSuccess = true
                    });
                }
            });
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) return;
            this.SaveDataToCloud(this.currentUser);
        }

        private void OnApplicationQuit() { this.SaveDataToCloud(this.currentUser); }

        private void SaveDataToCloud(FirebaseUser user)
        {
            if (this.currentUser == null) return;
            var databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

            var gameData = new Dictionary<string, object>();

            foreach (var kvp in this.userDataCache)
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