namespace GameModule.QuestModule.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FeatureTemplate.Scripts.InterfacesAndEnumCommon;
    using GameFoundation.Scripts.Interfaces;
    using GameModule.QuestModule.Blueprints.Base;
    using GameModule.QuestModule.Blueprints.Base.Interfaces;
    using Newtonsoft.Json;
    using UserData;

    public class QuestJournal : IFeatureLocalData, ILocalData
    {
        public Dictionary<string, QuestLog> Quests        = new();
        public Dictionary<string, QuestLog> QuestRewarded = new();

        public Dictionary<string, Dictionary<string, int>> TrackingCached = new();

        public bool IsQuestRewarded(string questId, string provideId, QuestProviderType questProviderType)
        {
            // if (questProviderType is QuestProviderType.Daily or QuestProviderType.BattlePass)
            // {
            //     this.QuestRewarded.Remove(questId);
            //
            //     return false;
            // }
            return this.QuestRewarded.FirstOrDefault(x => x.Key == questId && x.Value.ProviderId == provideId).Value != null;
        }
        
        public Type ControllerType => typeof(QuestManager);
    }

    [Serializable]
    public class QuestLog
    {
        public              string            ProviderId;
        public              string            QuestId;
        public              QuestProviderType QuestProviderType;
        public              QuestStatus       QuestStatus;
        public              string            QuestType;
        public              int               CountTaskOption;
        public              List<TaskLog>     TaskProgress = new();
        [JsonIgnore] public IBaseQuestRecord  BaseQuestRecord;
    }

    [Serializable]
    public class TaskLog
    {
        public int                       CountRequirementOption;
        public List<RequirementProgress> Progress = new();
        public QuestStatus               TaskStatus;

        [JsonIgnore] public ITaskRecord TaskRecord;
    }

    public class RequirementProgress
    {
        public string RequirementType;
        public string RequirementId;
        public int    CurrentValue;
        public int    RequiredValue;
        public bool   IsOptional;
    }

    public enum QuestProviderType
    {
        Main,
        Side,
        Achievement,
        Daily,
        Weekly,
        Monthly,
        Seasonal,
        Special,
        BattlePass,
        PlayerLevel,
        Event,
        Custom,
        Custom1,
        Custom2,
        Custom3,
        Custom4,
        Custom5,
        Custom6,
        Custom7,
        Custom8,
        Custom9,
        Custom10,
        Custom11,
        Custom12,
        Custom13,
        Custom14,
        Custom15,
        Custom16,
        Custom17,
        Custom18,
        Custom19,
        Custom20,
    }
}