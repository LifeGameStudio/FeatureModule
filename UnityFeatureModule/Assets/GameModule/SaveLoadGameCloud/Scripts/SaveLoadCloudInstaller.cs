namespace GameModule.SaveLoadGameCloud.Scripts
{
    using System.Collections.Generic;
    using GameModule.SaveLoadGameCloud.Scripts.Signal;
    using UnityEngine.Scripting;
    using Zenject;

    public class SaveLoadCloudInstaller : Installer<List<string>, SaveLoadCloudInstaller>
    {
        private readonly List<string> listCloudData;

        [Preserve]
        public SaveLoadCloudInstaller(List<string> listCloudData) { this.listCloudData = listCloudData; }

        public override void InstallBindings()
        {
            this.Container.DeclareSignal<UserCloudDataLoadCompletedSignal>();

#if FIREBASE_DATA_CLOUD
            this.Container.Bind<List<string>>().FromInstance(this.listCloudData).WhenInjectedInto<FirebaseDataCloudServices>();
            this.Container.BindInterfacesAndSelfTo<FirebaseDataCloudServices>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
#elif UNITY_CLOUD
            this.Container.Bind<List<string>>().FromInstance(this.listCloudData).WhenInjectedInto<UnityDataCloudServices>();
            this.Container.BindInterfacesAndSelfTo<UnityDataCloudServices>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
#else
#endif
        }
    }
}