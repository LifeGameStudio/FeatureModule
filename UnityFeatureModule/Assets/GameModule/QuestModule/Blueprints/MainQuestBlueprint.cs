namespace GameModule.QuestModule.Blueprints
{
    using System.Collections.Generic;
    using BlueprintFlow.BlueprintReader;
    using GameModule.QuestModule.Blueprints.Base;
    using GameModule.QuestModule.Blueprints.Base.Interfaces;

    [BlueprintReader("MainQuest")]
    public class MainQuestBlueprint : GenericBlueprintReaderByRow<string, MainQuestRecord>
    {
    }

    public class MainQuestRecord : BaseQuestRecord, IQuestRecord
    {
        public BlueprintByRow<MainQuestTaskRecord> TaskRecords { get; set; }

        public override List<ITaskRecord> Tasks()
        {
            var result = new List<ITaskRecord>();
            result.AddRange(this.TaskRecords);

            return result;
        }

        public string QuestDescription  { get; set; }
        public string GotoQuestDeepLink { get; set; }
    }

    public class MainQuestTaskRecord : BaseTaskRecord
    {
        public BlueprintByRow<QuestRequirementWithCondition> Requirements { get; set; }

        public override List<IQuestRequirement> RequirementRecords()
        {
            var result = new List<IQuestRequirement>();
            result.AddRange(this.Requirements);

            return result;
        }
    }
}