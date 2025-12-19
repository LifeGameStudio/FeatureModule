namespace GameModule.Tutorial.Scripts.TaskStateFlow.Goal
{
    using DG.Tweening;
    using GameModule.Tutorial.Scripts.DataState;
    using GameModule.Tutorial.Scripts.Signals;
    using Zenject;

    public interface ITaskGoal : ITaskStateFlow
    {
    }

    public abstract class BaseTaskGoal<T> : BaseTaskFlow<T>, ITaskGoal
    {
        protected BaseTaskGoal(ISignalBus signalBus) : base(signalBus) { }

        protected virtual void CompleteCurrentTask(TutorialTaskDataState taskDataState)
        {
            taskDataState.TaskState = TutorialState.Completed;

            //delay need to do complete action first
            DOVirtual.DelayedCall(0.1f, () => { this.SignalBus.Fire(new TaskCompleteSignal()); });
        }

        protected virtual bool IsMet() { return true; }
    }
}