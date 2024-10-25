namespace GameModule.DailyReward.Scripts.RewardSlotItem
{
    using FeatureTemplate.Scripts.MVP;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.Utilities.LoadImage;
    using GameModule.DailyReward.Scripts.Blueprints;
    using UnityEngine.UI;
    using Zenject;

    public class RewardSlotModel
    {
        public Reward            Reward;
    }

    public class RewardSlotView : FeatureBaseItemViewTemplate
    {
        public Image image;
    }

    public class RewardSlotPresenter : FeatureBaseItemPresenterTemplate<RewardSlotView, RewardSlotModel>
    {
        private readonly LoadImageHelper loadImageHelper;

        public RewardSlotPresenter(SignalBus signalBus, LoadImageHelper loadImageHelper, IGameAssets gameAssets) : base(signalBus, gameAssets) { this.loadImageHelper = loadImageHelper; }

        public override async void BindData(RewardSlotModel param)
        {
            base.BindData(param);
            this.View.image.sprite = await this.loadImageHelper.LoadLocalSprite(param.Reward.IconPath);
        }
    }
}