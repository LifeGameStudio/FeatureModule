namespace GameModule.Tutorial.Scripts.TaskStateFlow.CommonFlow
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Handle;
    using FeatureTemplate.Scripts.Services;
    using GameModule.Tutorial.Scripts.DataState;
    using Zenject;

    public class SetObjectStatusTutorialTaskData : IActionData
    {
        public string ObjectPath;
        public bool   Status;
    }

    public class SetObjectStatusTutorialTask : BaseTaskFlow<SetObjectStatusTutorialTaskData>
    {
        public SetObjectStatusTutorialTask(ISignalBus signalBus) : base(signalBus) { }

        public override string Id => "set_object_status";

        protected override UniTask ProcessInternal(TutorialTaskDataState taskDataState, SetObjectStatusTutorialTaskData tutorialTaskData)
        {
            var objectList = tutorialTaskData.ObjectPath.Split(",");

            foreach (var s in objectList)
            {
                var obj = FeatureObjectCollectionServices.Instance.GetObjectInstanceByPath(s);

                if (obj != null)
                {
                    obj.SetActive(tutorialTaskData.Status);
                }
            }

            return UniTask.CompletedTask;
        }
    }
}