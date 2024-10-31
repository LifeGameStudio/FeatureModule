namespace GameModule.Shop
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.RewardHandle;
    using GameModule.Condition;
    using GameModule.Shop.Cost;

    public class FeatureTransactionService
    {
        private readonly List<ICost>          costs;
        private readonly List<ICondition>     conditions;
        private readonly FeatureRewardHandler featureRewardHandler;

        public FeatureTransactionService(List<ICost> costs, List<ICondition> conditions, FeatureRewardHandler featureRewardHandler)
        {
            this.costs                = costs;
            this.conditions           = conditions;
            this.featureRewardHandler = featureRewardHandler;
        }

        /// <summary>
        /// Checks if all the conditions required for a transaction are met.
        /// It retrieves the conditions from the transaction record and verifies if
        /// each condition is satisfied using the current conditions available in the service.
        /// </summary>
        /// <param name="transactionRecord">The transaction record that contains the required conditions.</param>
        /// <returns>True if all required conditions are met, otherwise false.</returns>
        public bool CheckCondition(ITransactionRecord transactionRecord)
        {
            // Get the list of required conditions from the transactionRecord
            var listRequiredConditions = transactionRecord.GetConditions();

            // Check if all required conditions are met
            return listRequiredConditions.All(requiredCondition =>
            {
                // Find a matching condition in `this.conditions` by ConditionType
                var matchingCondition = this.conditions.FirstOrDefault(condition =>
                    condition.ConditionId.Equals(requiredCondition.ConditionType));

                // If a matching condition is found, check if it's met using the required ConditionParam
                return matchingCondition != null && matchingCondition.IsMet(requiredCondition.ConditionParam);
            });
        }

        /// <summary>
        /// Processes the purchase for a given transaction. 
        /// It checks if all costs can be afforded, applies the purchase if possible, 
        /// and then executes reward distribution.
        /// </summary>
        /// <param name="transactionRecord">The transaction record that includes cost and reward details.</param>
        /// <returns>True if the purchase is successful and rewards are distributed, otherwise false.</returns>
        public async UniTask<bool> DoPurchase(ITransactionRecord transactionRecord)
        {
            // if any item can not afford, return false
            if ((from item in this.costs
                    let matchingCost = transactionRecord.GetCosts().FirstOrDefault(cost => cost.CostType.Equals(item.Id))
                    where matchingCost != null
                    where !item.CanAfford(matchingCost)
                    select item).Any())
            {
                return false;
            }

            List<UniTask<bool>> purchaseTask = new();

            // Check all if any cost cannot purchase
            foreach (var item in this.costs)
            {
                // iterate through the list of costs and find the matching cost by CostId
                var matchingCost = transactionRecord.GetCosts().FirstOrDefault(cost => cost.CostType.Equals(item.Id));

                if (matchingCost == null)
                    continue;

                // If a matching cost is found, try purchase it
                purchaseTask.Add(item.Purchase(matchingCost));
            }

            var x = await UniTask.WhenAll(purchaseTask);

            var result = x.All(item => item);

            if (result)
                this.featureRewardHandler.AddRewards(transactionRecord.GetDeliverables(), null);

            return result;
        }
    }
}