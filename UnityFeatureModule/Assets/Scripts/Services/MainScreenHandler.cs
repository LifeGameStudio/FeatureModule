namespace Game.Scripts.Services
{
    using Cysharp.Threading.Tasks;
    using Game.Scripts.Scene.Home;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using Zenject;

    public class MainScreenHandler : IInitializable
    {
        private readonly ScreenManager screenManager;

        public MainScreenHandler(ScreenManager screenManager) { this.screenManager = screenManager; }

        public void Initialize()
        {
            this.screenManager.OpenScreen<MainScreenPresenter>().Forget();
        }
    }
}