namespace GameModule.Tutorial.Scripts.DataState
{
    using System.Collections.Generic;

    public class TutorialDataState
    {
        public List<TutorialElementDataState> TutorialElementsDataState = new List<TutorialElementDataState>();
        public TutorialElementDataState       CurrentTutorial;
    }
}