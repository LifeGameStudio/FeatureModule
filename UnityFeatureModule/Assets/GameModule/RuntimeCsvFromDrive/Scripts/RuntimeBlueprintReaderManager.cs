namespace GameModule.RuntimeCsvFromDrive.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using BlueprintFlow.APIHandler;
    using BlueprintFlow.BlueprintControlFlow;
    using BlueprintFlow.BlueprintReader;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Blueprints;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.UserData;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Services;
    using Google.Apis.Sheets.v4;
    using Google.Apis.Sheets.v4.Data;
    using UnityEngine.Scripting;
    using Zenject;

    public class RuntimeBlueprintReaderManager : BlueprintReaderManager
    {
        private readonly CsvLoaderData          csvLoaderData;
        private readonly ILogService            logService;
        private readonly BlueprintConfig        blueprintConfig;
        private          BuilderConfigBlueprint builderConfig;

        [Preserve]
        public RuntimeBlueprintReaderManager(ISignalBus signalBus, CsvLoaderData csvLoaderData, ILogService logService, DiContainer diContainer, IHandleUserDataServices handleUserDataServices,
            BlueprintConfig blueprintConfig,
            FetchBlueprintInfo fetchBlueprintInfo, BlueprintDownloader blueprintDownloader) : base(signalBus, logService, diContainer, handleUserDataServices, blueprintConfig, fetchBlueprintInfo,
            blueprintDownloader)
        {
            this.csvLoaderData   = csvLoaderData;
            this.logService      = logService;
            this.blueprintConfig = blueprintConfig;
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

        private async UniTask LoadToTalSheet(Dictionary<string, string> input)
        {
            this.builderConfig = new BuilderConfigBlueprint();            
            var service = this.GetSheetsService();

            var listSheets = await this.GetAllSheetWithSpreadSheet(service);

            var allData = await this.GetAllDataForAllSheet(listSheets, this.csvLoaderData.SpreadSheetId, service);

            var csvBuilder = this.GetDataFromSheetName(this.csvLoaderData.BuilderConfig,
                allData);

            await this.builderConfig.DeserializeFromCsv(csvBuilder);

            foreach (var s in this.builderConfig.First().Value.Blueprints)
            {
                var csvOfSheet = this.GetDataFromSheetName(s, allData);

                if (string.IsNullOrEmpty(csvOfSheet)) continue;
                input.Add($"{s}{this.blueprintConfig.BlueprintFileType}", csvOfSheet);
            }
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

        private string GetDataFromSheetName(string sheetName, Dictionary<string, ValueRange> allSheetDatas)
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

        private string EscapeCsvValue(string value)
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

        private async UniTask<Dictionary<string, ValueRange>> GetAllDataForAllSheet(List<Sheet> sheets, string spreadsheetId, SheetsService service)
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

            return valueRangeDict;
        }

        private string GetColumnName(int columnIndex)
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

        private async UniTask<List<Sheet>> GetAllSheetWithSpreadSheet(SheetsService services, string spreadSheetId = null)
        {
            spreadSheetId ??= this.csvLoaderData.SpreadSheetId;
            var request  = services.Spreadsheets.Get(spreadSheetId);
            var response = await request.ExecuteAsync();
            var sheets   = response.Sheets;

            return sheets.ToList();
        }

        private SheetsService GetSheetsService()
        {
            var json   = this.csvLoaderData.ServicesAccount;
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