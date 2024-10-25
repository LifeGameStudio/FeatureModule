namespace GameModule.DailyReward.Scripts
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
    using GameModule.DailyReward.Scripts.Blueprints;
    using GameModule.DailyReward.Scripts.Signals;
    using Zenject;

    // Input T as popup screen
    public class DailyRewardInstaller<T> : Installer<DailyRewardInstaller<T>> where T : IScreenPresenter
    {
        private SignalBus                     signalBus;
        private DailyRewardMiscParamBlueprint dailyRewardMiscParamBlueprint;
        private FeatureDataState              featureDataState;
        private DailyRewardService            dailyRewardService;
        private GameQueueActionContext        gameQueueActionContext;

        public override void InstallBindings()
        {
            this.SignalDeclaration();
            this.Container.BindInterfacesAndSelfTo<DailyRewardService>().AsCached().NonLazy();
            this.signalBus                     = this.Container.Resolve<SignalBus>();
            this.gameQueueActionContext        = this.Container.Resolve<GameQueueActionContext>();
            this.dailyRewardMiscParamBlueprint = this.Container.Resolve<DailyRewardMiscParamBlueprint>();
            this.featureDataState              = this.Container.Resolve<FeatureDataState>();
            this.dailyRewardService            = this.Container.Resolve<DailyRewardService>();
            this.AutoOpenScreen();
        }

        private void AutoOpenScreen() { this.signalBus.Subscribe<ScreenShowSignal>(this.OnScreenShowSignal); }

        protected async void OnScreenShowSignal(ScreenShowSignal screenShowSignal)
        {
            await UniTask.WaitUntil(() => this.featureDataState.IsBlueprintAndLocalDataLoaded);

            if (string.IsNullOrEmpty(this.dailyRewardMiscParamBlueprint.StartOnScreen) || !this.featureDataState.IsBlueprintAndLocalDataLoaded)
            {
                return;
            }

            if (screenShowSignal.ScreenPresenter.GetType().Name != this.dailyRewardMiscParamBlueprint.StartOnScreen) return;

            if (!this.dailyRewardService.IsNewDay()) return;
            this.gameQueueActionContext.AddScreenToQueueAction<T>();
        }

        private void SignalDeclaration() { this.Container.DeclareSignal<RewardClaimSignal>(); }
    }
}