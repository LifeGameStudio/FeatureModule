namespace GameModule.QuestModule.Provider
{
    using System.Collections.Generic;
    using System.Linq;
    using GameModule.QuestModule.Model;
    using GameModule.QuestModule.Signals;
    using UnityEngine.Scripting;
    using UserData;
    using Zenject;

    public class QuestProviderServices : IInitializable
    {
        private readonly ISignalBus                                    signalBus;
        private readonly QuestManager                                  questManager;
        private          Dictionary<QuestProviderType, IQuestProvider> questProviders;

        [Preserve]
        public QuestProviderServices(List<IQuestProvider> questProviders, ISignalBus signalBus, QuestManager questManager)
        {
            this.signalBus      = signalBus;
            this.questManager   = questManager;
            this.questProviders = questProviders.ToDictionary(x => x.QuestProviderType);
        }

        public void GiveQuestToUser(string questId, string providerId, QuestProviderType questProviderType)
        {
            if (this.questProviders.TryGetValue(questProviderType, out var questProvider))
            {
                questProvider.GiveNewQuest(questId, providerId, questProviderType);
            }
        }

        public void StartQuest(QuestProviderType questProviderType, string questId, string providerId) { this.questProviders[questProviderType].CheckToStartQuest(questId, providerId); }

        public void Initialize() { this.signalBus.Subscribe<QuestChangeStatusSignal>(this.OnQuestChangeStatus); }

        protected virtual void OnQuestChangeStatus(QuestChangeStatusSignal obj) { }

        public void SetupTaskContext(TaskLog taskLog, QuestProviderType questProviderType) { this.questProviders[questProviderType].SetupTaskContext(taskLog, questProviderType); }
    }
}