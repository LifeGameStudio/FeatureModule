namespace GameModule.RuntimeCsvFromDrive.Scripts.Blueprints
{
    using BlueprintFlow.BlueprintReader;

    [BlueprintReader("BlueprintConfig", blueprintScope: BlueprintScope.Ignore)]
    public class BlueprintConfigOnline : GenericBlueprintReaderByRow<int, BlueprintConfigData>
    {
    }

    [CsvHeaderKey("Id")]
    public class BlueprintConfigData
    {
        public int    Id;
        public string BundleVersion;
    }
}