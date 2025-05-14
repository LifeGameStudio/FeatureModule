namespace GameModule.RuntimeCsvFromDrive.Scripts
{
    using BlueprintFlow.BlueprintControlFlow;
    using Zenject;
    using Zenject.Internal;

    public class RuntimeLoadCsvInstaller : Installer<RuntimeLoadCsvInstaller>
    {
        [Preserve]
        public RuntimeLoadCsvInstaller() { }

        public override void InstallBindings()
        {
            this.Container.Rebind<BlueprintReaderManager>().To<RuntimeBlueprintReaderManager>().AsCached();
            this.Container.Bind<CsvLoaderData>().FromScriptableObjectResource(nameof(CsvLoaderData)).WhenInjectedInto<RuntimeBlueprintReaderManager>();
        }
    }
}