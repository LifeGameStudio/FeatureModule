namespace GameModule.Tutorial.Scripts.Services
{
    using System.Collections.Generic;
    using GameModule.Tutorial.Scripts.DataState;
    using GameModule.Tutorial.Scripts.TaskStateFlow;

    public class TaskActionServices
    {
        private Dictionary<string, ITaskStateFlow> cacheTaskActives = new();

        public TaskActionServices(List<ITaskStateFlow> taskFlow)
        {
            foreach (var item in taskFlow)
            {
                this.cacheTaskActives.Add(item.Id, item);
            }
        }

        public void DoTask(TutorialTaskDataState taskDataState, string taskActiveType, string taskActiveData)
        {
            if (this.cacheTaskActives.TryGetValue(taskActiveType, out var task))
            {
                task.Execute(taskDataState, taskActiveData);
            }
        }

        public void AssignTaskGoal(TutorialTaskDataState taskDataState) { this.DoTask(taskDataState, taskDataState.TaskRecord.TaskGoalType, taskDataState.TaskRecord.TaskGoalData); }
    }
}