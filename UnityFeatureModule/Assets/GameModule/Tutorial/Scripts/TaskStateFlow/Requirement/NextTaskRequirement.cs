namespace GameModule.Tutorial.Scripts.TaskStateFlow.Requirement
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using GameModule.Tutorial.Scripts.DataState;
    using GameModule.Tutorial.Scripts.Services;

    public class NextTaskRequirement : BasTaskRequirement<string>
    {
        public NextTaskRequirement(TaskActionServices taskActionServices) : base(taskActionServices) { }

        public override string Id => TutorialStaticValue.NextTask;

        protected override UniTask ProcessInternal(TutorialTaskDataState taskDataState, string model, CancellationToken token)
        {
            taskDataState.TaskState = TutorialState.InProgress;

            return UniTask.CompletedTask;
        }
    }
}