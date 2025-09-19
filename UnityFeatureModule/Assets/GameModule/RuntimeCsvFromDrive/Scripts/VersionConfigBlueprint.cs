namespace GameModule.RuntimeCsvFromDrive.Scripts
{
    using System;
    using System.Collections.Generic;
    using BlueprintFlow.BlueprintReader;

    [BlueprintReader("", blueprintScope: BlueprintScope.Ignore)]
    public class VersionConfigBlueprint : GenericBlueprintReaderByRow<string, VersionConfigRecord>
    {
    }

    [CsvHeaderKey("Id")]
    public class VersionConfigRecord
    {
        public string Id              { get; set; }
        public bool   AllowAllVersion { get; set; }
        public bool   UseInclude      { get; set; }

        public BlueprintByRow<PlatformVersionConfigRecord> PlatformVersionConfigs { get; set; }
    }

    [Serializable]
    public class PlatformVersionConfigRecord
    {
        public bool         Include        { get; set; }
        public List<string> AndroidVersion { get; set; }
        public List<string> IOSVersion     { get; set; }
    }
}