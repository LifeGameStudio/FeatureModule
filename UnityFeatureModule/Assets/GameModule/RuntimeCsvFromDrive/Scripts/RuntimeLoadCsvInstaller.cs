namespace GameModule.RuntimeCsvFromDrive.Scripts
{
    using BlueprintFlow.BlueprintControlFlow;
    using GameModule.RuntimeCsvFromDrive.Scripts.Mono;
    using Zenject;
    using Zenject.Internal;

    public class RuntimeLoadCsvInstaller : Installer<RuntimeLoadCsvInstaller>
    {
        [Preserve]
        public RuntimeLoadCsvInstaller() { }

        public override void InstallBindings()
        {
#if UNITY_ANDROID||UNITY_IOS||UNITY_EDITOR
            this.Container.Rebind<BlueprintReaderManager>().To<RuntimeBlueprintReaderManager>().AsCached();

#elif UNITY_WEBGL
            this.Container.Bind<WebGLGoogleSheetsBridge>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
            this.Container.Rebind<BlueprintReaderManager>().To<WebGlBlueprintReaderManager>().AsCached();
#endif
        }
    }
}