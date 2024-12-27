namespace Game.Scripts.Installer.Project
{
    using FeatureTemplate.Scripts.Installers;
    using FeatureTemplate.Scripts.Toast;
    using Game.Scripts.Services;
    using Game.Scripts.UnitTest;
    using GameFoundation.Scripts;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.Observer.Scripts;
    using GameModule.QuestModule;
    using GameModule.ScreenQueue.Scripts;
    using GameModule.TimeMarker.Scripts;
    using GameModule.UnitTest;
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
            TimeMarkInstaller.Install(this.Container);
            ScreenQueueInstaller.Install(this.Container);
            this.Container.Unbind<ISignalBus>();
            this.Container.Bind<ISignalBus>().To<Observer>().AsSingle().NonLazy();
            UnitTestInstaller<ObserverTest>.Install(this.Container);
            
        }
    }
}