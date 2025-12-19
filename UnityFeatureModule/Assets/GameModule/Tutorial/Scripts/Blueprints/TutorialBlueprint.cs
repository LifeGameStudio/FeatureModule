

namespace GameModule.Tutorial.Scripts.Blueprints
{
    using System;
    using BlueprintFlow.BlueprintReader;
#if TUTORIAL_ENABLE
    [BlueprintReader("Tutorial")]
    public class TutorialBlueprint : GenericBlueprintReaderByRow<int, TutorialRecord>
    {
    }
#endif

    [CsvHeaderKey("Id")]
    public class TutorialRecord
    {
        public int                                  Id          { get; set; }
        public int                                     Order       { get; set; }
        public BlueprintByRow<int, TutorialTaskRecord> TaskRecords { get; set; }
    }

    [CsvHeaderKey("TaskId")]
    [Serializable]
    public class TutorialTaskRecord
    {
        public int    TaskId              { get; set; }
        public string TaskName            { get; set; }
        public string TaskDescription     { get; set; }
        public string TaskRequirement     { get; set; }
        public string TaskRequirementData { get; set; }

        public string                           TaskGoalType      { get; set; }
        public string                           TaskGoalData      { get; set; }
        public BlueprintByRow<TaskActiveRecord> TaskActiveRecords { get; set; }

        public BlueprintByRow<TaskCompleteRecord> TaskCompleteRecords { get; set; }
        public bool                               IsTaskOptional      { get; set; }
    }

    [Serializable]
    public class TaskActiveRecord
    {
        public string TaskActiveType { get; set; }
        public string TaskActiveData { get; set; }
    }

    [Serializable]
    public class TaskCompleteRecord
    {
        public string TaskCompleteType { get; set; }
        public string TaskCompleteData { get; set; }
    }
}