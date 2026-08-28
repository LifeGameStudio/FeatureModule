namespace GameModule.RuntimeCsvFromDrive.Scripts.Mono
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Cysharp.Threading.Tasks;
    using Newtonsoft.Json;
    using UnityEngine;
    using UnityEngine.Scripting;

    public sealed class WebGLGoogleSheetsBridge : MonoBehaviour
    {
        public static WebGLGoogleSheetsBridge Instance { get; private set; }

        private readonly Dictionary<string, UniTaskCompletionSource<string>> pendingRequests =
            new Dictionary<string, UniTaskCompletionSource<string>>();

        private int requestId;

        // =========================================================
        // UNITY LIFECYCLE
        // =========================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[WebGL Sheets Bridge] Duplicate instance destroyed. " +
                    $"GameObject: {this.gameObject.name}");

                Destroy(this.gameObject);

                return;
            }

            Instance = this;

            DontDestroyOnLoad(this.gameObject);

            Debug.Log(
                $"[WebGL Sheets Bridge] Initialized | " +
                $"GameObject: {this.gameObject.name}");
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Debug.Log(
                $"[WebGL Sheets Bridge] Destroyed | " +
                $"PendingRequests: {this.pendingRequests.Count}");

            Instance = null;

            foreach (var pair in this.pendingRequests)
            {
                pair.Value.TrySetException(
                    new Exception(
                        "WebGLGoogleSheetsBridge was destroyed."));
            }

            this.pendingRequests.Clear();
        }

        // =========================================================
        // JAVASCRIPT IMPORT
        // =========================================================

#if UNITY_WEBGL && !UNITY_EDITOR

        /*
         * IMPORTANT
         *
         * This order MUST match WebGLGoogleSheets.jslib.
         *
         * GetSpreadsheet:
         *
         * 1. spreadsheetId
         * 2. gameObjectName
         * 3. serviceAccountJson
         * 4. requestId
         */

        [DllImport("__Internal")]
        private static extern void GoogleSheets_GetSpreadsheet(
            string spreadsheetId,
            string gameObjectName,
            string serviceAccountJson,
            string requestId);

        /*
         * GetValuesBatch:
         *
         * 1. spreadsheetId
         * 2. gameObjectName
         * 3. serviceAccountJson
         * 4. rangesJson
         * 5. requestId
         */

        [DllImport("__Internal")]
        private static extern void GoogleSheets_GetValuesBatch(
            string spreadsheetId,
            string gameObjectName,
            string serviceAccountJson,
            string rangesJson,
            string requestId);

#endif

        // =========================================================
        // GET SPREADSHEET
        // =========================================================

        public UniTask<string> GetSpreadsheet(
            string spreadsheetId,
            string serviceAccountJson)
        {
            var id = (++this.requestId).ToString();

            var tcs =
                new UniTaskCompletionSource<string>();

            this.pendingRequests[id] = tcs;

            Debug.Log(
                $"[WebGL Sheets Bridge] GetSpreadsheet START\n" +
                $"SpreadsheetId: {spreadsheetId}\n" +
                $"RequestId: {id}\n" +
                $"GameObject: {this.gameObject.name}\n" +
                $"ServiceAccountJson Exists: " +
                $"{!string.IsNullOrEmpty(serviceAccountJson)}");

            // -----------------------------------------------------
            // VALIDATE SPREADSHEET ID
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(spreadsheetId))
            {
                this.FailRequest(
                    id,
                    new ArgumentException(
                        "SpreadsheetId is empty."));

                return tcs.Task;
            }

            // -----------------------------------------------------
            // VALIDATE SERVICE ACCOUNT
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(serviceAccountJson))
            {
                this.FailRequest(
                    id,
                    new ArgumentException(
                        "ServiceAccountJson is empty."));

                return tcs.Task;
            }

#if UNITY_WEBGL && !UNITY_EDITOR

            try
            {
                var gameObjectName =
                    this.gameObject.name;

                Debug.Log(
                    $"[WebGL Sheets Bridge] " +
                    $"Calling JS GoogleSheets_GetSpreadsheet\n" +
                    $"SpreadsheetId: {spreadsheetId}\n" +
                    $"GameObject: {gameObjectName}\n" +
                    $"RequestId: {id}");

                /*
                 * DO NOT CHANGE THE ORDER.
                 */

                GoogleSheets_GetSpreadsheet(
                    spreadsheetId,
                    gameObjectName,
                    serviceAccountJson,
                    id);
            }
            catch (Exception exception)
            {
                this.FailRequest(
                    id,
                    exception);
            }

#else

            this.FailRequest(
                id,
                new PlatformNotSupportedException(
                    "Google Sheets WebGL bridge can only run on WebGL."));

#endif

            return tcs.Task;
        }

        // =========================================================
        // GET VALUES BATCH
        // =========================================================

        public UniTask<string> GetValuesBatch(
            string spreadsheetId,
            string serviceAccountJson,
            List<string> ranges)
        {
            var id = (++this.requestId).ToString();

            var tcs =
                new UniTaskCompletionSource<string>();

            this.pendingRequests[id] = tcs;

            Debug.Log(
                $"[WebGL Sheets Bridge] GetValuesBatch START\n" +
                $"SpreadsheetId: {spreadsheetId}\n" +
                $"RequestId: {id}\n" +
                $"GameObject: {this.gameObject.name}\n" +
                $"ServiceAccountJson Exists: " +
                $"{!string.IsNullOrEmpty(serviceAccountJson)}\n" +
                $"RangeCount: {ranges?.Count ?? 0}");

            // -----------------------------------------------------
            // VALIDATE SPREADSHEET ID
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(spreadsheetId))
            {
                this.FailRequest(
                    id,
                    new ArgumentException(
                        "SpreadsheetId is empty."));

                return tcs.Task;
            }

            // -----------------------------------------------------
            // VALIDATE SERVICE ACCOUNT
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(serviceAccountJson))
            {
                this.FailRequest(
                    id,
                    new ArgumentException(
                        "ServiceAccountJson is empty."));

                return tcs.Task;
            }

            // -----------------------------------------------------
            // VALIDATE RANGES
            // -----------------------------------------------------

            if (ranges == null || ranges.Count == 0)
            {
                this.FailRequest(
                    id,
                    new ArgumentException(
                        "Google Sheets ranges is empty."));

                return tcs.Task;
            }

            // -----------------------------------------------------
            // LOG RANGES
            // -----------------------------------------------------

            Debug.Log(
                $"[WebGL Sheets Bridge] GetValuesBatch Ranges:\n" +
                $"{string.Join("\n", ranges)}");

            // -----------------------------------------------------
            // SERIALIZE RANGES
            // -----------------------------------------------------

            var rangesJson =
                JsonUtility.ToJson(
                    new StringArrayWrapper
                    {
                        values = ranges.ToArray()
                    });

            Debug.Log(
                $"[WebGL Sheets Bridge] GetValuesBatch RangesJson:\n" +
                $"{rangesJson}");

#if UNITY_WEBGL && !UNITY_EDITOR

            try
            {
                var gameObjectName =
                    this.gameObject.name;

                Debug.Log(
                    $"[WebGL Sheets Bridge] " +
                    $"Calling JS GoogleSheets_GetValuesBatch\n" +
                    $"SpreadsheetId: {spreadsheetId}\n" +
                    $"GameObject: {gameObjectName}\n" +
                    $"RequestId: {id}\n" +
                    $"RangeCount: {ranges.Count}");

                /*
                 * DO NOT CHANGE THE ORDER.
                 */

                GoogleSheets_GetValuesBatch(
                    spreadsheetId,
                    gameObjectName,
                    serviceAccountJson,
                    rangesJson,
                    id);
            }
            catch (Exception exception)
            {
                this.FailRequest(
                    id,
                    exception);
            }

#else

            this.FailRequest(
                id,
                new PlatformNotSupportedException(
                    "Google Sheets WebGL bridge can only run on WebGL."));

#endif

            return tcs.Task;
        }

        // =========================================================
        // JAVASCRIPT CALLBACK
        // =========================================================

        [Preserve]
        public void OnGoogleSheetsResponse(string json)
        {
            Debug.Log(
                $"[WebGL Sheets Bridge] RESPONSE:\n{json}");

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError(
                    "[WebGL Sheets Bridge] " +
                    "Received EMPTY response.");

                return;
            }

            WebGLResponse response;

            try
            {
                response =
                    JsonConvert.DeserializeObject<WebGLResponse>(
                        json);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[WebGL Sheets Bridge] " +
                    "Failed to deserialize response.\n" +
                    $"JSON:\n{json}");

                Debug.LogException(exception);

                return;
            }

            if (response == null)
            {
                Debug.LogError(
                    "[WebGL Sheets Bridge] " +
                    "Response is NULL.");

                return;
            }

            Debug.Log(
                $"[WebGL Sheets Bridge] Response parsed | " +
                $"RequestId: {response.requestId} | " +
                $"Success: {response.success}");

            if (string.IsNullOrEmpty(response.requestId))
            {
                Debug.LogError(
                    "[WebGL Sheets Bridge] " +
                    "Response RequestId is EMPTY.");

                return;
            }

            // -----------------------------------------------------
            // FIND REQUEST
            // -----------------------------------------------------

            if (!this.pendingRequests.TryGetValue(
                    response.requestId,
                    out var tcs))
            {
                Debug.LogWarning(
                    $"[WebGL Sheets Bridge] " +
                    $"Unknown RequestId: {response.requestId}");

                return;
            }

            this.pendingRequests.Remove(
                response.requestId);

            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            if (response.success)
            {
                Debug.Log(
                    $"[WebGL Sheets Bridge] " +
                    $"Request SUCCESS\n" +
                    $"RequestId: {response.requestId}\n" +
                    $"DataLength: " +
                    $"{response.dataJson?.Length ?? 0}");

                tcs.TrySetResult(
                    response.dataJson);

                return;
            }

            // -----------------------------------------------------
            // ERROR
            // -----------------------------------------------------

            var error =
                string.IsNullOrEmpty(response.error)
                    ? "Unknown Google Sheets error."
                    : response.error;

            Debug.LogError(
                $"[WebGL Sheets Bridge] " +
                $"Request FAILED\n" +
                $"RequestId: {response.requestId}\n" +
                $"Error: {error}");

            tcs.TrySetException(
                new Exception(error));
        }

        // =========================================================
        // FAIL REQUEST
        // =========================================================

        private void FailRequest(
            string id,
            Exception exception)
        {
            if (!this.pendingRequests.TryGetValue(
                    id,
                    out var tcs))
            {
                Debug.LogError(
                    $"[WebGL Sheets Bridge] " +
                    $"Cannot fail request because RequestId " +
                    $"{id} does not exist.");

                return;
            }

            this.pendingRequests.Remove(id);

            Debug.LogError(
                $"[WebGL Sheets Bridge] " +
                $"Request FAILED BEFORE CALLBACK\n" +
                $"RequestId: {id}\n" +
                $"Error: {exception.Message}");

            Debug.LogException(exception);

            tcs.TrySetException(exception);
        }

        // =========================================================
        // DTO
        // =========================================================

        [Serializable]
        private sealed class StringArrayWrapper
        {
            public string[] values;
        }

        [Serializable]
        private sealed class WebGLResponse
        {
            public string requestId;

            public bool success;

            public string error;

            public string dataJson;
        }
    }
}