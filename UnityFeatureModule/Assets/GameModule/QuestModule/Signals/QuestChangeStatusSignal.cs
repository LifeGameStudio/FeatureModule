namespace GameModule.QuestModule.Signals
{
    using GameModule.QuestModule.Model;

    public class QuestChangeStatusSignal
    {
        public QuestLog QuestLog;

        public QuestChangeStatusSignal(QuestLog questLog) { this.QuestLog = questLog; }
    }
}