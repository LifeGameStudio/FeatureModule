namespace GameModule.Tutorial.Scripts.DataState
{
    using System.Collections.Generic;
    using GameModule.Tutorial.Scripts.Blueprints;

    public class TutorialElementDataState
    {
        public TutorialRecord TutorialRecord { get; set; }

        public int                         TaskIndex            { get; set; }
        public int                         CurrentCompleteTask  { get; set; }
        public int                         TargetTaskToComplete { get; set; }
        public List<TutorialTaskDataState> TaskDataStates       { get; set; } = new();
        public bool                        IsCompleted          => this.CurrentCompleteTask >= this.TargetTaskToComplete;
        public TutorialState               TutorialState        { get; set; }
    }

    public class TutorialTaskDataState
    {
        public TutorialTaskRecord TaskRecord { get; set; }
        public TutorialState      TaskState  { get; set; }
    }
}