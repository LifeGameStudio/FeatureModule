namespace GameModule.QuestModule.QuestMatcher
{
    using GameModule.QuestModule.Signals;

    public class CheckLevelItemMatcher : BaseTrackingQuestRequirementMatcher<int>
    {
        public override string Id => "check_level_equal";

        protected override bool IsMatch(int data, TrackingQuestSignal obj) { return data == obj.RequirementValue; }
    }
}