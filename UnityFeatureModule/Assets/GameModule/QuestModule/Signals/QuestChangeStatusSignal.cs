namespace GameModule.QuestModule.Signals
{
    using GameModule.QuestModule.Model;

    public class QuestChangeStatusSignal
    {
        public QuestLog QuestLog;

        public QuestChangeStatusSignal(QuestLog questLog) { this.QuestLog = questLog; }
    }
    
    public class TaskChangeStatusSignal
    {
        public QuestLog QuestLog;
        public TaskLog  TaskLog;

        public TaskChangeStatusSignal(QuestLog questLog, TaskLog taskLog)
        {
            this.QuestLog = questLog;
            this.TaskLog  = taskLog;
        }
    }
}