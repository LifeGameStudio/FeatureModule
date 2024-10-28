namespace GameModule.QuestModule.UI
{
    using System;
    using System.Linq;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.QuestModule.Model;
    using global::Blueprints;
    using TMPro;
    using UnityEngine.UI;

    public class QuestRewardItemModel
    {
        public QuestLog                     QuestLog  { get; set; }
        public Action<QuestRewardItemModel> OnRefresh { get; set; }
    }

    public class QuestRewardItemView : TViewMono
    {
        public Image           imgIcon;
        public TextMeshProUGUI txtDes;
        public Button          btnClaim;
    }

    public class QuestRewardItemPresenter : BaseUIItemPresenter<QuestRewardItemView, QuestRewardItemModel>
    {
        private readonly ScreenManager screenManager;
        private readonly QuestManager  questManager;

        public QuestRewardItemPresenter(IGameAssets gameAssets, ScreenManager screenManager, QuestManager questManager) : base(gameAssets)
        {
            this.screenManager = screenManager;
            this.questManager  = questManager;
        }

        public override void BindData(QuestRewardItemModel param)
        {
            this.View.btnClaim.gameObject.SetActive(param.QuestLog.QuestStatus == QuestStatus.Completed);
            this.View.btnClaim.onClick.RemoveAllListeners();

            this.View.btnClaim.onClick.AddListener(() =>
            {
                this.questManager.ShowPopupClaimReward(param.QuestLog);

                param.OnRefresh?.Invoke(param);
            });

            if (param.QuestLog.QuestRecord.Tasks.Count > 0)
            {
                this.View.txtDes.text = param.QuestLog.QuestRecord.Tasks.Last().Description;
            }
        }
    }
}