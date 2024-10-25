namespace GameModule.DailyReward.Scripts.MVP
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.MonoUltils;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.DailyReward.Scripts;
    using GameModule.DailyReward.Scripts.Blueprints;
    using GameModule.DailyReward.Scripts.RewardSlotItem;
    using GameModule.DailyReward.Scripts.Signals;
    using UnityEngine;
    using Zenject;
    using Color = UnityEngine.Color;

    public class DailyRewardView : DailyRewardBaseView
    {
        [SerializeField] public List<RewardSlotAdapter> adapters;
    }

    [PopupInfo(nameof(DailyRewardView), isOverlay: true)]
    public class DailyRewardPresenter : DailyRewardBasePresenter
    {
        private readonly FeatureDailyRewardBlueprint featureDailyRewardBlueprint;
        private readonly DiContainer                 diContainer;
        public           DailyRewardView             View => (DailyRewardView)base.View;

        private FeatureButtonModel claimFeatureButtonModel;

        public DailyRewardPresenter(SignalBus signalBus, FeatureDailyRewardBlueprint featureDailyRewardBlueprint,
            ScreenManager screenManager, SceneDirector sceneDirector, DiContainer diContainer) : base(signalBus, screenManager, sceneDirector)
        {
            this.featureDailyRewardBlueprint = featureDailyRewardBlueprint;
            this.diContainer                 = diContainer;
        }

        protected override async UniTask InternalBindData()
        {
            // ReSharper disable once PossibleLossOfFraction
            if (this.DailyRewardService.IsNewDay())
                this.DailyRewardDataController.Page = Mathf.FloorToInt(this.DailyRewardDataController.Today / this.DailyRewardMiscParamBlueprint.TimeLoop);

            await this.Reload();

            this.CheckButtonStatus();
        }

        protected override void OnClaimReward(RewardClaimSignal signal) { }

        protected override int Today => this.DailyRewardDataController.Today;

        /// <summary>
        /// Claim reward at a specific day.
        /// </summary>
        /// <returns></returns>
        protected void ClaimReward(int day, GameObject source) { this.DailyRewardService.ClaimReward(day, source); }

        protected override UniTask Reload()
        {
            // Calculate the current page start based on today's day and the TimeLoop.
            // For example, if today is day 9 and TimeLoop is 7, the page should show days 8-14.
            var startDay = Mathf.FloorToInt((this.DailyRewardDataController.Today - 1) / this.DailyRewardMiscParamBlueprint.TimeLoop)
                * this.DailyRewardMiscParamBlueprint.TimeLoop + 1;

            // Ensure we're showing a valid range of days, adjusting the Page property if necessary.
            this.DailyRewardDataController.Page = (this.DailyRewardDataController.Today - 1) / this.DailyRewardMiscParamBlueprint.TimeLoop;

            for (var index = 0; index < this.View.adapters.Count; index++)
            {
                var item = this.View.adapters[index];

                // Calculate the actual DayIndex for this slot (within the current page).
                item.DayIndex = startDay + index;

                // Skip if the calculated DayIndex is 0 (this won't happen as long as Today >= 1).
                if (item.DayIndex == 0)
                {
                    continue; // Skip invalid day.
                }

                item.InitButton(_ => this.UnlockReward());

                // Lock the reward if the DayIndex is greater than today's day.
                item.IsLocked = item.DayIndex > this.DailyRewardDataController.Today;

                // Calculate which reward to display using modulo for looping.
                var rewardDayIndex = (item.DayIndex - 1) % this.DailyRewardMiscParamBlueprint.TimeLoop + 1;

                // Set lock icon based on the current day and blueprint data.
                if (this.featureDailyRewardBlueprint.Count >= rewardDayIndex)
                {
                    item.SetLockIcon(item.DayIndex == this.DailyRewardDataController.Today + 1 &&
                                     this.featureDailyRewardBlueprint[rewardDayIndex.ToString()].ShowAdsNextDay);
                }
                else
                {
                    item.SetLockIcon(item.DayIndex == this.DailyRewardDataController.Today + 1 &&
                                     this.featureDailyRewardBlueprint[(rewardDayIndex).ToString()].ShowAdsNextDay);
                }

                // Get the rewards for the current day (actual day, but load based on looping).

                var iconList = this.GetRewardsAt(index + 1).Select(x =>
                    new RewardSlotModel()
                    {
                        Reward = x
                    }
                ).ToList();

                this.View.adapters[index].InitItemAdapter(iconList, this.diContainer).Forget();
            }

            return UniTask.CompletedTask;
        }

        private async void UnlockReward()
        {
            this.LogMessage("Unlock next day reward", Color.green);
            this.DailyRewardService.UnlockNextDayReward();
            await this.Reload();
            this.CheckButtonStatus();
        }

        private void CheckButtonStatus()
        {
            this.claimFeatureButtonModel.ButtonStatus =
                this.View.adapters.Any(item => !item.IsLocked && !this.DailyRewardDataController.IsClaimed(item.DayIndex)) ? ButtonStatus.On : ButtonStatus.Off;

            foreach (var item in this.View.adapters.Where(x => this.DailyRewardDataController.IsClaimed(x.DayIndex)))
            {
                item.UpdateClaim(true);
            }

            this.View.claimButton.RefreshButtonStatus();
        }

        protected override void ClaimButtonClick(FeatureButtonModel model)
        {
            if (model.ButtonStatus == ButtonStatus.Off)
            {
                return;
            }
            
            foreach (var item in this.View.adapters.Where(item => !item.IsLocked && !this.DailyRewardDataController.IsClaimed(item.DayIndex)))
            {
                this.ClaimReward(item.DayIndex, item.gameObject);
            }

            this.CheckButtonStatus();
        }

        /// <summary>
        /// Read the reward at a specific day.
        /// </summary>
        /// <param name="dayOffset">The offset to that day calculate since today.</param>
        /// <returns></returns>
        private List<Reward> GetRewardsAt(int dayOffset) { return this.DailyRewardService.ReadRewardsAtDay(dayOffset); }
    }
}