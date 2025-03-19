#if UNITY_CLOUD

namespace GameModule.SaveLoadGameCloud.Scripts
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.Utilities.Extension;
    using Newtonsoft.Json;
    using Unity.Services.Authentication;
    using Unity.Services.CloudSave;
    using Unity.Services.Core;

    public class UnityDataCloudServices : BaseHandleDataCloud
    {
        protected override async UniTask SaveData()
        {
            if (UnityServices.Instance.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) return;
            var gameData = new Dictionary<string, object>();

            foreach (var kvp in this.UserDataCache)
            {
                if (this.ListIgnoreCloudData.Contains(kvp.Value.GetType().Name)) continue;

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
            //Load Data
            var keys = new HashSet<string>();

            foreach (var kvp in this.UserDataCache)
            {
                keys.Add(kvp.Key);
            }

            var gameDatas = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            foreach (var item in gameDatas)
            {
                var key = item.Key.Replace("LD-", "");

                if (!this.UserDataCache.TryGetValue(item.Key, out var value1) || this.ListIgnoreCloudData.Contains(key))
                    continue;

                if (item.Value?.Value == null)
                    continue;

                var jsonString = JsonConvert.SerializeObject(item.Value.Value);
                var value      = JsonConvert.DeserializeObject(jsonString, value1.GetType());

                value.CopyTo(this.UserDataCache[item.Key]);
                this.LogMessage($"Data Loaded from cloud {item.Key}");
            }
        }

        private void OnSignedIn() { this.LogMessage($"Signed In {AuthenticationService.Instance.PlayerId}"); }

        private void OnSignInFailed(RequestFailedException obj) { this.LogMessage($"Sign In Failed{obj.Message}"); }

        private void OnSignedOut() { this.LogMessage("Signed Out"); }
    }
}
#endif