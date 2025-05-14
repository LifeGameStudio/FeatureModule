namespace GameModule.Shop
{
    using System.Collections.Generic;
    using System.Linq;
    using BlueprintFlow.BlueprintReader;
    using FeatureTemplate.Scripts.RewardHandle;
    using GameModule.Condition;

    [BlueprintReader("Shop",blueprintScope:BlueprintScope.Ignore)]
    public class ShopBlueprint : GenericBlueprintReaderByRow<string, ShopRecord>
    {
        public string GetIconPath(ITransactionRecord record)
        {
            return this[record.Id].Icon;
        }
        
        public string GetTitle(ITransactionRecord record)
        {
            return this[record.Id].Title;
        }
    }

    public interface ITransactionRecord
    {
        string Id { get; set; }

        public List<ICostRecord>   GetCosts();
        public List<IConditionRecord> GetConditions();
        public List<IRewardRecord> GetDeliverables();
    }

    [CsvHeaderKey("Id")]
    public class ShopRecord : ITransactionRecord
    {
        public string                         Id           { get; set; }
        public string                         Icon         { get; set; }
        public string                         Title        { get; set; }
        public BlueprintByRow<ShopCondition>  Conditions   { get; set; }
        public BlueprintByRow<ShopCostRecord> Costs        { get; set; }
        public BlueprintByRow<Deliverable>    Deliverables { get; set; }

        public List<ICostRecord> GetCosts() { return this.Costs.Select(d => (ICostRecord)d).ToList(); }

        public List<IConditionRecord> GetConditions() { return this.Conditions.Cast<IConditionRecord>().ToList(); }

        public List<IRewardRecord> GetDeliverables()
        {
            // Assuming Deliverables is an enumerable or collection of Deliverable objects
            return this.Deliverables.Select(d => (IRewardRecord)d).ToList();
        }
    }

    [CsvHeaderKey("CostId")]
    public class ShopCostRecord : ICostRecord
    {
        public string CostId    { get; set; }
        public string CostType  { get; set; }
        public string  CostValue { get; set; }
    }

    public interface ICostRecord
    {
        public string CostId    { get; set; }
        public string CostType  { get; set; }
        public string  CostValue { get; set; }
    }

    [CsvHeaderKey("ConditionId")]
    public class ShopCondition : IConditionRecord
    {
        public string ConditionId    { get; set; }
        public string ConditionType  { get; set; }
        public string ConditionParam { get; set; }
    }

    public class Deliverable : IRewardRecord
    {
        public string RewardId    { get; set; }
        public string RewardType  { get; set; }
        public int    RewardValue { get; set; }
    }
}