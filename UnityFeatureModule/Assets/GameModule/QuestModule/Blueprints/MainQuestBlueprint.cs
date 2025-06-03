namespace GameModule.QuestModule.Blueprints
{
    using System;
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
        public int    QuestIndex;

        public string                            QuestType;
        public string                            QuestIcon;
        public string                            QuestDescription;
        public int                               CountTaskOption;
        public BlueprintByRow<QuestRewardRecord> QuestRewardRecords;
        public BlueprintByRow<TaskRecord>        Tasks;
        public string                            GotoQuestDeepLink { get; set; }
    }

    [CsvHeaderKey("TaskId")]
    [Serializable]
    public class TaskRecord
    {
        public string                                 TaskId;
        public bool                                   TaskOption;
        public int                                    CoutRequirementOption;
        public string                                 TaskIcon;
        public string                                 GoToTaskDeepLink;
        public BlueprintByRow<QuestStatus, TaskSate>  TaskSates;
        public string                                 Description;
        public string                                 TaskName;
        public BlueprintByRow<QuestRequirementRecord> RequirementRecords;
        public BlueprintByRow<TaskRewardRecord>       RewardRecords;
    }

    public class QuestRequirementRecord : RequirementsRecord
    {
        public string TrackingType;
        public bool   RequirementOption;
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
    public class TaskRewardRecord
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
        Completed,
        Failed,
        Rewarded
    }
}