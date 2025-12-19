#if TUTORIAL_ENABLE


namespace GameModule.Tutorial.Scripts.Blueprints
{
    using BlueprintFlow.BlueprintReader;

    [BlueprintReader("TutorialConfig")]
    public class TutorialConfig:GenericBlueprintReaderByCol
    {
        public bool EnableTutorials { get; set; }
    }
}
#endif