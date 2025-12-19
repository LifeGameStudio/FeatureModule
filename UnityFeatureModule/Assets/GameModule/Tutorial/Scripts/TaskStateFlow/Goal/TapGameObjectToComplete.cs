namespace GameModule.Tutorial.Scripts.TaskStateFlow.Goal
{
    using System;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Handle;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameModule.Tutorial.Scripts.DataState;
    using GameModule.Tutorial.Scripts.Services;
    using GameModule.Tutorial.Scripts.TaskStateFlow.MonoUltilities;
    using R3;
    using UnityEngine;
    using Zenject;
    using Object = UnityEngine.Object;

    public enum ObjectType
    {
        UI,
        World2d,
        World3d,
    }

    public class TapGameObjectToCompleteData : IActionData
    {
        public string     GameObjectPath;
        public ObjectType Type;
    }

    public class TapGameObjectToComplete : BaseTaskGoal<TapGameObjectToCompleteData>
    {
        private IDisposable dis;
        public TapGameObjectToComplete(ISignalBus signalBus) : base(signalBus) { }

        public override string Id => TutorialStaticValue.TaskFlow.TapToGameObject;

        protected override UniTask ProcessInternal(TutorialTaskDataState taskDataState, TapGameObjectToCompleteData model)
        {
            this.dis = Observable.EveryUpdate().Subscribe(_ =>
            {
                if (!FeatureObjectCollectionServices.Instance.GetObjectInstanceByPath(model.GameObjectPath, out var targetObject)) return;

                var iClickable = targetObject.GetComponent<IClickable>();

                if (iClickable == null)
                {
                    switch (model.Type)
                    {
                        case ObjectType.UI:
                            iClickable = targetObject.GetOrAddComponent<UIClickable>();

                            break;
                        case ObjectType.World2d:
                            iClickable = targetObject.GetOrAddComponent<WorldClickable2D>();

                            break;
                        case ObjectType.World3d:
                            iClickable = targetObject.GetOrAddComponent<WorldClickable3D>();

                            break;
                    }
                }

                if (iClickable != null)
                {
                    Action onClicked = null;

                    onClicked = () =>
                    {
                        iClickable.Clicked -= onClicked;

                        if (taskDataState.TaskState == TutorialState.Completed)
                        {
                            return;
                        }

                        this.CompleteCurrentTask(taskDataState);

                        if (iClickable is Component component)
                        {
                            Object.Destroy(component, 0.1f);
                        }
                    };

                    iClickable.Clicked += onClicked;
                }

                this.dis.Dispose();
            });

            return UniTask.CompletedTask;
        }
    }
}