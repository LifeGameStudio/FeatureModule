namespace GameModule.QuestModule.Blueprints
{
    using BlueprintFlow.BlueprintReader;
    using GameModule.QuestModule.Blueprints.Base;
    using GameModule.QuestModule.Blueprints.Base.Interfaces;

    [BlueprintReader("MainQuest")]
    public class MainQuestBlueprint : GenericBlueprintReaderByRow<string, MainQuestRecord>
    {
    }

    public class MainQuestRecord : BaseQuestRecord
    {
        public BlueprintByRow<MainQuestTaskRecord> TaskRecords { get; set; }

        public override BlueprintByRow<ITaskRecord> Tasks()
        {
            var result = new BlueprintByRow<ITaskRecord>();
            result.AddRange(this.TaskRecords);

            return result;
        }
    }

    public class MainQuestTaskRecord : BaseTaskRecord
    {
        public BlueprintByRow<QuestRequirementWithCondition> Requirements { get; set; }

        public override BlueprintByRow<IQuestRequirement> RequirementRecords()
        {
            var result = new BlueprintByRow<IQuestRequirement>();
            result.AddRange(this.Requirements);

            return result;
        }
    }
}