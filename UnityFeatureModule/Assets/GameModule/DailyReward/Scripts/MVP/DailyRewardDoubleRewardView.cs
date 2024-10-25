namespace GameModule.DailyReward.Scripts.MVP
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.MonoUltils;
    using FeatureTemplate.Scripts.Services;
    using FeatureTemplate.Scripts.Services.Ads;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.DailyReward.Scripts;
    using GameModule.DailyReward.Scripts.Signals;
    using UnityEngine;
    using Zenject;

    public class DailyRewardDoubleRewardView : DailyRewardBaseView
    {
        public                  FeatureButtonView       Claimx2Button;
        [SerializeField] public List<RewardSlotAdapter> adapters;
    }

    [PopupInfo("DailyRewardViewx2")]
    public class DailyRewardDoubleRewardPresenter : DailyRewardBasePresenter
    {
        private readonly FeatureAdsServices          featureAdsServices;
        private          FeatureButtonModel          claimX2FeatureButtonModel;
        private          DailyRewardDoubleRewardView View => (DailyRewardDoubleRewardView)base.View;

        public DailyRewardDoubleRewardPresenter(SignalBus signalBus, ScreenManager screenManager, SceneDirector sceneDirector,
            FeatureAdsServices featureAdsServices) : base(signalBus, screenManager, sceneDirector)
        {
            this.featureAdsServices = featureAdsServices;
        }

        protected override UniTask InternalBindData()
        {
            if (this.DailyRewardService.IsNewDay())
            {
                if (this.Today == 1)
                {
                    this.DailyRewardDataController.ResetDailyReward();
                }
            }
            this.claimX2FeatureButtonModel = new FeatureButtonModel()
            {
                ButtonName      = "Claimx2Button",
                ButtonStatus    = ButtonStatus.On,
                ScreenPresenter = this,
                ScreenViewName  = this.View.name
            };

            this.View.Claimx2Button.InitButtonEvent(this.Claimx2Button, this.claimX2FeatureButtonModel);

            this.LogMessage("Today is " + this.Today);

            this.Reload();

            return UniTask.CompletedTask;
        }

        protected override void OnClaimReward(RewardClaimSignal signal)
        {
            this.Reload();
            this.UpdateUI();
        }

        protected override int Today
        {
            get
            {
                if (this.DailyRewardService.IsNewDay())
                    return this.DailyRewardDataController.LastClaimedDay % this.DailyRewardMiscParamBlueprint.TimeLoop + 1;
                else
                    return this.DailyRewardDataController.LastClaimedDay;
            }
        }

        private void Claimx2Button(FeatureButtonModel obj)
        {
            this.featureAdsServices.ShowRewardedAd("Claimx2_DailyReward", () =>
                {
                    for (var i = 0; i < 2; i++)
                    {
                        this.DailyRewardService.ClaimReward(this.Today, null);
                    }
                }
            );
        }

        private void UpdateUI()
        {
            this.ClaimFeatureButtonModel.ButtonStatus = ButtonStatus.Off;
            this.View.claimButton.RefreshButtonStatus();
            this.claimX2FeatureButtonModel.ButtonStatus = ButtonStatus.Off;
            this.View.Claimx2Button.RefreshButtonStatus();
        }

        protected override UniTask Reload()
        {
            for (var index = 0; index < this.View.adapters.Count; index++)
            {
                var item = this.View.adapters[index];
                item.DayIndex = index + 1;

                item.UpdateClaim(this.DailyRewardDataController.IsClaimed(item.DayIndex));

                item.IsLocked = item.DayIndex > this.Today;
            }

            return UniTask.CompletedTask;
        }
    }
}