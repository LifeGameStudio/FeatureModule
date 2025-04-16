namespace GameModule.SaveLoadGameCloud.Scripts
{
    using System.Collections.Generic;
    using System.Reflection;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Interfaces;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameModule.SaveLoadGameCloud.Scripts.Interfaces;
    using GameModule.SignInModule.Scripts;
    using ServiceImplementation.FireBaseRemoteConfig;
    using UnityEngine;
    using Zenject;

    public abstract class BaseHandleDataCloud : MonoBehaviour, IHandleDataCloud
    {
        [Inject] protected DiContainer                    Container;
        [Inject] protected IHandleUserDataServices        HandleUserDataServices;
        [Inject] protected ILoginServices                 LoginServices;
        [Inject] protected ISignalBus                     SignalBus;
        [Inject] protected List<string>                   ListIgnoreCloudData { get; set; }
        protected          bool                           IsFirebaseReady     { get; set; }
        protected          Dictionary<string, ILocalData> UserDataCache = new();

        protected virtual void Awake() { }

        protected virtual void Start()
        {
            this.SignalBus.Subscribe<RemoteConfigFetchedSucceededSignal>(this.OnRemoteConfigFetchedSucceeded);
            this.SignalBus.Subscribe<UserDataLoadedSignal>(this.OnUserDataLoaded);
        }

        private void OnUserDataLoaded()
        {
            this.UserDataCache =
                (Dictionary<string, ILocalData>)typeof(BaseHandleUserDataServices).GetField("userDataCache", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this.HandleUserDataServices);
        }

        private void OnRemoteConfigFetchedSucceeded() { this.IsFirebaseReady = true; }

        protected virtual void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                this.Login();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                this.SaveData();
            }
        }

        protected virtual void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) return;
            this.SaveData();
        }

        protected virtual void OnApplicationQuit() { this.SaveData(); }

        protected abstract UniTask SaveData();

        public abstract UniTask Login();

        public abstract UniTask<Dictionary<string, string>> LoadData(bool forceOverrideToLocal = false);

        public virtual UniTask Logout() { return UniTask.CompletedTask; }

        public virtual bool IsSignedIn { get; }
    }
}