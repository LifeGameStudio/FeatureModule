namespace GameModule.DailyReward.Scripts.MVP
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.MonoUltils;
    using FeatureTemplate.Scripts.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.DailyReward.Scripts;
    using GameModule.DailyReward.Scripts.Blueprints;
    using GameModule.DailyReward.Scripts.Data;
    using GameModule.DailyReward.Scripts.Signals;
    using Zenject;

    public class DailyRewardBaseView : FeatureBasePopupViewTemplate
    {
        public FeatureButtonView exitButton;
        public FeatureButtonView claimButton;
    }

    public abstract class DailyRewardBasePresenter : FeatureBasePopupPresenterTemplate<DailyRewardBaseView>
    {
        [Inject] protected readonly DailyRewardService            DailyRewardService;
        [Inject] protected readonly DailyRewardMiscParamBlueprint DailyRewardMiscParamBlueprint;
        [Inject] protected readonly DailyRewardDataController     DailyRewardDataController;
        protected                   FeatureButtonModel            ClaimFeatureButtonModel;

        public DailyRewardBasePresenter(SignalBus signalBus, ScreenManager screenManager, SceneDirector sceneDirector) : base(signalBus, screenManager,
            sceneDirector)
        {
        }

        public override UniTask BindData()
        {
            this.SignalBus.Subscribe<RewardClaimSignal>(this.OnClaimReward);

            this.View.exitButton.InitButtonEvent(_ => this.ClosePopup(), new FeatureButtonModel()
            {
                ButtonName      = "btn_exit",
                ScreenPresenter = this,
                ScreenViewName  = this.View.name,
                ButtonStatus    = ButtonStatus.On
            });

            this.ClaimFeatureButtonModel = new FeatureButtonModel
            {
                ButtonName      = "btn_claim",
                ScreenPresenter = this,
                ScreenViewName  = this.View.name,
                ButtonStatus    = ButtonStatus.On
            };

            this.View.claimButton.InitButtonEvent(this.ClaimButtonClick, this.ClaimFeatureButtonModel);

            return this.InternalBindData();
        }

        protected virtual void ClaimButtonClick(FeatureButtonModel model)
        {
            if(model.ButtonStatus == ButtonStatus.Off) return;
            this.DailyRewardService.ClaimReward(this.Today, null);
        }

        protected abstract UniTask InternalBindData();

        private void ClosePopup() { this.CloseViewAsync().Forget(); }

        protected abstract void OnClaimReward(RewardClaimSignal rewardClaimSignal);

        protected abstract int Today { get; }

        protected void ClaimTodayReward()
        {
            this.DailyRewardService.ClaimReward(this.Today, null);
            this.Reload().Forget();
        }

        protected abstract UniTask Reload();

        public override void Dispose()
        {
            this.SignalBus.Unsubscribe<RewardClaimSignal>(this.OnClaimReward);
            base.Dispose();
        }
    }
}