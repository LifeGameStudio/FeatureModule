namespace GameModule.DailyReward.Scripts.Blueprints
{
    using BlueprintFlow.BlueprintReader;

    [BlueprintReader("DailyRewardMiscParam")]
    public class DailyRewardMiscParamBlueprint : GenericBlueprintReaderByCol
    {
        /// <summary>
        /// Name of the presenter to show the popup
        /// </summary>
        public string StartOnScreen { get; set; }
        public int TimeLoop { get; set; }
    }
}