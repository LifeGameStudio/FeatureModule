namespace GameModule.QuestModule.Provider
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Handle;
    using GameModule.QuestModule.Blueprints;
    using GameModule.QuestModule.Model;
    using UnityEngine;
    using UserData;

    public class MainQuestProvider : BaseQuestProvider
    {
        private readonly MainQuestBlueprint mainQuestBlueprint;

        public MainQuestProvider(QuestManager questManager, MainQuestBlueprint mainQuestBlueprint, List<IActionHandle> questContexts) : base(questManager,
            questContexts)
        {
            this.mainQuestBlueprint = mainQuestBlueprint;
        }

        public override QuestProviderType QuestProviderType => QuestProviderType.Main;

        public override QuestRecord GetQuestRecord(string questId, string providerId) { return this.mainQuestBlueprint[questId]; }

        protected override UniTask InitInternal()
        {
            foreach (var item in this.mainQuestBlueprint)
            {
                var questLog = this.QuestManager.GetQuest(item.Key, "");

                if (questLog == null)
                {
                    this.GiveNewQuest(item.Key, "", this.QuestProviderType);
                }

                if (questLog != null)
                {
                    continue;
                }

                this.CheckToStartQuest(item.Key, "");
                this.CheckToStartAllTaskOfQuest(item.Key, "");
            }

            return UniTask.CompletedTask;
        }

        public override void CheckToStartAllTaskOfQuest(string questId, string providerId)
        {
            var questLog = this.QuestManager.GetQuest(questId, providerId);

            foreach (var taskLog in questLog.TaskProgress)
            {
                if (taskLog.TaskStatus is QuestStatus.InProgress or QuestStatus.Completed or QuestStatus.Rewarded)
                {
                    continue;
                }

                if (taskLog.TaskRecord.RequirementRecords.First().TrackingType.Equals(nameof(TrackingType.InQuest)) && questLog.TaskProgress.IndexOf(taskLog) > 0)
                {
                    continue;
                }

                this.QuestManager.UpdateTaskStatus(questId, providerId, taskLog.TaskRecord.TaskId, QuestStatus.InProgress);
            }
        }

        public override QuestRecord GetNextQuest(string lastMainQuestId)
        {
            var lastQuestRecord = this.mainQuestBlueprint[lastMainQuestId];
            var nextQuestOrder  = lastQuestRecord.QuestIndex + 1;
            nextQuestOrder = Mathf.Min(nextQuestOrder, this.mainQuestBlueprint.Count - 1);
            var questRecord = this.mainQuestBlueprint.FirstOrDefault(x => x.Value.QuestIndex == nextQuestOrder).Value;

            return questRecord;
        }

        public override void SetupContext(TaskLog taskLog) { }
    }
}