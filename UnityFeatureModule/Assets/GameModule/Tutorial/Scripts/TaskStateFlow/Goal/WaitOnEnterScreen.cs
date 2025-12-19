namespace GameModule.Tutorial.Scripts.TaskStateFlow.Goal
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Handle;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.Tutorial.Scripts.DataState;
    using R3;
    using Zenject;

    public class WaitOnEnterScreenTaskGoalData : IActionData
    {
        public string ScreenType;
    }

    public class WaitOnEnterScreen : BaseTaskGoal<WaitOnEnterScreenTaskGoalData>
    {
        private readonly ScreenManager                 screenManager;
        private          TutorialTaskDataState         currentTaskDataState;
        private          WaitOnEnterScreenTaskGoalData currentModel;

        public WaitOnEnterScreen(ISignalBus signalBus, ScreenManager screenManager) : base(signalBus) { this.screenManager = screenManager; }

        public override string Id => "wait_on_enter_screen";

        public override void Initialize()
        {
            base.Initialize();
            this.screenManager.CurrentActiveScreen.Subscribe(this.OnScreenOpen);
        }

        private void OnScreenOpen(IScreenPresenter screenPresenter)
        {
            if (screenPresenter == null || this.currentTaskDataState == null)
                return;

            if (screenPresenter.GetType().Name.Equals(this.currentModel.ScreenType))
            {
                this.CompleteCurrentTask(this.currentTaskDataState);
                this.currentTaskDataState = null;
                this.currentModel         = null;
            }
        }

        protected override bool IsMet()
        {
            return this.screenManager.CurrentActiveScreen is { Value: not null } &&
                   this.screenManager.CurrentActiveScreen.Value.GetType().Name.Equals(this.currentModel.ScreenType);
        }

        protected override UniTask ProcessInternal(TutorialTaskDataState taskDataState, WaitOnEnterScreenTaskGoalData model)
        {
            this.currentModel         = model;
            this.currentTaskDataState = taskDataState;

            if (!this.IsMet()) return UniTask.CompletedTask;
            this.CompleteCurrentTask(this.currentTaskDataState);
            this.currentTaskDataState = null;
            this.currentModel         = null;

            return UniTask.CompletedTask;
        }
    }
}