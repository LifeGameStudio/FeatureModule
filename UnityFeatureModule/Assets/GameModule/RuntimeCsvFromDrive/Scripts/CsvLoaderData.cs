namespace GameModule.RuntimeCsvFromDrive.Scripts
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "CsvLoaderData", menuName = "GameModule/RuntimeCsvFromDrive/CsvLoaderData")]
    public class CsvLoaderData : ScriptableObject
    {
        [field: SerializeField] public string SpreadSheetId   { get; set; }
        [field: SerializeField] public string ServicesAccount { get; set; }
        [field: SerializeField] public string BuilderConfig   { get; set; } = "BuilderConfig";
    }
}