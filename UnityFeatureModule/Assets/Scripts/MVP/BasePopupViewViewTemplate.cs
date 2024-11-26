namespace Game.Scripts.MVP
{
    using FeatureTemplate.Scripts.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class BasePopupViewViewTemplate : FeatureBasePopupViewTemplate
    {
    }

    public abstract class BasePopupPresenterTemplate<TView> : FeatureBasePopupPresenterTemplate<TView> where TView : BasePopupViewViewTemplate
    {
        protected BasePopupPresenterTemplate(ISignalBus signalBus, ScreenManager screenManager, SceneDirector sceneDirector) : base(signalBus, screenManager, sceneDirector) { }
    }

    public abstract class BasePopupPresenterTemplate<TView, TModel> : FeatureBasePopupScreenPresenterTemplate<TView, TModel> where TView : BasePopupViewViewTemplate
    {
        protected BasePopupPresenterTemplate(ISignalBus signalBus, ScreenManager screenManager, SceneDirector sceneDirector, ILogService logger) : base(signalBus, screenManager, sceneDirector, logger)
        {
        }
    }
}