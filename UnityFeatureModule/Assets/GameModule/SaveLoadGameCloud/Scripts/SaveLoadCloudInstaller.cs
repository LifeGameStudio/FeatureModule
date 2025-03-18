namespace GameModule.SaveLoadGameCloud.Scripts
{
    using System.Collections.Generic;
    using GameModule.SaveLoadGameCloud.Scripts.Signal;
    using UnityEngine.Scripting;
    using Zenject;

    public class SaveLoadCloudInstaller : Installer<List<string>, SaveLoadCloudInstaller>
    {
        private readonly List<string> listIgnoreCloudData;

        [Preserve]
        public SaveLoadCloudInstaller(List<string> listIgnoreCloudData) { this.listIgnoreCloudData = listIgnoreCloudData; }

        public override void InstallBindings()
        {
            this.Container.DeclareSignal<UserCloudDataLoadCompletedSignal>();

#if FIREBASE_DATA_CLOUD
            this.Container.Bind<List<string>>().FromInstance(this.listIgnoreCloudData).WhenInjectedInto<FirebaseDataCloudServices>();
            this.Container.BindInterfacesAndSelfTo<FirebaseDataCloudServices>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
#endif
        }
    }
}