namespace GameModule.Condition
{
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.Utilities.Extension;
    using Zenject;

    public class ConditionInstaller : Installer<ConditionInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<ConditionHandler>().AsCached().NonLazy();
            this.OnAfterDataLoaded();
        }
        
        private async void OnAfterDataLoaded()
        {
            await UniTask.WaitUntil(() => this.Container.Resolve<FeatureDataState>().IsBlueprintAndLocalDataLoaded);
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<ICondition>();
        }
    }
}