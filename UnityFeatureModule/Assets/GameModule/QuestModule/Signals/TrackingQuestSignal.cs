namespace GameModule.QuestModule.Signals
{
    using System.Collections.Generic;

    public class TrackingQuestSignal
    {
        public string       RequirementType  { get; }
        public List<string> RequirementIds   { get; }
        public int          RequirementValue { get; }

        public TrackingQuestSignal(string requirementType, List<string> requirementIds, int requirementValue)
        {
            this.RequirementType  = requirementType;
            this.RequirementIds   = requirementIds;
            this.RequirementValue = requirementValue;
        }
    }
}