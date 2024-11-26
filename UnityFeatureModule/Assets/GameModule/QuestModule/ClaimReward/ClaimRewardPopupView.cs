namespace GameModule.QuestModule.ClaimReward
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.RewardHandle;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.Utilities.LogService;
    using global::ClaimReward;
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    public class ClaimRewardPopupModel
    {
        public List<IRewardRecord> RewardResult = new();
    }

    public class ClaimRewardPopupView : BaseView
    {
        public ClaimRewardAdapter claimRewardAdapter;
        public Button             btnClaim;
    }

    [PopupInfo(nameof(ClaimRewardPopupView), false)]
    public class ClaimRewardPopupPresenter : BasePopupPresenter<ClaimRewardPopupView, ClaimRewardPopupModel>
    {
        private readonly FeatureRewardHandler       featureRewardHandler;
        private readonly ScreenManager              screenManager;
        private readonly DiContainer                diContainer;
        private          List<ClaimRewardItemModel> claimRewardItemModels = new();

        public ClaimRewardPopupPresenter(ISignalBus signalBus, FeatureRewardHandler featureRewardHandler, ScreenManager screenManager, ILogService logService, DiContainer diContainer) : base(signalBus,
            logService)
        {
            this.featureRewardHandler = featureRewardHandler;
            this.screenManager        = screenManager;
            this.diContainer          = diContainer;
        }

        protected override void OnViewReady()
        {
            base.OnViewReady();
            this.View.btnClaim.onClick.AddListener(this.OnClose);
        }

        private void OnClose() { this.CloseView(); }

        public override UniTask BindData(ClaimRewardPopupModel popupModel)
        {
            this.PrepareModel(popupModel);
            this.featureRewardHandler.AddRewards(popupModel.RewardResult.ToList<IRewardRecord>(), null);

            return UniTask.CompletedTask;
        }

        private void PrepareModel(ClaimRewardPopupModel popupModel)
        {
            this.claimRewardItemModels.Clear();

            foreach (var asset in popupModel.RewardResult)
            {
                var claimRewardItemModel = new ClaimRewardItemModel()
                {
                    Asset      = asset,
                    OnShowInfo = this.OnShowInfo
                };

                this.claimRewardItemModels.Add(claimRewardItemModel);
            }

            this.View.claimRewardAdapter.InitItemAdapter(this.claimRewardItemModels, this.diContainer).Forget();
        }

        protected virtual void OnShowInfo(ClaimRewardItemModel obj) { Debug.LogError($"ToDo fill data for AssetInfoPopupModel"); }
    }
}