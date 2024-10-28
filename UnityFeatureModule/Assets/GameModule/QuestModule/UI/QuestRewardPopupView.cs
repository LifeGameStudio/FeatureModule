namespace GameModule.QuestModule.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameModule.QuestModule.Model;
    using global::Blueprints;
    using global::QuestModule.UI;
    using TMPro;
    using UnityEngine.UI;
    using Zenject;

    public class QuestRewardPopupView : BaseView
    {
        public QuestListRewardAdapter questListRewardAdapter;
        public Image                  imgCurrentMainQuest;
        public TextMeshProUGUI        txtCurrentMainQuest;
        public TextMeshProUGUI        txtCurrenProgressMainQuest;
        public Button                 btnClose;
    }

    [PopupInfo(nameof(QuestRewardPopupView), false)]
    public class QuestRewardPopupPresenter : BasePopupPresenter<QuestRewardPopupView>
    {
        private readonly QuestManager               questManager;
        private readonly DiContainer                diContainer;
        private          List<QuestRewardItemModel> questRewardItemModels = new();

        public QuestRewardPopupPresenter(SignalBus signalBus, QuestManager questManager, DiContainer diContainer) : base(signalBus)
        {
            this.questManager = questManager;
            this.diContainer  = diContainer;
        }

        protected override void OnViewReady()
        {
            base.OnViewReady();
            this.View.btnClose.onClick.AddListener(this.CloseView);
        }

        public override UniTask BindData()
        {
            this.PrepareModel();

            this.FillDataCurrentMainQuest();

            return UniTask.CompletedTask;
        }

        private void FillDataCurrentMainQuest()
        {
            var currentMainQuest= this.questManager.GetCurrentMainQuest();
            if (currentMainQuest != null)
            {
                var taskLog = this.questManager.GetCurrentTaskProgress(currentMainQuest.QuestId, currentMainQuest.ProviderId) ?? currentMainQuest.TaskProgress.LastOrDefault(x => x.TaskStatus == QuestStatus.Completed);

                if(taskLog==null)return;

                this.View.txtCurrentMainQuest.text = taskLog.TaskRecord.Description;
               
                var totalTaskComplete = currentMainQuest.TaskProgress.Count(x => x.TaskStatus == QuestStatus.Completed);
                this.View.txtCurrenProgressMainQuest.text = $"{totalTaskComplete} / {currentMainQuest.TaskProgress.Count}";
            }
            else
            {
                this.View.txtCurrentMainQuest.text = "No Quest";
            }
        }

        private void PrepareModel()
        {
            this.questRewardItemModels.Clear();
            var listQuest         = this.questManager.GetAllQuestsType(QuestProviderType.Main);
            var allCompletedQuest = listQuest.FindAll(x => x.QuestStatus is QuestStatus.Completed or QuestStatus.Rewarded);

            foreach (var q in allCompletedQuest)
            {
                var model = new QuestRewardItemModel()
                {
                    QuestLog  = q,
                    OnRefresh = this.RefreshView
                };

                this.questRewardItemModels.Add(model);
            }

            this.View.questListRewardAdapter.InitItemAdapter(this.questRewardItemModels, this.diContainer).Forget();
        }

        private void RefreshView(QuestRewardItemModel obj) { this.View.questListRewardAdapter.Refresh(); }
    }
}