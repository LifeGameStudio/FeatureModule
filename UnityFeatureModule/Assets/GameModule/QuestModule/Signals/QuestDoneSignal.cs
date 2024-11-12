namespace GameModule.QuestModule.Signals
{
    using GameModule.QuestModule.Model;

    public class QuestDoneSignal
    {
        public string            QuestId           { get; }
        public string            ProviderId        { get; }
        public QuestProviderType QuestProviderType { get; }

        public QuestDoneSignal(string questId, string providerId, QuestProviderType questProviderType)
        {
            this.QuestId           = questId;
            this.ProviderId        = providerId;
            this.QuestProviderType = questProviderType;
        }
    }
}