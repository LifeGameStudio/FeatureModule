namespace GameModule.QuestModule.QuestMatcher
{
    using GameModule.QuestModule.Blueprints.Base.Interfaces;
    using GameModule.QuestModule.Signals;

    public interface ITrackingQuestRequirementMatcher
    {
        string Id { get; }
        bool   IsMatch(IQuestRequirementWithCondition record, TrackingQuestSignal obj);
    }
}