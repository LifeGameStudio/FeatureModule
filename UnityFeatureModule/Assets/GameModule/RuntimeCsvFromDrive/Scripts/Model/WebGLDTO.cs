namespace GameModule.RuntimeCsvFromDrive.Scripts.Model
{
    using System;

    [Serializable]
    public class SpreadsheetResponse
    {
        public SpreadsheetSheet[] sheets;
    }

    [Serializable]
    public class SpreadsheetSheet
    {
        public SpreadsheetProperties properties;
    }

    [Serializable]
    public class SpreadsheetProperties
    {
        public string         title;
        public GridProperties gridProperties;
    }

    [Serializable]
    public class GridProperties
    {
        public int rowCount;
        public int columnCount;
    }

    [Serializable]
    public class WebGLBatchGetResponse
    {
        public string           spreadsheetId;
        public WebGLValueRange[] valueRanges;
    }

    [Serializable]
    public class WebGLValueRange
    {
        public string range;

        public string majorDimension;

        public string etag;

        /*
         * FIXED.
         *
         * Google returns:
         *
         *   "values": [ ["id","name"], ["1","Kiem go"] ]
         *
         * i.e. an array OF ARRAYS. The previous WebGLRow[] declared each row as
         * a JSON OBJECT with a "values" field, so Newtonsoft threw:
         *
         *   JsonSerializationException: Cannot deserialize the current JSON
         *   array (e.g. [1,2,3]) into type 'WebGLRow' because the type
         *   requires a JSON object to deserialize correctly.
         *
         * That exception was swallowed by the try/catch in
         * GetAllDataForAllSheet, so the symptom was "0 sheets loaded" rather
         * than a visible crash.
         *
         * Cells are strings because the request uses the default
         * valueRenderOption = FORMATTED_VALUE. Newtonsoft still coerces a bare
         * JSON number to string, so numeric cells are safe either way.
         *
         * Note: Google TRIMS trailing empty cells, so rows can be shorter than
         * the header row. That matches the behaviour of the native
         * Google.Apis.Sheets path, so downstream code needs no change.
         */
        public string[][] values;
    }

    /*
     * WebGLRow is deleted on purpose - it was the cause of the bug.
     * If any other file still references it, replace
     *     row.values          ->  row
     *     WebGLRow[] values   ->  string[][] values
     */
}
