namespace Game.Scripts.Scene.Loading
{
    using BlueprintFlow.BlueprintControlFlow;
    using Core.AnalyticServices;
    using FeatureTemplate.Scripts.Screen;
    using FeatureTemplate.Scripts.Services;
    using FeatureTemplate.Scripts.Services.Ads;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using Zenject;

    [ScreenInfo(nameof(FeatureLoadingScreenView))]
    public class LoadingScreenPresenter : FeatureLoadingScreenPresenter
    {
        public LoadingScreenPresenter(ISignalBus signalBus, IFeatureAdServices featureAdsServices, BlueprintReaderManager blueprintReaderManager, FeatureUserDataManager userDataManager,
            IAnalyticServices analyticServices, ObjectPoolManager objectPoolManager, IGameAssets gameAssets, GameQueueActionContext gameQueueActionContext, ILogService logger,
            ScreenManager screenManager, SceneDirector sceneDirector) : base(signalBus, featureAdsServices, blueprintReaderManager, userDataManager, analyticServices, objectPoolManager, gameAssets,
            gameQueueActionContext, logger, screenManager, sceneDirector)
        {
        }
    }
}