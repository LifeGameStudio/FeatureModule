using System;
using System.Linq;
using Blueprints;
using GameFoundation.Scripts.AssetLibrary;
using GameFoundation.Scripts.UIModule.MVP;
using GameModule.QuestModule.Model;
using GameModule.QuestModule.NotificationBag;
using TMPro;
using UnityEngine.UI;
using Zenject;

public class QuestItemModel
{
    public bool                   IsNoQuest    { get; set; }
    public QuestLog               QuestLog     { get; set; }
    public Action<QuestItemModel> ShowAllQuest { get; set; }
    public Action<QuestItemModel> OnSearch     { get; set; }
}

public class QuestItemView : TViewMono
{
    public Button                  btnSearch;
    public Button                  btnShowAllQuest;
    public Image                   imgTarget;
    public TextMeshProUGUI         txtDescription;
    public TextMeshProUGUI         txtProgress;
    public Image                   imgProgress;
    public NotificationBagItemView notificationBagItemView;
}

public class QuestItemPresenter : BaseUIItemPresenter<QuestItemView, QuestItemModel>
{
    private readonly DiContainer diContainer;

    private NotificationBagItemPresenter notificationBagItemPresenter;
    public QuestItemPresenter(IGameAssets gameAssets, DiContainer diContainer) : base(gameAssets) { this.diContainer = diContainer; }

    public override void BindData(QuestItemModel param)
    {
        this.CheckToShowNotificationBag(param);
        this.View.btnShowAllQuest.onClick.RemoveAllListeners();
        this.View.btnShowAllQuest.onClick.AddListener(() => { param.ShowAllQuest?.Invoke(param); });

        this.View.btnSearch.onClick.RemoveAllListeners();
        this.View.btnSearch.onClick.AddListener(() => { param.OnSearch?.Invoke(param); });

        if (param.IsNoQuest)
        {
            this.View.txtDescription.text = "No Quest";
            this.View.txtProgress.text    = "";
            this.View.imgTarget.gameObject.SetActive(false);

            return;
        }

        var taskLog = param.QuestLog.TaskProgress.FirstOrDefault(x => x.TaskStatus == QuestStatus.InProgress) ?? param.QuestLog.TaskProgress.LastOrDefault(x => x.TaskStatus == QuestStatus.Completed);
        this.View.imgTarget.gameObject.SetActive(true);

        if (taskLog == null)
        {
            return;
        }

        this.View.txtDescription.text = taskLog.TaskRecord.Description;

        var progressFinished = taskLog.Progress.Count(x => x.CurrentValue >= x.RequiredValue);

        this.View.txtProgress.text       = $"{Math.Min(progressFinished, taskLog.Progress.Count)}/{taskLog.Progress.Count}";
        this.View.imgProgress.fillAmount = (float)progressFinished / taskLog.Progress.Count;
    }

    private void CheckToShowNotificationBag(QuestItemModel param)
    {
        var notificationBagItemModel = new NotificationBagItemModel()
        {
            HasNotice = !param.IsNoQuest
        };

        this.notificationBagItemPresenter ??= this.diContainer.Instantiate<NotificationBagItemPresenter>();
        this.notificationBagItemPresenter.SetView(this.View.notificationBagItemView);

        if (!param.IsNoQuest)
        {
            notificationBagItemModel.HasNotice = param.QuestLog.QuestStatus == QuestStatus.Completed;
        }

        this.notificationBagItemPresenter.BindData(notificationBagItemModel);
    }
}