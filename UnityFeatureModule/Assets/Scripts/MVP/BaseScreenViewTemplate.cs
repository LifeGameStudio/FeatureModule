namespace Game.Scripts.MVP
{
    using FeatureTemplate.Scripts.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class BaseScreenViewTemplate : FeatureBaseScreenViewTemplate
    {
    }

    public abstract class BaseScreenPresenterTemplate<TView> : FeatureBaseScreenPresenterTemplate<TView> where TView : BaseScreenViewTemplate
    {
        protected BaseScreenPresenterTemplate(ISignalBus signalBus, GameQueueActionContext gameQueueActionContext, ILogService logger, ScreenManager screenManager, SceneDirector sceneDirector) : base(
            signalBus, gameQueueActionContext, logger, screenManager, sceneDirector)
        {
        }
    }

    public abstract class BaseScreenPresenterTemplate<TView, TModel> : FeatureBaseScreenPresenterTemplate<TView, TModel> where TView : BaseScreenViewTemplate
    {
        protected BaseScreenPresenterTemplate(ISignalBus signalBus, GameQueueActionContext gameQueueActionContext, ScreenManager screenManager, SceneDirector sceneDirector, ILogService logger) : base(
            signalBus, gameQueueActionContext, screenManager, sceneDirector, logger)
        {
        }
    }
}