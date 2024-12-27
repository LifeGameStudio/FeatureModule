namespace GameModule.TimeMarker.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using R3;
    using Sirenix.Utilities;
    using UnityEngine;
    using Zenject;

    public class FutureMarkService : ITickable
    {
        private readonly TimeMarkDataController timeMarkDataController;
        public Dictionary<string, ReactiveProperty<float>> FutureMarkTimers = new Dictionary<string, ReactiveProperty<float>>();

        public FutureMarkService(TimeMarkDataController timeMarkDataController)
        {
            this.timeMarkDataController = timeMarkDataController;
        }

        public void CreateFutureMark(string key, DateTime futureDate)
        {
            this.timeMarkDataController.CreateFutureMark(key, futureDate);
        }

        public DateTime GetFutureDate(string key)
        {
            return this.timeMarkDataController.GetFutureMark(key);
        }

        public void RemoveFutureMark(string key)
        {
            this.timeMarkDataController.RemoveFutureMark(key);
        }

        public void UpdateFutureMark(string key, DateTime futureDate)
        {
            this.timeMarkDataController.UpdateFutureMark(key, futureDate);
        }

        public ReactiveProperty<float> GetCountDownToFutureMark(string key)
        {
            if (this.FutureMarkTimers.ContainsKey(key))
            {
                return this.FutureMarkTimers[key];
            }
            else
            {
                var futureDate = this.GetFutureDate(key);
                var distance = futureDate.Subtract(DateTime.UtcNow).TotalSeconds;
                var timer = new ReactiveProperty<float>((float)distance);
                this.FutureMarkTimers.Add(key, timer);
                return timer;
            }
        }
        
        private Dictionary<string, ReactiveProperty<float>> snapShot = new();
        
        public void Tick()
        {
            // Increment each ReactiveProperty<float> in timeSpanDictionary by delta time
            var deltaSeconds = Time.deltaTime;

            // create a snapshot of the dictionary to avoid modify
            this.snapShot.Clear();

            lock (this.FutureMarkTimers)
            {
                this.FutureMarkTimers.ForEach(x => this.snapShot.Add(x.Key, x.Value));
            }

            for (int i = 0; i < this.snapShot.Count; i++)
            {
                var timeSpan = this.snapShot.Values.ToList()[i];
                timeSpan.Value -= deltaSeconds;
                timeSpan.ForceNotify();

                if (timeSpan.Value <= 0)
                {
                    this.FutureMarkTimers.Remove(this.snapShot.Keys.ToList()[i]);
                    i--;
                }
            }
        }
    }
}