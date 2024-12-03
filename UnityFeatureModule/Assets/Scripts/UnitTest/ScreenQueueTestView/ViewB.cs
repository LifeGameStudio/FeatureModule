namespace Game.Scripts.UnitTest.ScreenQueueTestView
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using Game.Scripts.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class ViewBModel
    {
        
    }
    
    public class ViewB : BasePopupViewViewTemplate
    {
        
    }
    [PopupInfo(nameof(ViewB))]
    public class PresenterB : BasePopupPresenterTemplate<ViewB,ViewBModel>
    {
        public PresenterB(ISignalBus signalBus, ScreenManager screenManager, SceneDirector sceneDirector, ILogService logger) : base(signalBus, screenManager, sceneDirector, logger)
        {
        }

        public override async UniTask BindData(ViewBModel popupModel)
        {
            this.LogMessage("TestV: Open View B");
            this.WaitForCloseView();
        }

        private async void WaitForCloseView()
        {
            await UniTask.WaitForSeconds(4);
            await this.CloseViewAsync();
            this.LogMessage("TestV: Close View B complete");
        }
    }
}