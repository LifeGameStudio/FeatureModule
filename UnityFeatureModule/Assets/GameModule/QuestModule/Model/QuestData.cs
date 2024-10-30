namespace GameModule.QuestModule.Model
{
    using System;
    using System.Collections.Generic;
    using FeatureTemplate.Scripts.InterfacesAndEnumCommon;
    using GameFoundation.Scripts.Interfaces;
    using global::Blueprints;
    using Newtonsoft.Json;

    public class QuestJournal : IFeatureLocalData, ILocalData
    {
        public Dictionary<string, QuestLog>                Quests         = new();
        public Dictionary<string, Dictionary<string, int>> TrackingCached = new();

        public Type ControllerType => typeof(QuestManager);

        public void Init() { }
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
        [JsonIgnore] public QuestRecord       QuestRecord;
    }

    [Serializable]
    public class TaskLog
    {
        public              int        CountRequirementOption;
        public List<RequirementProgress> Progress = new();
        public QuestStatus               TaskStatus;

        [JsonIgnore] public TaskRecord TaskRecord;
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
        Achievement
    }
}