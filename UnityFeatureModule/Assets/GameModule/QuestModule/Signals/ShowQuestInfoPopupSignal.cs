namespace GameModule.QuestModule.Signals
{
    using GameModule.QuestModule.Blueprints.Base.Interfaces;

    public class ShowQuestInfoPopupSignal
    {
        public IBaseQuestRecord QuestRecord;
        public string           NpcId;
    }
}