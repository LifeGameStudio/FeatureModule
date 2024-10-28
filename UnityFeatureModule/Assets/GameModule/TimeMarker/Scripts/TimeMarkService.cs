namespace GameModule.TimeMarker.Scripts
{
    using System;

    public class TimeMarkService
    {
        private readonly TimeMarkDataController timeMarkDataController;

        public TimeMarkService(TimeMarkDataController timeMarkDataController)
        {
            this.timeMarkDataController = timeMarkDataController;
        }
        
        public void AddTimeMark(string key, DateTime time)
        {
            this.timeMarkDataController.AddTimeMark(key, time);
        }
        
        public bool GetTimeMark(string key, out DateTime time)
        {
            return this.timeMarkDataController.GetTimeMark(key, out time);
        }
        
        public void RemoveTimeMark(string key)
        {
            this.timeMarkDataController.RemoveTimeMark(key);
        }
        
        public void UpdateTimeMark(string key, DateTime time)
        {
            this.timeMarkDataController.UpdateTimeMark(key, time);
        }

        public bool IsNewDay(string timeMarkKey)
        {
             this.timeMarkDataController.GetTimeMark(timeMarkKey, out var dateTime);
             return dateTime.Date < DateTime.Now.Date;
        }
        
        public int GetDayDifference(string timeMarkKey)
        {
            this.timeMarkDataController.GetTimeMark(timeMarkKey, out var dateTime);
            return (DateTime.Now.Date - dateTime.Date).Days;
        }
        
        public void ResetTimeMark(string timeMarkKey)
        {
            this.timeMarkDataController.RemoveTimeMark(timeMarkKey);
        }
    }
}