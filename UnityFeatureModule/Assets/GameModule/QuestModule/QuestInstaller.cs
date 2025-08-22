namespace GameModule.QuestModule
{
    using GameFoundation.Scripts.Utilities.Extension;
    using GameModule.QuestModule.Provider;
    using GameModule.QuestModule.Signals;
    using Zenject;
    using Zenject.Internal;

    public class QuestInstaller : Installer<QuestInstaller>
    {
        [Preserve]
        public QuestInstaller() { }

        public override void InstallBindings()
        {
            this.Container.DeclareSignal<TrackingQuestSignal>();
            this.Container.DeclareSignal<TaskChangeStatusSignal>();
            this.Container.DeclareSignal<QuestChangeStatusSignal>();
            this.Container.DeclareSignal<RefreshQuestViewSignal>();
            this.Container.DeclareSignal<ShowQuestInfoPopupSignal>();
            this.Container.BindInterfacesAndSelfTo<TrackingQuestServices>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<IQuestProvider>();
            this.Container.BindInterfacesAndSelfTo<QuestProviderServices>().AsCached().NonLazy();
        }
    }
}