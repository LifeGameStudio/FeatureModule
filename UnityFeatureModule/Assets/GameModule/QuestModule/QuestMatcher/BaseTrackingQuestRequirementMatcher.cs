namespace GameModule.QuestModule.QuestMatcher
{
    using System;
    using System.Globalization;
    using GameModule.QuestModule.Blueprints.Base.Interfaces;
    using GameModule.QuestModule.Signals;
    using Newtonsoft.Json;

    public abstract class BaseTrackingQuestRequirementMatcher : ITrackingQuestRequirementMatcher
    {
        public abstract string Id { get; }

        public abstract bool IsMatch(IQuestRequirementWithCondition record, TrackingQuestSignal obj);
    }

    public abstract class BaseTrackingQuestRequirementMatcher<T> : BaseTrackingQuestRequirementMatcher
    {
        public override bool IsMatch(IQuestRequirementWithCondition record, TrackingQuestSignal obj) { return this.IsMatch(this.DeserializeData(record.RequirementConditionData), obj); }

        private T DeserializeData(string data)
        {
            return Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.String => (T)(object)data,
                TypeCode.Int32 => (T)(object)int.Parse(data),
                TypeCode.Single => (T)(object)float.Parse(data, CultureInfo.InvariantCulture),
                TypeCode.Double => (T)(object)double.Parse(data, CultureInfo.InvariantCulture),
                TypeCode.Boolean => (T)(object)bool.Parse(data),
                TypeCode.Int64 => (T)(object)long.Parse(data),
                _ => JsonConvert.DeserializeObject<T>(data)
            };
        }

        protected abstract bool IsMatch(T data, TrackingQuestSignal obj);
    }
}