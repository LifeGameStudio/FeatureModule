namespace GameModule.Tutorial.Scripts.TaskStateFlow
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Handle;
    using GameModule.Tutorial.Scripts.DataState;
    using Zenject;

    public interface ITaskStateFlow
    {
        string  Id { get; }
        UniTask Execute(TutorialTaskDataState taskDataState, string data);
    }

    public abstract class BaseTaskFlow<T> : BaseActionHandle<T>, IInitializable, ITaskStateFlow
    {
        protected readonly ISignalBus SignalBus;

        protected BaseTaskFlow(ISignalBus signalBus) { this.SignalBus = signalBus; }

        public virtual UniTask Execute(TutorialTaskDataState taskDataState, string data)
        {
            var model = this.DeserializeData(data);
            this.ProcessInternal(taskDataState, model);

            return UniTask.CompletedTask;
        }

        protected abstract UniTask ProcessInternal(TutorialTaskDataState taskDataState, T model);
        public virtual     void    Initialize() { }
    }
}