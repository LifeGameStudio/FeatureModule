namespace GameModule.QuestModule
{
    using GameFoundation.Scripts.Utilities.Extension;
    using GameModule.QuestModule.Signals;
    using global::QuestModule.Context;
    using global::QuestModule.Provider;
    using Zenject;

    public class QuestInstaller : Installer<QuestInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.DeclareSignal<TrackingQuestSignal>();
            this.Container.DeclareSignal<QuestDoneSignal>();
            this.Container.DeclareSignal<RefreshQuestViewSignal>();
            this.Container.DeclareSignal<ShowQuestInfoPopupSignal>();
            this.Container.BindInterfacesAndSelfTo<TrackingQuestServices>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<IQuestProvider>();
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<IQuestContext>();
            this.Container.BindInterfacesAndSelfTo<QuestProviderServices>().AsCached().NonLazy();
        }
    }
}