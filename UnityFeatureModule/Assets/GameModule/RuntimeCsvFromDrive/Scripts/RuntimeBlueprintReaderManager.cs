namespace GameModule.RuntimeCsvFromDrive.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using BlueprintFlow.APIHandler;
    using BlueprintFlow.BlueprintControlFlow;
    using BlueprintFlow.BlueprintReader;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate._3rdPlugins.SyncGoogleDriver.Scripts;
    using FeatureTemplate.Scripts.InterfacesAndEnumCommon;
    using FeatureTemplate.Scripts.Localization.Interfaces;
    using FeatureTemplate.Scripts.Localization.Services;
    using FeatureTemplate.Scripts.Services;
    using FeatureTemplate.Scripts.Services.Common;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameModule.RuntimeCsvFromDrive.Scripts.Blueprints;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Services;
    using Google.Apis.Sheets.v4;
    using Google.Apis.Sheets.v4.Data;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Scripting;
    using Zenject;
    using Color = UnityEngine.Color;
    using Object = UnityEngine.Object;

    public class RuntimeBlueprintReaderManager : BlueprintReaderManager, IStartable
    {
        private          FeatureSyncCsvWithGoogleDriver                     csvLoaderData;
        private readonly LocalizationDataOnline                             localizationDataOnline;
        private readonly ILogService                                        logService;
        private readonly BlueprintFlow.BlueprintControlFlow.BlueprintConfig blueprintConfig;
        private          BuilderConfigBlueprint                             builderConfig;

        protected string LocalizationSheetName;
        protected string LocalizationBlueprintSheetName;

        [Preserve]
        public RuntimeBlueprintReaderManager(ISignalBus signalBus, FeatureManuallyUnityEvent featureManuallyUnity, LocalizationDataOnline localizationDataOnline, ILogService logService,
            DiContainer diContainer,
            IHandleUserDataServices handleUserDataServices,
            BlueprintFlow.BlueprintControlFlow.BlueprintConfig blueprintConfig,
            FetchBlueprintInfo fetchBlueprintInfo, BlueprintDownloader blueprintDownloader) : base(signalBus, logService, diContainer, handleUserDataServices, blueprintConfig, fetchBlueprintInfo,
            blueprintDownloader)
        {
            this.localizationDataOnline = localizationDataOnline;
            this.logService             = logService;
            this.blueprintConfig        = blueprintConfig;
            featureManuallyUnity.AddStart(this);
        }

        public void Initialize()
        {
            this.csvLoaderData                  = Resources.Load<FeatureSyncCsvWithGoogleDriver>("SyncGoogleDriver");
            this.LocalizationSheetName          = this.csvLoaderData.syncDataInfo.LocalizationSheetName;
            this.LocalizationBlueprintSheetName = this.csvLoaderData.syncDataInfo.LocalizationBlueprintSheetName;
        }

        protected override async UniTask LoadRawBlueprint(Dictionary<string, string> input)
        {
            try
            {
                await this.LoadToTalSheet(input);
            }
            catch (Exception e)
            {
                this.logService.Error($"Blueprint Load Online Error {e.Message}");
            }
        }

        protected virtual async UniTask LoadToTalSheet(Dictionary<string, string> input)
        {
            this.builderConfig = new BuilderConfigBlueprint();
            var service = this.GetSheetsService();

            var listSheets = await this.GetAllSheetWithSpreadSheet(service);

            var allData = await this.GetAllDataForAllSheet(listSheets, this.csvLoaderData.syncDataInfo.SpreadSheetId, service);

            var csvBuilder = this.GetDataFromSheetName(this.csvLoaderData.syncDataInfo.SheetBuilderConfig,
                allData);

            if (allData.ContainsKey(this.csvLoaderData.syncDataInfo.SheetConfigVersion))
            {
                var configVersion    = new BlueprintConfigOnline();
                var configVersionCsv = this.GetDataFromSheetName(this.csvLoaderData.syncDataInfo.SheetConfigVersion, allData);

                if (!configVersionCsv.IsNullOrEmpty())
                {
                    await configVersion.DeserializeFromCsv(configVersionCsv);

                    if (!this.AllowLoadOnline(configVersion))
                    {
                        return;
                    }
                }
            }

            await this.builderConfig.DeserializeFromCsv(csvBuilder);

            foreach (var s in this.builderConfig.First().Value.Blueprints)
            {
                var csvOfSheet = this.GetDataFromSheetName(s, allData);

                if (string.IsNullOrEmpty(csvOfSheet)) continue;
                input.Add($"{s}{this.blueprintConfig.BlueprintFileType}", csvOfSheet);
            }
        }

        private bool AllowLoadOnline(BlueprintConfigOnline blueprintConfigOnlineVersion)
        {
            var bundleVersion = Object.FindFirstObjectByType<FeatureGameVersion>(FindObjectsInactive.Include);
            var value         = "";
            var text          = bundleVersion.GetComponentInChildren<TextMeshProUGUI>(true);

            if (text != null)
            {
                value = text.text;
                var start = value.IndexOf("Build:", StringComparison.Ordinal);

                var sub = value.Substring(start);

                var lastDash = sub.LastIndexOf('-');
                value = sub.Substring(0, lastDash).Trim();
                value = Regex.Replace(value, @"\s+", "");
            }

            if (value.IsNullOrEmpty())
            {
                return true;
            }

            foreach (var c in blueprintConfigOnlineVersion.Values)
            {
                if (c.BundleVersion.ToLower().Equals(value.ToLower()))
                {
                    this.LogMessage($"Block load online with {c.BundleVersion}", Color.red);

                    return false;
                }
            }

            return true;
        }

        protected override UniTask<string> CheckToLoadCsv(Dictionary<string, string> listRawBlueprints, BlueprintReaderAttribute bpAttribute, bool resourceMode, bool attributeMode)
        {
            if (this.builderConfig.Count > 1)
            {
                var itemRecord = this.builderConfig.ElementAt(1).Value;

                if (itemRecord.Blueprints.Contains(bpAttribute.DataPath))
                {
                    attributeMode = false;
                }
            }
            else
            {
                if (listRawBlueprints.ContainsKey(bpAttribute.DataPath))
                {
                    attributeMode = false;
                }
            }

            return base.CheckToLoadCsv(listRawBlueprints, bpAttribute, false, attributeMode);
        }

        protected virtual string GetDataFromSheetName(string sheetName, Dictionary<string, CustomValueRange> allSheetDatas)
        {
            var csvOutput = "";

            var values = allSheetDatas[sheetName].Values;

            if (values is { Count: > 0 })
            {
                using var writer = new StringWriter();

                var headerRow = values[0];
                writer.WriteLine(string.Join(",", headerRow.Select(cell => this.EscapeCsvValue(cell?.ToString() ?? string.Empty))));

                for (var i = 1; i < values.Count; i++)
                {
                    var row = values[i];

                    if (row == null || row.All(cell => cell == null)) continue;

                    {
                        var csvRow = row.Select(cell => cell?.ToString() != null ? this.EscapeCsvValue(cell.ToString()) : string.Empty);
                        writer.WriteLine(string.Join(",", csvRow));
                    }
                }

                csvOutput = writer.ToString();
            }

            return csvOutput;
        }

        protected virtual string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                value = "\"" + value + "\"";
            }

            return value;
        }

        protected virtual async UniTask<Dictionary<string, CustomValueRange>> GetAllDataForAllSheet(List<Sheet> sheets, string spreadsheetId, SheetsService service)
        {
            const int batchSize        = 50;
            var       valueRangeDict   = new Dictionary<string, ValueRange>();
            var       rangeToSheetName = new Dictionary<string, string>();
            var       ranges           = new List<string>();

            foreach (var s in sheets)
            {
                var sheetName = s.Properties.Title;

                var numRows = (int)(s.Properties.GridProperties.RowCount ?? 1000);
                var numCols = (int)(s.Properties.GridProperties.ColumnCount ?? 26);
                var range   = $"{sheetName}!A1:{GetColumnName(numCols)}{numRows}";

                ranges.Add(range);
                rangeToSheetName[range] = sheetName;
            }

            var totalBatches = (int)Math.Ceiling(ranges.Count / (float)batchSize);

            for (var i = 0; i < ranges.Count; i += batchSize)
            {
                var batchIndex = i / batchSize;
                var batch      = ranges.Skip(i).Take(batchSize).ToList();

                try
                {
                    var request = service.Spreadsheets.Values.BatchGet(spreadsheetId);
                    request.Ranges = batch;
                    var response = await request.ExecuteAsync();

                    if (response?.ValueRanges != null)
                    {
                        foreach (var valueRange in response.ValueRanges)
                        {
                            var key = valueRange.Range;
                            key = key.Replace("'", string.Empty);

                            if (rangeToSheetName.TryGetValue(key, out var sheetName))
                            {
                                valueRangeDict[sheetName] = valueRange;
                            }
                        }
                    }

                    var progress = ((float)(batchIndex + 1) / totalBatches) * 100;
                }
                catch (Exception ex)
                {
                    // ignored
                }

                await UniTask.Delay(100);
            }

            var input = valueRangeDict.ToDictionary(kvp => kvp.Key, kvp => new CustomValueRange
            {
                Values         = kvp.Value.Values,
                ETag           = kvp.Value.ETag,
                Range          = kvp.Value.Range,
                MajorDimension = kvp.Value.MajorDimension
            });

            await this.GetCustomValueRange(input);

            return input;
        }

        protected virtual async UniTask GetCustomValueRange(Dictionary<string, CustomValueRange> input)
        {
#if UNITY_LOCALIZATION
            var localizationTable = input.First(x => x.Key.Equals(this.LocalizationSheetName));
            var localizationLanguage = input.FirstOrDefault(x => x.Key.Equals(this.LocalizationBlueprintSheetName));

            var instance = new FeatureTemplate.Scripts.Localization.Blueprint.LocalizationLanguageBlueprint();
            var csvOfSheet = this.GetDataFromSheetName(localizationLanguage.Key, input);
            await instance.DeserializeFromCsv(csvOfSheet);

            var tmp = new Dictionary<string, string>();

            foreach (var item in instance)
            {
                this.localizationDataOnline.LocalizationDatas[item.Key] = new LocalizationDataModel();
            }

            for (var i = 0; i < localizationTable.Value.Values.Count; i++)
            {
                var listData = localizationTable.Value.Values[i];
                var current = listData.Select(x => x.ToString()).ToList();

                if (i == 0)
                {
                    for (var index = 0; index < current.Count; index++)
                    {
                        if (index == 0) continue;
                        var realKey = current[index];
                        var findItem = instance.FirstOrDefault(x => x.Value.FullName.Equals(realKey));
                        tmp.Add(findItem.Value.FullName, findItem.Key);
                    }

                    continue;
                }

                for (var index = 0; index < current.Count; index++)
                {
                    if (index == 0) continue;
                    var findKey = tmp.ElementAt(index - 1).Value;
                    this.localizationDataOnline.LocalizationDatas[findKey].LocalizedTexts.Add(current[0], current[index]);
                }
            }
#endif
        }

        protected virtual string GetColumnName(int columnIndex)
        {
            columnIndex--;
            var columnName = "";

            while (columnIndex >= 0)
            {
                int remainder = columnIndex % 26;
                columnName  = (char)(remainder + 'A') + columnName;
                columnIndex = (columnIndex / 26) - 1;
            }

            return columnName;
        }

        protected virtual async UniTask<List<Sheet>> GetAllSheetWithSpreadSheet(SheetsService services, string spreadSheetId = null)
        {
            spreadSheetId ??= this.csvLoaderData.syncDataInfo.SpreadSheetId;
            var request  = services.Spreadsheets.Get(spreadSheetId);
            var response = await request.ExecuteAsync();
            var sheets   = response.Sheets;

            return sheets.ToList();
        }

        protected virtual SheetsService GetSheetsService()
        {
            var json   = this.csvLoaderData.syncDataInfo.ServicesAccountJson;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var credential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets, SheetsService.Scope.Drive);

            // Create Google Sheets API service.
            var getSheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName       = "UnityGoogleSheet",
            });

            return getSheetsService;
        }
    }
}