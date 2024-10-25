namespace GameModule.DailyReward.Scripts.Signals
{
    using System.Collections.Generic;
    using FeatureTemplate.Scripts.RewardHandle;

    public class RewardClaimSignal
    {
        public List<IRewardRecord> Reward;
        public int          Day;
    }
}