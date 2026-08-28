namespace GameModule.RuntimeCsvFromDrive.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BlueprintFlow.APIHandler;
    using BlueprintFlow.BlueprintControlFlow;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Localization.Services;
    using FeatureTemplate.Scripts.Services;
    using FeatureTemplate.Scripts.Services.Common;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameModule.RuntimeCsvFromDrive.Scripts.Model;
    using GameModule.RuntimeCsvFromDrive.Scripts.Mono;
    using Google.Apis.Sheets.v4;
    using Google.Apis.Sheets.v4.Data;
    using Newtonsoft.Json;
    using UnityEngine;
    using UnityEngine.Scripting;
    using Zenject;
    using GridProperties = Google.Apis.Sheets.v4.Data.GridProperties;

    public class WebGlBlueprintReaderManager : RuntimeBlueprintReaderManager
    {
        [Preserve]
        public WebGlBlueprintReaderManager(
            ISignalBus signalBus,
            FeatureDataState featureDataState,
            FeatureManuallyUnityEvent featureManuallyUnity,
            LocalizationDataOnline localizationDataOnline,
            ILogService logService,
            DiContainer diContainer,
            IHandleUserDataServices handleUserDataServices,
            BlueprintConfig blueprintConfig,
            FetchBlueprintInfo fetchBlueprintInfo,
            BlueprintDownloader blueprintDownloader)
            : base(
                signalBus,
                featureDataState,
                featureManuallyUnity,
                localizationDataOnline,
                logService,
                diContainer,
                handleUserDataServices,
                blueprintConfig,
                fetchBlueprintInfo,
                blueprintDownloader)
        {
            Debug.Log("[WebGlBlueprintReaderManager] Constructor");
        }

        protected override async UniTask<List<Sheet>> GetAllSheetWithSpreadSheet(SheetsService services, string spreadSheetId = null)
        {
            Debug.Log($"[WebGL Sheets] GetAllSheetWithSpreadSheet START | " + $"SpreadsheetId: {spreadSheetId}");

            spreadSheetId ??= this.csvLoaderData.syncDataInfo.SpreadSheetId;

            Debug.Log($"[WebGL Sheets] Resolved SpreadsheetId: {spreadSheetId}");

            if (string.IsNullOrEmpty(spreadSheetId))
            {
                Debug.LogError("[WebGL Sheets] SpreadsheetId is EMPTY.");

                return new List<Sheet>();
            }

            var serviceAccountJson = this.csvLoaderData.syncDataInfo.ServicesAccountJson;

            Debug.Log($"[WebGL Sheets] ServiceAccountJson exists: " + $"{!string.IsNullOrEmpty(serviceAccountJson)}");

            if (string.IsNullOrEmpty(serviceAccountJson))
            {
                Debug.LogError("[WebGL Sheets] ServiceAccountJson is EMPTY.");

                return new List<Sheet>();
            }

            try
            {
                Debug.Log("[WebGL Sheets] Calling JS -> GetSpreadsheet...");

                var json = await WebGLGoogleSheetsBridge.Instance.GetSpreadsheet(spreadSheetId, serviceAccountJson);

                Debug.Log($"[WebGL Sheets] GetSpreadsheet RESPONSE | " + $"Length: {json?.Length ?? 0}");

                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError(
                        "[WebGL Sheets] GetSpreadsheet returned EMPTY response.");

                    return new List<Sheet>();
                }

                Debug.Log($"[WebGL Sheets] GetSpreadsheet JSON:\n{json}");

                var response = JsonConvert.DeserializeObject<SpreadsheetResponse>(json);

                if (response?.sheets == null)
                {
                    Debug.LogError("[WebGL Sheets] SpreadsheetResponse.sheets is NULL.");

                    return new List<Sheet>();
                }

                Debug.Log($"[WebGL Sheets] Google returned " + $"{response.sheets.Length} sheets.");

                var result =
                    response.sheets
                        .Where(x =>
                            x != null &&
                            x.properties != null)
                        .Select(x =>
                        {
                            var title =
                                x.properties.title;

                            var rowCount =
                                x.properties.gridProperties?.rowCount
                                ?? 1000;

                            var columnCount =
                                x.properties.gridProperties?.columnCount
                                ?? 26;

                            Debug.Log(
                                $"[WebGL Sheets] Sheet found | " +
                                $"Title: {title} | " +
                                $"Rows: {rowCount} | " +
                                $"Columns: {columnCount}");

                            return new Sheet
                            {
                                Properties =
                                    new SheetProperties
                                    {
                                        Title = title,

                                        GridProperties =
                                            new GridProperties
                                            {
                                                RowCount    = rowCount,
                                                ColumnCount = columnCount
                                            }
                                    }
                            };
                        })
                        .ToList();

                Debug.Log($"[WebGL Sheets] GetAllSheetWithSpreadSheet END | " + $"Result count: {result.Count}");

                return result;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                return new List<Sheet>();
            }
        }

        protected override async UniTask<Dictionary<string, CustomValueRange>> GetAllDataForAllSheet(List<Sheet> sheets, string spreadsheetId, SheetsService service)
        {
            Debug.Log($"[WebGL Sheets] GetAllDataForAllSheet START | " + $"SpreadsheetId: {spreadsheetId} | " + $"Sheet count: {sheets?.Count ?? 0}");

            if (sheets == null || sheets.Count == 0)
            {
                Debug.LogWarning("[WebGL Sheets] No sheets to load.");

                return new Dictionary<string, CustomValueRange>();
            }

            var serviceAccountJson = this.csvLoaderData.syncDataInfo.ServicesAccountJson;

            if (string.IsNullOrEmpty(serviceAccountJson))
            {
                Debug.LogError("[WebGL Sheets] ServiceAccountJson is EMPTY.");

                return new Dictionary<string, CustomValueRange>();
            }

            var ranges = sheets.Where(sheet => sheet != null && sheet.Properties != null).Select(sheet =>
                {
                    var title =
                        sheet.Properties.Title;

                    var rowCount =
                        sheet.Properties
                            .GridProperties?.RowCount
                        ?? 1000;

                    var columnCount =
                        sheet.Properties
                            .GridProperties?.ColumnCount
                        ?? 26;

                    var range =
                        $"{EscapeSheetName(title)}!A1:" +
                        $"{this.GetColumnName(columnCount)}" +
                        $"{rowCount}";

                    Debug.Log($"[WebGL Sheets] Build Range | " + $"Sheet: {title} | " + $"Range: {range}");

                    return range;
                })
                .ToList();

            Debug.Log($"[WebGL Sheets] BatchGet range count: {ranges.Count}");

            Debug.Log($"[WebGL Sheets] BatchGet ranges:\n" + string.Join("\n", ranges));

            try
            {
                Debug.Log("[WebGL Sheets] Calling JS -> GetValuesBatch...");

                var json = await WebGLGoogleSheetsBridge.Instance.GetValuesBatch(spreadsheetId, serviceAccountJson, ranges);

                Debug.Log($"[WebGL Sheets] GetValuesBatch RESPONSE | " + $"Length: {json?.Length ?? 0}");

                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError("[WebGL Sheets] GetValuesBatch returned EMPTY response.");

                    return new Dictionary<string, CustomValueRange>();
                }

                Debug.Log($"[WebGL Sheets] GetValuesBatch JSON:\n{json}");

                var response = JsonConvert.DeserializeObject<WebGLBatchGetResponse>(json);

                if (response?.valueRanges == null)
                {
                    Debug.LogError("[WebGL Sheets] Batch response valueRanges is NULL.");

                    return new Dictionary<string, CustomValueRange>();
                }

                Debug.Log($"[WebGL Sheets] Received " + $"{response.valueRanges.Length} value ranges.");

                var result =
                    new Dictionary<string, CustomValueRange>();

                foreach (var valueRange in response.valueRanges)
                {
                    if (valueRange == null)
                    {
                        Debug.LogWarning("[WebGL Sheets] Received NULL valueRange.");

                        continue;
                    }

                    Debug.Log($"[WebGL Sheets] Processing ValueRange | " + $"Range: {valueRange.range} | " + $"MajorDimension: {valueRange.majorDimension} | " +
                              $"Rows: {valueRange.values?.Length ?? 0}");

                    var sheetName = ExtractSheetName(valueRange.range);

                    Debug.Log($"[WebGL Sheets] Extracted SheetName: " + $"{sheetName}");

                    if (string.IsNullOrEmpty(sheetName))
                    {
                        Debug.LogWarning($"[WebGL Sheets] Cannot extract sheet name " + $"from range: {valueRange.range}");

                        continue;
                    }

                    var customValueRange =
                        new CustomValueRange
                        {
                            Range =
                                valueRange.range,

                            MajorDimension =
                                valueRange.majorDimension,

                            ETag =
                                valueRange.etag,

                            Values = valueRange.values?.Select(row => (IList<object>)(row ?? Array.Empty<string>()).Select(cell => (object)cell).ToList())
                                .ToList()
                        };

                    var rowCount = customValueRange.Values?.Count ?? 0;

                    var columnCount = customValueRange.Values?.FirstOrDefault()?.Count ?? 0;

                    Debug.Log($"[WebGL Sheets] Parsed Sheet | " + $"Name: {sheetName} | " + $"Rows: {rowCount} | " + $"Columns: {columnCount}");

                    result[sheetName] =
                        customValueRange;
                }

                Debug.Log($"[WebGL Sheets] Parsed result count: " + $"{result.Count}");

                Debug.Log("[WebGL Sheets] Calling GetCustomValueRange...");

                await this.GetCustomValueRange(result);

                Debug.Log("[WebGL Sheets] GetCustomValueRange DONE.");

                Debug.Log($"[WebGL Sheets] GetAllDataForAllSheet END | " + $"Result count: {result.Count}");

                return result;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                return new Dictionary<string, CustomValueRange>();
            }
        }

        private static string EscapeSheetName(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                Debug.LogWarning("[WebGL Sheets] EscapeSheetName received EMPTY name.");

                return string.Empty;
            }

            var escaped = $"'{sheetName.Replace("'", "''")}'";

            Debug.Log($"[WebGL Sheets] EscapeSheetName | " +
                      $"'{sheetName}' -> '{escaped}'");

            return escaped;
        }

        private static string ExtractSheetName(string range)
        {
            if (string.IsNullOrEmpty(range))
            {
                Debug.LogWarning("[WebGL Sheets] ExtractSheetName received EMPTY range.");

                return string.Empty;
            }

            var bangIndex =
                range.LastIndexOf('!');

            if (bangIndex <= 0)
            {
                Debug.LogWarning($"[WebGL Sheets] Invalid range: {range}");

                return string.Empty;
            }

            var sheetName =
                range.Substring(
                    0,
                    bangIndex);

            if (sheetName.Length >= 2 &&
                sheetName[0] == '\'' &&
                sheetName[sheetName.Length - 1] == '\'')
            {
                sheetName =
                    sheetName.Substring(
                        1,
                        sheetName.Length - 2);

                sheetName =
                    sheetName.Replace("''", "'");
            }

            Debug.Log($"[WebGL Sheets] ExtractSheetName | " + $"'{range}' -> '{sheetName}'");

            return sheetName;
        }
    }
}