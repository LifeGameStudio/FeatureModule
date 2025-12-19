namespace GameModule.Tutorial.Scripts.TaskStateFlow.Goal
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameModule.Tutorial.Scripts.DataState;
    using R3;
    using UnityEngine;
    using Zenject;

    public class TapAnyWhereToComplete : BaseTaskGoal<string>
    {
        private IDisposable dis;
        public TapAnyWhereToComplete(ISignalBus signalBus) : base(signalBus) { }

        public override string Id => "tap_any_where_to_complete";

        protected override UniTask ProcessInternal(TutorialTaskDataState taskDataState, string model)
        {
            this.dis = Observable.EveryUpdate().Subscribe(_ =>
            {
                if (Input.GetMouseButtonDown(0))
                {
                    this.dis.Dispose();
                    this.CompleteCurrentTask(taskDataState);
                }
            });

            return UniTask.CompletedTask;
        }
    }
}