namespace Game.Scripts.Installer.Project
{
    using FeatureTemplate.Scripts.Installers;
    using FeatureTemplate.Scripts.Toast;
    using Game.Scripts.Services;
    using GameFoundation.Scripts;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.QuestModule;
    using UnityEngine.EventSystems;
    using Zenject;

    public class GameProjectInstaller : MonoInstaller
    {
        public FeatureToastController featureToastController;

        public override void InstallBindings()
        {
            SignalDeclarationInstaller.Install(this.Container);
            GameFoundationInstaller.Install(this.Container);
            FeaturesInstaller.Install(this.Container, this.featureToastController);
            this.Container.Resolve<ScreenManager>().gameObject.SetActive(false);
            //EventSystem
            this.Container.Bind<EventSystem>().FromComponentInNewPrefabResource("EventSystem").AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<GameDataState>().AsCached().NonLazy();
            QuestInstaller.Install(this.Container);
        }
    }
}