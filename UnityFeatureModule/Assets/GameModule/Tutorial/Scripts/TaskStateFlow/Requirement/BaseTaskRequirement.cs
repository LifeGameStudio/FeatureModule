namespace GameModule.Tutorial.Scripts.TaskStateFlow.Requirement
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Handle;
    using FeatureTemplate.Scripts.Services;
    using GameModule.Tutorial.Scripts.DataState;
    using GameModule.Tutorial.Scripts.Services;
    using UnityEngine;

    public interface ITaskRequirement
    {
        string Id { get; }

        UniTask Execute(TutorialTaskDataState taskDataState, string data);
    }

    public abstract class BasTaskRequirement<T> : BaseActionHandle<T>, ITaskRequirement, IDisposable
    {
        private readonly TaskActionServices      TaskActionServices;
        private          CancellationTokenSource cts;

        protected BasTaskRequirement(TaskActionServices taskActionServices) { this.TaskActionServices = taskActionServices; }

        public async UniTask Execute(TutorialTaskDataState taskDataState, string data)
        {
            if (taskDataState.TaskState == TutorialState.Completed)
                return;

            this.cts?.Cancel();
            this.cts?.Dispose();
            this.cts = new CancellationTokenSource();

            var model = this.DeserializeData(taskDataState.TaskRecord.TaskRequirementData);

            await this.ProcessInternal(taskDataState, model, this.cts.Token);

            this.DoActiveAction(taskDataState);
            this.TaskActionServices.AssignTaskGoal(taskDataState);

            try
            {
                await UniTask.WaitUntil(
                    () => taskDataState.TaskState == TutorialState.Completed,
                    cancellationToken: this.cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            this.DoCompletedAction(taskDataState);
        }

        private void DoCompletedAction(TutorialTaskDataState taskDataState)
        {
            this.LogMessage($"Task Completed: {taskDataState.TaskRecord.TaskId}",Color.red);
            foreach (var taskActionRecord in taskDataState.TaskRecord.TaskCompleteRecords)
            {
                this.TaskActionServices.DoTask(taskDataState, taskActionRecord.TaskCompleteType, taskActionRecord.TaskCompleteData);
            }
        }

        private void DoActiveAction(TutorialTaskDataState taskDataState)
        {
            foreach (var taskActionRecord in taskDataState.TaskRecord.TaskActiveRecords)
            {
                this.TaskActionServices.DoTask(taskDataState, taskActionRecord.TaskActiveType, taskActionRecord.TaskActiveData);
            }
        }

        protected abstract UniTask ProcessInternal(TutorialTaskDataState taskDataState, T data, CancellationToken cancellationToken);

        public void Dispose()
        {
            this.cts?.Cancel();
            this.cts?.Dispose();
            this.cts = null;
        }
    }
}