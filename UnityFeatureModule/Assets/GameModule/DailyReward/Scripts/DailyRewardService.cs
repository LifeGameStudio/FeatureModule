namespace GameModule.DailyReward.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FeatureTemplate.Scripts.RewardHandle;
    using FeatureTemplate.Scripts.Services;
    using GameModule.DailyReward.Scripts.Blueprints;
    using GameModule.DailyReward.Scripts.Data;
    using GameModule.DailyReward.Scripts.Signals;
    using UnityEngine;
    using Zenject;

    public class DailyRewardService
    {
        private readonly DailyRewardDataController     dailyRewardDataController;
        private readonly FeatureRewardHandler          featureRewardHandler;
        private readonly DailyRewardMiscParamBlueprint dailyRewardMiscParamBlueprint;
        private readonly FeatureDailyRewardBlueprint   featureDailyRewardBlueprint;
        private readonly SignalBus                     signalBus;

        // Constructor to initialize
        public DailyRewardService(DailyRewardDataController dailyRewardDataController, FeatureRewardHandler featureRewardHandler,
            DailyRewardMiscParamBlueprint dailyRewardMiscParamBlueprint,
            FeatureDailyRewardBlueprint featureDailyRewardBlueprint, SignalBus signalBus)
        {
            this.dailyRewardDataController     = dailyRewardDataController;
            this.featureRewardHandler          = featureRewardHandler;
            this.dailyRewardMiscParamBlueprint = dailyRewardMiscParamBlueprint;
            this.featureDailyRewardBlueprint   = featureDailyRewardBlueprint;
            this.signalBus                     = signalBus;
        }

        public bool UnlockNextDayReward()
        {
            if (!this.featureDailyRewardBlueprint[this.dailyRewardDataController.Today.ToString()].ShowAdsNextDay)
            {
                return false;
            }

            this.dailyRewardDataController.DayOffSet++;

            return true;
        }

        // Claim reward if eligible
        public void ClaimReward(int day, GameObject source) // Huy 3/10/2024: Hi vong sau nay khong phai sua base @@
        {
            // Trigger the onRewardClaim event, passing the currentDay as argument
            var currentRewards = this.featureDailyRewardBlueprint.GetRewards(day.ToString());

            var list = currentRewards.Select(rewardBlueprintData => new RewardRecord()
                { RewardType = rewardBlueprintData.RewardType, RewardValue = rewardBlueprintData.RewardValue, RewardId = rewardBlueprintData.RewardId }).Cast<IRewardRecord>().ToList();

            this.featureRewardHandler.AddRewards(list, source);
            // Update claim time and save it
            this.dailyRewardDataController.LastSavedDay   = DateTime.UtcNow;
            this.dailyRewardDataController.LastClaimedDay = day;
            this.LogMessage("Claim reward at day " + day, Color.green);
            this.dailyRewardDataController.ClaimReward(day);

            this.signalBus.Fire(new RewardClaimSignal()
            {
                Reward = currentRewards.Cast<IRewardRecord>().ToList(),
                Day    = day
            });
        }

        public List<Reward> ReadRewardsAtDayOffSet(int dayOffset)
        {
            var today = this.dailyRewardDataController.Today;

            var rewardAtDay = today + dayOffset;

            if (rewardAtDay > this.dailyRewardMiscParamBlueprint.TimeLoop)
            {
                rewardAtDay %= this.dailyRewardMiscParamBlueprint.TimeLoop;
            }

            return this.featureDailyRewardBlueprint.GetRewards(rewardAtDay.ToString());
        }

        public List<Reward> ReadRewardsAtDay(int day)
        {
            var rewardAtDay = day;

            if (rewardAtDay > this.dailyRewardMiscParamBlueprint.TimeLoop)
            {
                rewardAtDay %= this.dailyRewardMiscParamBlueprint.TimeLoop;
            }

            return this.featureDailyRewardBlueprint.GetRewards(rewardAtDay.ToString());
        }

        // Check if it's a new day based on last claim
        public bool IsNewDay()
        {
            var currentUtcTime = DateTime.UtcNow;
            var lastClaimDate  = this.dailyRewardDataController.LastSavedDay.Date;
            var currentDate    = currentUtcTime.Date;

            // If the current day (at UTC) is different from the last claim day, it's a new day
            return currentDate > lastClaimDate;
        }
    }
}