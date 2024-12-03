namespace Game.Scripts.UnitTest.ScreenQueueTestView
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using Game.Scripts.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using Zenject;

    public class ViewA : BasePopupViewViewTemplate
    {
        
    }
    [PopupInfo(nameof(ViewA))]
    public class PresenterA : BasePopupPresenterTemplate<ViewA>
    {
        public PresenterA(ISignalBus signalBus, ScreenManager screenManager, SceneDirector sceneDirector) : base(signalBus, screenManager, sceneDirector)
        {
        }

        public override async UniTask BindData()
        {
            this.LogMessage("TestV: Open View A");
            this.WaitForCloseView();
        }

        private async void WaitForCloseView()
        {
            await UniTask.WaitForSeconds(2);
            await this.CloseViewAsync();
            this.LogMessage("TestV: Close View A complete");

        }
    }
}