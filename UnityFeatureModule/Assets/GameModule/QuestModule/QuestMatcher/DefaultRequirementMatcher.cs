namespace GameModule.QuestModule.QuestMatcher
{
    using GameModule.QuestModule.Blueprints.Base.Interfaces;
    using GameModule.QuestModule.Signals;

    public class DefaultRequirementMatcher : BaseTrackingQuestRequirementMatcher
    {
        public override string Id                                                                      => "";
        public override bool   IsMatch(IQuestRequirementWithCondition record, TrackingQuestSignal obj) { return true; }
    }
}