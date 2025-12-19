namespace GameModule.Tutorial.Scripts
{
    using GameFoundation.Scripts.Utilities.Extension;
    using GameModule.Tutorial.Scripts.DataState;
    using GameModule.Tutorial.Scripts.Services;
    using GameModule.Tutorial.Scripts.Signals;
    using GameModule.Tutorial.Scripts.TaskStateFlow;
    using GameModule.Tutorial.Scripts.TaskStateFlow.Requirement;
    using UnityEngine.Scripting;
    using Zenject;

    public class TutorialInstaller : Installer<TutorialInstaller>
    {
        [Preserve]
        public TutorialInstaller() { }

        public override void InstallBindings()
        {
#if TUTORIAL_ENABLE
            this.Container.DeclareSignal<TaskCompleteSignal>();
            this.Container.Bind<TutorialDataState>().AsCached();
            this.Container.BindInterfacesAndSelfTo<TaskActionServices>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<ITaskRequirement>();
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<ITaskStateFlow>();
            this.Container.BindInterfacesAndSelfTo<TutorialServices>().AsCached().NonLazy();
#endif
        }
    }
}