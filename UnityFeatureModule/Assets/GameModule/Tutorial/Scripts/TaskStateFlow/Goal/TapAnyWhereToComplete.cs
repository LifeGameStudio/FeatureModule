namespace GameModule.Tutorial.Scripts.TaskStateFlow.Goal
{
    using Cysharp.Threading.Tasks;
    using GameModule.Tutorial.Scripts.DataState;
    using UnityEngine;
    using Zenject;

    public class TapAnyWhereToComplete : BaseTaskGoal<string>, ITickable
    {
        private TutorialTaskDataState currentTaskDataState;
        public TapAnyWhereToComplete(ISignalBus signalBus) : base(signalBus) { }

        public override string Id => "tap_any_where_to_complete";

        protected override UniTask ProcessInternal(TutorialTaskDataState taskDataState, string model)
        {
            this.currentTaskDataState = taskDataState;

            return UniTask.CompletedTask;
        }

        public void Tick()
        {
            if (!Input.GetMouseButtonDown(0) || this.currentTaskDataState == null) return;
            this.CompleteCurrentTask(this.currentTaskDataState);
            this.currentTaskDataState = null;
        }
    }
}