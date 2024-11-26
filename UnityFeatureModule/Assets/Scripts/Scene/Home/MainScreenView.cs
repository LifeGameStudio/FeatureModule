using Cysharp.Threading.Tasks;
using Game.Scripts.MVP;
using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
using GameFoundation.Scripts.Utilities.LogService;
using GameModule.QuestModule;
using GameModule.QuestModule.Model;
using GameModule.QuestModule.Signals;
using QuestModule.Provider;
using Zenject;

namespace Game.Scripts.Scene.Home
{
    public class MainScreenView : BaseScreenViewTemplate
    {
    }

    [ScreenInfo(nameof(MainScreenView))]
    public class MainScreenPresenter : BaseScreenPresenterTemplate<MainScreenView>
    {
        [Inject] private QuestManager _questManager;
        [Inject] private TrackingQuestServices _trackingQuestServices;
        [Inject] private QuestProviderServices questProviderServices;

        public MainScreenPresenter(ISignalBus signalBus, GameQueueActionContext gameQueueActionContext,
            ILogService logger, ScreenManager screenManager, SceneDirector sceneDirector) : base(signalBus,
            gameQueueActionContext, logger, screenManager, sceneDirector)
        {
        }

        public override UniTask BindData()
        {
            this.SignalBus.Subscribe<QuestDoneSignal>(OnQuestDone);
            
            StartQuest("1");
            
            this.SignalBus.Fire<TrackingQuestSignal>(new TrackingQuestSignal("play_level", "", 1));

            // this._questManager.GetAllQuestRewardAndSetStatus("", "1");
            
            return UniTask.CompletedTask;
        }

        private void OnQuestDone(QuestDoneSignal signal)
        {
            StartQuest((int.Parse(signal.QuestId)+1).ToString());
        }

        private void StartQuest(string id)
        {
            questProviderServices.GiveQuestToUser(id, "", QuestProviderType.Main);
            questProviderServices.StartQuest(QuestProviderType.Main, id, "");
        }
    }
}