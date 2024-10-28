namespace Blueprints
{
    using System;
    using System.Collections.Generic;
    using BlueprintFlow.BlueprintReader;

    [BlueprintReader("MainQuest")]
    public class MainQuestBlueprint : GenericBlueprintReaderByRow<string, QuestRecord>
    {
    }

    [CsvHeaderKey("Id")]
    [Serializable]
    public class QuestRecord
    {
        public string Id;

        //public string                            QuestName;
        public string                            QuestIcon;
        public string                            QuestType;
        public BlueprintByRow<QuestRewardRecord> QuestRewardRecords;
        public BlueprintByRow<TaskRecord>        Tasks;
    }

    [CsvHeaderKey("TaskId")]
    [Serializable]
    public class TaskRecord
    {
        public string                                 TaskId;
        public BlueprintByRow<QuestStatus, TaskSate>  TaskSates;
        public string                                 Description;
        public string                                 TaskName;
        public BlueprintByRow<QuestRequirementRecord> RequirementRecords;
        public BlueprintByRow<RewardRecord>           RewardRecords;
    }

    public class QuestRequirementRecord : RequirementsRecord
    {
        public string TrackingType;
    }

    [CsvHeaderKey("RequirementType")]
    public class RequirementsRecord
    {
        public string RequirementId;
        public int    RequirementValue;
        public string RequirementType;
    }

    [CsvHeaderKey("TaskState")]
    [Serializable]
    public class TaskSate
    {
        public QuestStatus  TaskState;
        public List<string> QuestContextIds;
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
        public string QuestRewardId;
        public string QuestRewardType;
        public int    QuestRewardValue;
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
        MeetNpc,
        Completed,
        Failed,
        Rewarded
    }
}