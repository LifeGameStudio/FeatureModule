namespace GameModule.QuestModule.Blueprints.Base
{
    using System;
    using BlueprintFlow.BlueprintReader;
    using GameModule.QuestModule.Blueprints;
    using GameModule.QuestModule.Blueprints.Base.Interfaces;

    [CsvHeaderKey("Id")]
    [Serializable]
    public abstract class BaseQuestRecord : IBaseQuestRecord
    {
        public string Id              { get; set; }
        public int    QuestIndex      { get; set; }
        public string QuestType       { get; set; }
        public string QuestIcon       { get; set; }
        public int    CountTaskOption { get; set; }

        public          BlueprintByRow<QuestRewardRecord> QuestRewardRecords { get; set; }
        public abstract BlueprintByRow<ITaskRecord>       Tasks();

    }

    [CsvHeaderKey("TaskId")]
    [Serializable]
    public abstract class BaseTaskRecord : ITaskRecord
    {
        public          string                            TaskId                { get; set; }
        public          bool                              TaskOption            { get; set; }
        public          int                               CoutRequirementOption { get; set; }
        public          string                            TaskIcon              { get; set; }
        public          string                            GoToTaskDeepLink      { get; set; }
        public          string                            Description           { get; set; }
        public          string                            TaskName              { get; set; }

        public BlueprintByRow<RewardRecord>     RewardRecords         { get; set; }

        public abstract BlueprintByRow<IQuestRequirement> RequirementRecords();
    }


    public class TaskRecord:BaseTaskRecord
    {
        public BlueprintByRow<QuestRequirement>      Requirements { get; set; }

        public override BlueprintByRow<IQuestRequirement> RequirementRecords()
        {
            var reult = new BlueprintByRow<IQuestRequirement>();
            reult.AddRange(this.Requirements);

            return reult;
        }
    }

    public class QuestRequirement : RequirementsRecord, IQuestRequirement
    {
        public string TrackingType { get; set; }

        public bool RequirementOption { get; set; }

        public string GetRequirementId() => this.RequirementId;

        public int GetRequirementValue() => this.RequirementValue;

        public string GetRequirementType() => this.RequirementType;
    }

    public class QuestRequirementWithCondition : QuestRequirement, IQuestRequirementWithCondition
    {
        public string RequirementConditionId   { get; set; }
        public string RequirementConditionData { get; set; }
    }

    [CsvHeaderKey("TaskState")]
    [Serializable]
    public class TaskSate
    {
        public QuestStatus                        TaskState;
        public BlueprintByRow<QuestContextRecord> QuestContext;
    }

    [CsvHeaderKey("QuestContextType")]
    public class QuestContextRecord
    {
        public string QuestContextType;
        public string QuestContextData;
    }

    [CsvHeaderKey("TaskRewardId")]
    [Serializable]
    public class RewardRecord
    {
        public string TaskRewardId;
        public string TaskRewardType;
        public int    TaskRewardValue;
    }

    [CsvHeaderKey("QuestRewardId")]
    [Serializable]
    public class QuestRewardRecord
    {
        public string QuestRewardId    { get; set; }
        public string QuestRewardType  { get; set; }
        public int    QuestRewardValue { get; set; }
    }

    public enum TrackingType
    {
        Total,
        InQuest
    }

    public enum QuestStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        Rewarded
    }
}