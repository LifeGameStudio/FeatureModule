namespace Game.Scripts.MVP
{
    using FeatureTemplate.Scripts.MVP;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.MVP;
    using Zenject;

    public abstract class BaseItemViewTemplate : FeatureBaseItemViewTemplate
    {
    }

    public abstract class BaseItemPresenterTemplate<TView> : FeatureBaseItemPresenterTemplate<TView> where TView : TViewMono
    {
        protected BaseItemPresenterTemplate(ISignalBus signalBus, IGameAssets gameAssets) : base(signalBus, gameAssets)
        {
        }
    }

    public abstract class BaseItemPresenterTemplate<TView, TModel> : FeatureBaseItemPresenterTemplate<TView, TModel> where TView : TViewMono
    {
        protected BaseItemPresenterTemplate(ISignalBus signalBus, IGameAssets gameAssets) : base(signalBus, gameAssets)
        {
        }
    }
}