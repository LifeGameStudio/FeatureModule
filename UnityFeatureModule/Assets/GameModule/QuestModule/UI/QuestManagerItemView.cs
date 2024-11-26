using System;
using System.Collections.Generic;
using System.Linq;
using Blueprints;
using Cysharp.Threading.Tasks;
using GameFoundation.Scripts.AssetLibrary;
using GameFoundation.Scripts.UIModule.MVP;
using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
using GameModule.QuestModule.Model;
using GameModule.QuestModule.Signals;
using GameModule.QuestModule.UI;
using QuestModule.UI;
using TMPro;
using Zenject;

public class QuestManagerItemView : TViewMono
{
    public QuestManagerItemAdapter questManagerItemAdapter;
    public TextMeshProUGUI         txtSideQuestDes;
}

public class QuestManagerItemPresenter : BaseUIItemPresenter<QuestManagerItemView>, IDisposable
{
    private List<QuestItemModel> questItemModels = new();

    private readonly ScreenManager screenManager;
    private readonly QuestManager  questManager;
    private readonly DiContainer   diContainer;
    private readonly ISignalBus     signalBus;

    public QuestManagerItemPresenter(IGameAssets gameAssets, ScreenManager screenManager, QuestManager questManager, DiContainer diContainer, ISignalBus signalBus) : base(gameAssets)
    {
        this.screenManager = screenManager;
        this.questManager  = questManager;
        this.diContainer   = diContainer;
        this.signalBus     = signalBus;
    }

    public void BindData()
    {
        this.Dispose();
        this.signalBus.Subscribe<RefreshQuestViewSignal>(this.OnPrepareQuestData);
        this.OnPrepareQuestData();
    }

    private void OnPrepareQuestData()
    {
        this.PrepareModel();
        this.UpdateStatusSideQuest();
    }

    private void PrepareModel()
    {
        this.questItemModels.Clear();

        var mainQuest = this.questManager.GetCurrentMainQuest();

        if (mainQuest != null)
        {
            var questItemModel = new QuestItemModel()
            {
                QuestLog     = mainQuest,
                ShowAllQuest = this.ShowAllReward,
                OnSearch     = this.OnSearch,
            };

            this.questItemModels.Add(questItemModel);
        }
        else
        {
            var questItemModel = new QuestItemModel()
            {
                IsNoQuest    = true,
                ShowAllQuest = this.ShowAllReward,
                OnSearch     = this.OnSearch,
            };

            this.questItemModels.Add(questItemModel);
        }

        this.View.questManagerItemAdapter.InitItemAdapter(this.questItemModels, this.diContainer).Forget();
    }

    private void OnSearch(QuestItemModel obj) { }

    private void ShowAllReward(QuestItemModel obj) { this.screenManager.OpenScreen<QuestRewardPopupPresenter>().Forget(); }

    private void UpdateStatusSideQuest()
    {
        this.View.txtSideQuestDes.text = "";
        var sideQuest = this.questManager.QuestJournal.Quests.FirstOrDefault(x => x.Value.QuestProviderType == QuestProviderType.Side && x.Value.QuestStatus == QuestStatus.InProgress).Value;

        if (sideQuest == null) return;
        var task = sideQuest.TaskProgress.FirstOrDefault(x => x.TaskStatus != QuestStatus.Rewarded);

        if (task == null)
        {
            this.View.txtSideQuestDes.text = "";

            return;
        }

        this.View.txtSideQuestDes.text = task.TaskRecord.Description;
    }

    public void Dispose() { this.signalBus.TryUnsubscribe<RefreshQuestViewSignal>(this.OnPrepareQuestData); }
}