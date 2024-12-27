namespace GameModule.TimeMarker.Scripts
{
    using System;
    using System.Collections.Generic;
    using FeatureTemplate.Scripts.InterfacesAndEnumCommon;
    using GameFoundation.Scripts.Interfaces;

    public class TimeMarkData : ILocalData, IFeatureLocalData
    {
        public Dictionary<string, DateTime> TimeMarks = new Dictionary<string, DateTime>();
        public Dictionary<string, DateTime> FutureMarks = new Dictionary<string, DateTime>();
        public void Init()
        {
            this.TimeMarks   = new();
            this.FutureMarks = new();
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
        #region TimeMark
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

        #endregion
        #region Future Mark

        public void CreateFutureMark(string key, DateTime futureTime)
        {
            var data = this.localData;

            if (data.FutureMarks.ContainsKey(key))
            {
                throw new Exception($"TimeMark - Create: Future Mark is already created");
            }
            else
            {
                data.FutureMarks.Add(key, futureTime);
            }
        }

        public DateTime GetFutureMark(string key)
        {
            var data = (TimeMarkData) this.localData;

            if (!data.TimeMarks.TryGetValue(key, out var mark))
            {
                throw new Exception($"TimeMark - Get: Future Mark have not been created");
            }
            return mark;
        }

        public void RemoveFutureMark(string key)
        {
            var data = (TimeMarkData)this.localData;
            if (!data.FutureMarks.ContainsKey(key))
            {
                throw new Exception($"TimeMark - Remove: Future Mark have not been created");
            }
            data.FutureMarks.Remove(key);
        }

        public void UpdateFutureMark(string key, DateTime time)
        {
            var data = (TimeMarkData)this.localData;
            if (data.FutureMarks.ContainsKey(key))
            {
                data.FutureMarks[key] = time;
            }
            else
            {
                throw new Exception($"TimeMark - Update: Future Mark have not been created");
            }
        }

        #endregion
    }
}