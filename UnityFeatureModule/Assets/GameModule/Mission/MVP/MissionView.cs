namespace GameModule.Mission.MVP
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.MVP;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using UnityEngine;
    using Zenject;

    public class MissionView : FeatureBasePopupViewTemplate
    {
    }

    [PopupInfo("MissionView")]
    public class MissionPresenter : FeatureBasePopupPresenterTemplate<MissionView>
    {
        private readonly MissionService missionService;

        public MissionPresenter(ISignalBus signalBus, MissionService missionService, ScreenManager screenManager, SceneDirector sceneDirector) : base(signalBus, screenManager, sceneDirector)
        {
            this.missionService = missionService;
        }

        public override UniTask BindData()
        {
            // test function List mission
            this.missionService.GetMissions().ForEach(mission => this.LogMessage(mission.Title, Color.cyan));

            return UniTask.CompletedTask;
        }
    }
}