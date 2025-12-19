namespace GameModule.Tutorial.Scripts.TaskStateFlow.Goal
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameModule.Tutorial.Scripts.DataState;
    using GameModule.Tutorial.Scripts.Services;
    using R3;
    using UnityEngine;
    using Zenject;

    public class TapAnyWhereToComplete : BaseTaskGoal<string>
    {
        private IDisposable dis;
        public TapAnyWhereToComplete(ISignalBus signalBus) : base(signalBus) { }

        public override string Id => TutorialStaticValue.TaskFlow.TapAnywhere;

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