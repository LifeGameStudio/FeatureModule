namespace GameModule.DailyReward.Scripts.Blueprints
{
    using System;
    using System.Collections.Generic;
    using BlueprintFlow.BlueprintReader;
    using FeatureTemplate.Scripts.RewardHandle;

    [BlueprintReader("DailyRewardData")]
    public class FeatureDailyRewardBlueprint : GenericBlueprintReaderByRow<string, DailyRewardRecord>
    {
        public List<Reward> GetRewards(string day) { return this[day].Rewards; }
    }

    [CsvHeaderKey("Id")]
    public class DailyRewardRecord
    {
        public string Id             { get; set; }
        public string DayIndex       { get; set; }
        public string Title          { get; set; }
        public string Description    { get; set; }
        public bool   ShowAdsNextDay { get; set; }

        public BlueprintByRow<Reward> Rewards { get; set; }
    }

    [Serializable]
    [CsvHeaderKey("RId")]
    public class Reward : RewardRecord
    {
        public string RId      { get; set; }
        public string IconPath { get; set; }
    }
}