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
        
        /// <summary>
        /// Retrieves the time mark associated with the specified key. 
        /// If the key does not exist, creates a new time mark with the current time, 
        /// stores it in the TimeMarks dictionary, and returns false.
        /// </summary>
        /// <param name="key">The unique key identifying the time mark.</param>
        /// <param name="time">The output parameter that will hold the retrieved or newly created time mark.</param>
        /// <returns>
        /// True if the time mark was found and retrieved; false if a new time mark was created and stored.
        /// </returns>
        public bool GetOrCreateTimeMark(string key, out DateTime time)
        {
            var data = (TimeMarkData)this.localData;

            if (data.TimeMarks.ContainsKey(key))
            {
                time = data.TimeMarks[key];
                return true;
            }
            else
            {
                // Create a new time mark, store it, and return false
                time                = DateTime.Now;
                data.TimeMarks[key] = time; // Store the new time mark for future use
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