namespace GameModule.Tutorial.Scripts.TaskStateFlow.CommonFlow
{
    using Cysharp.Threading.Tasks;
    using GameModule.Tutorial.Scripts.DataState;
    using Zenject;

    public class HighLightUITaskData
    {
        public string GameObjectPath;
    }

    public class HighLightUI : BaseTaskFlow<HighLightUITaskData>
    {
        public HighLightUI(ISignalBus signalBus) : base(signalBus) { }

        public override    string  Id                                                                              => "highlight_ui";
        protected override UniTask ProcessInternal(TutorialTaskDataState taskDataState, HighLightUITaskData model) { return UniTask.CompletedTask; }
    }
}