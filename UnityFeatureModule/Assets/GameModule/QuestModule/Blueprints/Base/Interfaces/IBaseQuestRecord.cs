namespace GameModule.QuestModule.Blueprints.Base.Interfaces
{
    using BlueprintFlow.BlueprintReader;

    public interface IBaseQuestRecord
    {
        string Id              { get; set; }
        int    QuestIndex      { get; set; }
        string QuestType       { get; set; }
        string QuestIcon       { get; set; }
        int    CountTaskOption { get; set; }

        public BlueprintByRow<QuestRewardRecord> QuestRewardRecords { get; set; }
        public BlueprintByRow<ITaskRecord>       Tasks();
    }

    public interface IQuestRecord : IBaseQuestRecord
    {
        string QuestDescription  { get; set; }
        string GotoQuestDeepLink { get; set; }
    }

    public interface IQuestRewardRecord
    {
        string QuestRewardId    { get; set; }
        string QuestRewardType  { get; set; }
        int    QuestRewardValue { get; set; }
    }

    public interface ITaskRecord
    {
        public string                            TaskId                { get; set; }
        public bool                              TaskOption            { get; set; }
        public int                               CoutRequirementOption { get; set; }
        public string                            TaskIcon              { get; set; }
        public string                            GoToTaskDeepLink      { get; set; }
        public string                            Description           { get; set; }
        public string                            TaskName              { get; set; }
        public BlueprintByRow<IQuestRequirement> RequirementRecords();
        public BlueprintByRow<RewardRecord>      RewardRecords { get; set; }
    }

    public interface ITaskRecordWithContext : ITaskRecord
    {
        public BlueprintByRow<QuestStatus, TaskSate> TaskSates { get; set; }
    }

    public interface IQuestRequirement
    {
        string TrackingType { get; set; }

        bool   RequirementOption { get; set; }
        string GetRequirementId();
        int    GetRequirementValue();
        string GetRequirementType();
    }

    public interface IQuestRequirementWithCondition : IQuestRequirement
    {
        public string RequirementConditionId   { get; set; }
        public string RequirementConditionData { get; set; }
    }
}