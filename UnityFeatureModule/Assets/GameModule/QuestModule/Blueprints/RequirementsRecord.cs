namespace GameModule.QuestModule.Blueprints
{
    using BlueprintFlow.BlueprintReader;

    [CsvHeaderKey("RequirementType")]
    public class RequirementsRecord
    {
        public string RequirementId;
        public int    RequirementValue;
        public string RequirementType;
    }
}