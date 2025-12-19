namespace GameModule.Tutorial.Scripts.TaskStateFlow.Goal
{
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using FeatureTemplate.Scripts.Handle;
    using GameModule.Tutorial.Scripts.DataState;
    using Zenject;

    public class WaitTimeTaskGoalData : IActionData
    {
        public float WaitDuration;
    }

    public class WaitTime : BaseTaskGoal<WaitTimeTaskGoalData>
    {
        public WaitTime(ISignalBus signalBus) : base(signalBus) { }

        public override string Id => "wait_time";

        protected override UniTask ProcessInternal(TutorialTaskDataState taskDataState, WaitTimeTaskGoalData model)
        {
            DOVirtual.DelayedCall(model.WaitDuration, () => { this.CompleteCurrentTask(taskDataState); });

            return UniTask.CompletedTask;
        }

      
    }
}