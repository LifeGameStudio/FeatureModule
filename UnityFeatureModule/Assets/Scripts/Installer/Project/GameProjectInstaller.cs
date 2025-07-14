namespace Game.Scripts.Installer.Project
{
    using System.Collections.Generic;
    using FeatureTemplate.Scripts.Installers;
    using FeatureTemplate.Scripts.Toast;
    using Game.Scripts.Services;
    using GameFoundation.Scripts;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.LeaderBoard;
    using GameModule.LeaderBoard.Scripts;
    using GameModule.QuestModule;
    using GameModule.RuntimeCsvFromDrive.Scripts;
    using GameModule.SaveLoadGameCloud.Scripts;
    using GameModule.SignInModule.Scripts;
    using UnityEngine.EventSystems;
    using Zenject;

    public class GameProjectInstaller : MonoInstaller
    {
        public FeatureToastController featureToastController;

        public override void InstallBindings()
        {
            SignalDeclarationInstaller.Install(this.Container);
            GameFoundationInstaller.Install(this.Container);
            FeaturesInstaller.Install(this.Container, this.featureToastController,new Dictionary<string,string>());
            this.Container.Resolve<ScreenManager>().gameObject.SetActive(false);
            //EventSystem
            this.Container.Bind<EventSystem>().FromComponentInNewPrefabResource("EventSystem").AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<GameDataState>().AsCached().NonLazy();
            QuestInstaller.Install(this.Container);
            // TimeMarkInstaller.Install(this.Container);
            // UnitTestInstaller<TimeMarkTest>.Install(this.Container);
            // LoginServicesInstaller.Install(this.Container);
            // SaveLoadCloudInstaller.Install(this.Container, new List<string>() { "BlueprintInfoData" });
            // RuntimeLoadCsvInstaller.Install(this.Container);
            this.Container.BindInterfacesAndSelfTo<LeaderboardTesting>().AsSingle().NonLazy();
            LoginServicesInstaller.Install(this.Container);
            LeaderboardServicesInstaller.Install(this.Container);
        }
    }
}