namespace Game.Scripts.Scene.Home
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Game.Scripts.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameModule.QuestModule.Model;
    using GameModule.QuestModule.Signals;
    using Zenject;

    public class MainScreenView : BaseScreenViewTemplate
    {
    }

    [ScreenInfo(nameof(MainScreenView))]
    public class MainScreenPresenter : BaseScreenPresenterTemplate<MainScreenView>
    {
        [Inject] private QuestManager _questManager;
        // [Inject] private TrackingQuestServices _trackingQuestServices;
        // [Inject] private QuestProviderServices questProviderServices;

        public MainScreenPresenter(ISignalBus signalBus, GameQueueActionContext gameQueueActionContext,
            ILogService logger, ScreenManager screenManager, SceneDirector sceneDirector) : base(signalBus,
            gameQueueActionContext, logger, screenManager, sceneDirector)
        {
        }

        public override UniTask BindData()
        {
            // this.SignalBus.Subscribe<QuestDoneSignal>(OnQuestDone);

            this.SignalBus.Fire<TrackingQuestSignal>(new TrackingQuestSignal("daily_login", new List<string>()
            {
                ""
            }, 1));

            // this._questManager.GetAllQuestRewardAndSetStatus("", "1");

            return UniTask.CompletedTask;
        }

        private void OnQuestDone(QuestChangeStatusSignal signal) { }

        private void StartQuest(string id)
        {
            // questProviderServices.GiveQuestToUser(id, "", QuestProviderType.Main);
            // questProviderServices.StartQuest(QuestProviderType.Main, id, "");
        }
    }
}