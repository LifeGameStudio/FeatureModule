#if UNITY_CLOUD

namespace GameModule.SaveLoadGameCloud.Scripts
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameModule.SaveLoadGameCloud.Scripts.Signal;
    using Newtonsoft.Json;
    using Unity.Services.Authentication;
    using Unity.Services.CloudSave;
    using Unity.Services.Core;
    using IUnityServices = GameModule.SaveLoadGameCloud.Scripts.Interfaces.IUnityServices;

    public class UnityDataCloudServices : BaseHandleDataCloud, IUnityServices
    {
        public override bool   IsSignedIn => AuthenticationService.Instance.IsSignedIn;
        public override string UserId     => AuthenticationService.Instance.PlayerId;

        protected override async UniTask SaveData()
        {
            if (UnityServices.Instance.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) return;
            var gameData = new Dictionary<string, object>();

            foreach (var kvp in this.UserDataCache)
            {
                if (!this.ListSaveData.Contains(kvp.Value.GetType().Name)) continue;

                gameData[kvp.Key] = kvp.Value;
            }

            await CloudSaveService.Instance.Data.Player.SaveAsync(gameData);
            this.LogMessage($"Data Saved to {AuthenticationService.Instance.PlayerId}");
        }

        public override async UniTask Login()
        {
            //init unity services
            if (UnityServices.Instance.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                return;
            }

            //Signin
            var token = await this.LoginServices.SignIn();
            AuthenticationService.Instance.SignedIn     += this.OnSignedIn;
            AuthenticationService.Instance.SignedOut    += this.OnSignedOut;
            AuthenticationService.Instance.SignInFailed += this.OnSignInFailed;
            await AuthenticationService.Instance.SignInWithGoogleAsync(token.Item1);
        }

        public override async UniTask<Dictionary<string, string>> LoadData()
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                this.SignalBus.Fire(new UserCloudDataLoadCompletedSignal()
                {
                    IsSuccess = false
                });

                return new Dictionary<string, string>();
            }

            //Load Data
            var keys = new HashSet<string>();

            foreach (var kvp in this.UserDataCache)
            {
                keys.Add(kvp.Key);
            }

            var gameDatas = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            var result    = new Dictionary<string, string>();

            foreach (var item in gameDatas)
            {
                var key = item.Key.Replace("LD-", "");

                if (item.Value?.Value == null)
                    continue;

                var jsonString = JsonConvert.SerializeObject(item.Value.Value);

                result.Add(key, jsonString);
            }
            this.SignalBus.Fire(new UserCloudDataLoadCompletedSignal()
            {
                IsSuccess = true
            });
            return result;
        }

        public override UniTask SaveDataFromCloudToLocal(Dictionary<string, string> input)
        {
            foreach (var item in input)
            {
                if (!this.UserDataCache.TryGetValue(item.Key, out var value1) || this.ListSaveData.Contains(item.Key))
                    continue;

                var value = JsonConvert.DeserializeObject(item.Value, value1.GetType());

                value.CopyTo(this.UserDataCache[item.Key]);
                this.LogMessage($"Data Loaded from cloud {item.Key}");
            }

            return UniTask.CompletedTask;
        }

        public async UniTask LinkGoogleAccount(bool isForce, Action<AuthenticationException> authenticationException = null, Action<RequestFailedException> requestFailedException = null,
            Action onComplete = null)
        {
            try
            {
                var token = await this.LoginServices.SignIn();
                await AuthenticationService.Instance.LinkWithGoogleAsync(token.Item1, new LinkOptions() { ForceLink = isForce });
                await this.SaveData();
                onComplete?.Invoke();
            }
            catch (AuthenticationException e)
            {
                this.LogMessage($"Authentication error: {e.Message}");
                authenticationException?.Invoke(e);
            }
            catch (RequestFailedException e)
            {
                this.LogMessage($"Request failed: {e.Message}");
                requestFailedException?.Invoke(e);
            }
            catch (Exception e)
            {
                this.LogMessage($"Unexpected error: {e.Message}");
            }
        }

        public bool IsGoogleLinked()
        {
            var playerInfo = AuthenticationService.Instance.PlayerInfo;

            if (playerInfo == null)
            {
                this.LogMessage("PlayerInfo is null. User may not be signed in.");

                return false;
            }

            var identities = playerInfo.GetGoogleId();

            return !string.IsNullOrEmpty(identities);
        }

        public override UniTask Logout()
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
                this.LoginServices.ClearSession();
                this.LogMessage("Signed Out");
            }

            return UniTask.CompletedTask;
        }

        private void OnSignedIn() { this.LogMessage($"Signed In {AuthenticationService.Instance.PlayerId}"); }

        private void OnSignInFailed(RequestFailedException obj) { this.LogMessage($"Sign In Failed{obj.Message}"); }

        private void OnSignedOut() { this.LogMessage("Signed Out"); }
    }
}
#endif