namespace GameModule.TimeMarker.Scripts
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using R3;
    using Sirenix.Utilities;
    using UnityEngine;
    using Zenject;

    public class TimeMarkService : ITickable
    {
        private readonly TimeMarkDataController timeMarkDataController;

        // Dictionary to cache the ReactiveProperty<float> for each key
        private readonly Dictionary<string, ReactiveProperty<float>> timeSpanDictionary = new();
        private readonly Dictionary<string, ReactiveProperty<float>> timeSpanOffDictionary = new();
        private readonly Dictionary<string, bool> isPaused = new();

        public TimeMarkService(TimeMarkDataController timeMarkDataController)
        {
            this.timeMarkDataController = timeMarkDataController;
        }

        public void AddTimeMark(string key, DateTime time)
        {
            this.timeMarkDataController.AddTimeMark(key, time);
        }

        public bool GetOrCreateTimeMark(string key, out DateTime createTime)
        {
            return this.timeMarkDataController.GetOrCreateTimeMark(key, out createTime);
        }

        public void RemoveTimeMark(string key)
        {
            this.timeSpanOffDictionary.Remove(key, out var time);
            this.timeMarkDataController.RemoveTimeMark(key);
            this.timeSpanDictionary.Remove(key); // Remove from dictionary if the key is deleted
            this.isPaused.Remove(key); // Also remove pause state
        }

        private async void AddToTimeSpanOff(string key)
        {
            timeSpanOffDictionary.TryAdd(key, await this.GetOrCreateTimer(key));
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

        public void PauseTimeMark(string timeMarkKey, bool pause)
        {
            if (this.isPaused.ContainsKey(timeMarkKey))
            {
                this.isPaused[timeMarkKey] = pause; // Update existing key
            }
            else
            {
                this.isPaused.Add(timeMarkKey, pause); // Add if not exists
            }
        }

        public void ResetTimeMark(string timeMarkKey, bool turnOnTimerAfterReset = false)
        {
            this.AddToTimeSpanOff(timeMarkKey);
            this.timeMarkDataController.RemoveTimeMark(timeMarkKey);
            this.timeMarkDataController.AddTimeMark(timeMarkKey, DateTime.Now);
            this.timeSpanDictionary.Remove(timeMarkKey); // Remove from dictionary if the key is deleted
            this.isPaused.Remove(timeMarkKey); // Also remove pause state

            if (turnOnTimerAfterReset)
            {
                this.GetOrCreateTimer(timeMarkKey).Forget();
            }
        }

        public async UniTask<ReactiveProperty<float>> GetOrCreateTimer(string key)
        {
            return await GetOrCreateTimeSpan(key);
        }

        [Obsolete("Use the new GetOrCreateTimer method instead.", false)]
        public async UniTask<ReactiveProperty<float>> GetOrCreateTimeSpan(string key)
        {
            if (!timeSpanDictionary.TryGetValue(key, out var timeSpan))
            {
                // Create a new ReactiveProperty if it doesn't exist in the dictionary
                if (this.timeSpanOffDictionary.TryGetValue(key, out var value))
                {
                    timeSpan = value;
                    this.timeSpanOffDictionary.Remove(key);
                }
                else
                {
                    timeSpan = new ReactiveProperty<float>();
                }

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

        private Dictionary<string, ReactiveProperty<float>> snapShot = new();

        public void Tick() // TODO: implement real-time marker, ignore time scale
        {
            var deltaSeconds = Time.deltaTime;

            // Create a snapshot of the dictionary to avoid modification issues
            this.snapShot.Clear();

            lock (this.timeSpanDictionary)
            {
                this.timeSpanDictionary.ForEach(x => this.snapShot.Add(x.Key, x.Value));
            }

            foreach (var kvp in this.snapShot)
            {
                var key = kvp.Key;
                var timeSpan = kvp.Value;

                if (!this.isPaused.TryGetValue(key, out var paused) || !paused) // Only update if not paused
                {
                    timeSpan.Value += deltaSeconds;
                    timeSpan.ForceNotify();
                }
            }
        }
    }
}
