namespace Game.Scripts.UnitTest.ScreenQueueTestView
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using Game.Scripts.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using Zenject;

    public class ViewC : BasePopupViewViewTemplate
    {
        
    }
    [PopupInfo(nameof(ViewC))]
    public class PresenterC : BasePopupPresenterTemplate<ViewC>
    {
        public PresenterC(ISignalBus signalBus, ScreenManager screenManager, SceneDirector sceneDirector) : base(signalBus, screenManager, sceneDirector)
        {
        }

        public override async UniTask BindData()
        {
            this.LogMessage("TestV: Open View C");
            this.WaitForCloseView();
        }
        
        private async void WaitForCloseView()
        {
            await UniTask.WaitForSeconds(2);
            await this.CloseViewAsync();
            this.LogMessage("TestV: Close View C complete");
        }
    }
}