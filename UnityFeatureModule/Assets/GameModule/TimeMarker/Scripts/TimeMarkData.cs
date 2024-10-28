namespace GameModule.TimeMarker.Scripts
{
    using System;
    using System.Collections.Generic;
    using FeatureTemplate.Scripts.InterfacesAndEnumCommon;
    using GameFoundation.Scripts.Interfaces;

    public class TimeMarkData : ILocalData, IFeatureLocalData
    {
        public Dictionary<string, DateTime> TimeMarks = new Dictionary<string, DateTime>();
        public void Init()
        {
            this.TimeMarks = new();
        }

        public Type ControllerType => typeof(TimeMarkDataController);
    }
    
    public class TimeMarkDataController : IFeatureControllerData
    {
        private readonly TimeMarkData localData;
        public TimeMarkDataController(TimeMarkData localData)
        {
            this.localData = localData;
        }
        
        public void AddTimeMark(string key, DateTime time)
        {
            var data = (TimeMarkData) this.localData;
            data.TimeMarks[key] = time;
        }
        
        public bool GetTimeMark(string key, out DateTime time)
        {
            var data = (TimeMarkData) this.localData;
            if(data.TimeMarks.ContainsKey(key))
            {
                time = data.TimeMarks[key];
                return true;
            }
            else
            {
                time = DateTime.Now;
                return false;
            }
        }
        
        public void RemoveTimeMark(string key)
        {
            var data = (TimeMarkData) this.localData;
            data.TimeMarks.Remove(key);
        }
        
        public void UpdateTimeMark(string key, DateTime time)
        {
            var data = (TimeMarkData) this.localData;
            if(data.TimeMarks.ContainsKey(key))
            {
                data.TimeMarks[key] = time;
            }
        }
    }
}