namespace GameModule.TimeMarker.Scripts
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using R3;
    using UnityEngine;
    using Zenject;

    public class TimeMarkService : ITickable
    {
        private readonly TimeMarkDataController timeMarkDataController;

        // Dictionary to cache the ReactiveProperty<float> for each key
        private readonly Dictionary<string, ReactiveProperty<float>> timeSpanDictionary = new Dictionary<string, ReactiveProperty<float>>();

        public TimeMarkService(TimeMarkDataController timeMarkDataController)
        {
            this.timeMarkDataController = timeMarkDataController;
        }
        
        public void AddTimeMark(string key, DateTime time)
        {
            this.timeMarkDataController.AddTimeMark(key, time);
        }
        
        public bool GetOrCreateTimeMark(string key, out DateTime time)
        {
            return this.timeMarkDataController.GetOrCreateTimeMark(key, out time);
        }
        
        public void RemoveTimeMark(string key)
        {
            this.timeMarkDataController.RemoveTimeMark(key);
            this.timeSpanDictionary.Remove(key); // Remove from dictionary if the key is deleted
        }
        
        public void UpdateTimeMark(string key, DateTime time)
        {
            this.timeMarkDataController.UpdateTimeMark(key, time);
            if (this.timeSpanDictionary.TryGetValue(key, out var timeSpan))
            {
                // Recalculate the initial time span if it exists in the dictionary
                timeSpan.Value = (float)(DateTime.Now - time).TotalSeconds;
            }
        }

        public bool IsNewDay(string timeMarkKey)
        {
            if (this.timeMarkDataController.GetOrCreateTimeMark(timeMarkKey, out var dateTime))
            {
                return dateTime.Date < DateTime.Now.Date;
            }
            return true;
        }
        
        public int GetDayDifference(string timeMarkKey)
        {
            if (this.timeMarkDataController.GetOrCreateTimeMark(timeMarkKey, out var dateTime))
            {
                return (DateTime.Now.Date - dateTime.Date).Days;
            }
            return 0;
        }
        
        public void ResetTimeMark(string timeMarkKey)
        {
            this.timeMarkDataController.RemoveTimeMark(timeMarkKey);
            timeSpanDictionary.Remove(timeMarkKey); // Also remove from dictionary if the key is reset
        }

        // New function to get or create a ReactiveProperty<float> for the timespan in seconds
        public async UniTask<ReactiveProperty<float>> GetOrCreateTimeSpan(string key)
        {
            if (!timeSpanDictionary.TryGetValue(key, out var timeSpan))
            {
                // Create a new ReactiveProperty if it doesn't exist in the dictionary
                timeSpan = new ReactiveProperty<float>();
                timeSpanDictionary[key] = timeSpan;

                // Check if the time mark exists in the data controller
                if (this.timeMarkDataController.GetOrCreateTimeMark(key, out var savedTime))
                {
                    // Initialize the timeSpan value based on the saved time
                    timeSpan.Value = (float)(DateTime.Now - savedTime).TotalSeconds;
                }
                else
                {
                    // If there is no saved time, set the timespan value to 0
                    timeSpan.Value = 0f;
                }
            }

            await UniTask.DelayFrame(1);
            return timeSpan;
        }

        public void Tick() // TODO: implement realtime marker, ignore time scale
        {
            // Increment each ReactiveProperty<float> in timeSpanDictionary by delta time
            float deltaSeconds = Time.deltaTime;
            foreach (var timeSpan in timeSpanDictionary.Values)
            {
                timeSpan.Value += deltaSeconds;
                timeSpan.ForceNotify();
            }
        }
    }
}
