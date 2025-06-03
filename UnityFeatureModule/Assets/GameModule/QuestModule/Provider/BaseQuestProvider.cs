namespace GameModule.QuestModule.Provider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Handle;
    using FeatureTemplate.Scripts.Services;
    using GameModule.QuestModule.Blueprints;
    using GameModule.QuestModule.Model;
    using UserData;
    using Zenject;

    public interface IQuestProvider
    {
        QuestProviderType QuestProviderType { get; }
        QuestRecord       GetQuestRecord(string questId, string providerId);
        void              GiveNewQuest(string questId, string providerId, QuestProviderType questProviderType);
        void              CheckToStartQuest(string questId, string providerId);
        QuestRecord       GetNextQuest(string lastMainQuestId);
        void              SetupTaskContext(TaskLog taskLog, QuestProviderType questProviderType);
        void              CheckToStartAllTaskOfQuest(string questId, string providerId);
    }

    public abstract class BaseQuestProvider : IQuestProvider, IInitializable, IDisposable
    {
        [Inject] private   FeatureDataState featureDataState;
        protected readonly QuestManager     QuestManager;

        public abstract QuestProviderType                 QuestProviderType { get; }
        private         Dictionary<string, IActionHandle> questContexts;

        protected BaseQuestProvider(QuestManager questManager, List<IActionHandle> questContexts)
        {
            this.QuestManager  = questManager;
            this.questContexts = questContexts.ToDictionary(x => x.Id);
        }

        public abstract QuestRecord GetQuestRecord(string questId, string providerId);

        public void GiveNewQuest(string questId, string providerId, QuestProviderType questProviderType)
        {
            if (this.QuestProviderType != questProviderType)
                return;

            var questRecord = this.GetQuestRecord(questId, providerId);
            this.QuestManager.CheckToAddNewQuest(questId, providerId, questProviderType, questRecord);
            this.QuestManager.UpdateQuestStatus(providerId, questId, QuestStatus.NotStarted);
        }

        public virtual void CheckToStartQuest(string questId, string providerId)
        {
            var questInfo = this.QuestManager.CheckToAddNewQuest(questId, providerId, this.QuestProviderType, this.GetQuestRecord(questId, providerId));

            switch (questInfo.QuestStatus)
            {
                case QuestStatus.Completed or QuestStatus.Rewarded or QuestStatus.InProgress:
                    return;
                case QuestStatus.NotStarted:
                    this.QuestManager.UpdateQuestStatus(providerId, questId, QuestStatus.InProgress);
                    this.SetupContext(questInfo.TaskProgress.FirstOrDefault(x => x.TaskStatus == QuestStatus.InProgress));

                    break;
            }
        }

        public abstract QuestRecord GetNextQuest(string lastMainQuestId);

        public void SetupTaskContext(TaskLog taskLog, QuestProviderType questProviderType)
        {
            if (this.QuestProviderType != questProviderType)
                return;

            this.SetupContext(taskLog);
        }

        public virtual void CheckToStartAllTaskOfQuest(string questId, string providerId) { }

        /// <summary>
        /// Setup Context at Each Task
        /// </summary>
        /// <param name="taskLog"></param>
        public virtual void SetupContext(TaskLog taskLog)
        {
            if (taskLog.TaskRecord.TaskSates.TryGetValue(taskLog.TaskStatus, out var questContext))
            {
                foreach (var contextRecord in questContext.QuestContext)
                {
                    if (this.questContexts.TryGetValue(contextRecord.QuestContextType, out var context))
                    {
                        context.Execute(null, contextRecord.QuestContextData);
                    }
                }
            }
        }

        public async void Initialize()
        {
            await UniTask.WaitUntil(() => this.featureDataState.IsBlueprintAndLocalDataLoaded);
            await this.QuestManager.LoadRecord(this);
            await this.InitInternal();
        }

        protected virtual async UniTask InitInternal() { }

        public virtual void Dispose() { }
    }
}