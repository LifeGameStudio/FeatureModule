#if TUTORIAL_ENABLE

namespace GameModule.Tutorial.Scripts.LocalData
{
    using System;
    using System.Collections.Generic;
    using FeatureTemplate.Scripts.InterfacesAndEnumCommon;
    using FeatureTemplate.Scripts.Models.Controllers;
    using GameFoundation.Scripts.Interfaces;

    public class TutorialData : ILocalData, IFeatureLocalData
    {
        public HashSet<int> ListTutorialCompleted = new();
        public Type         ControllerType => typeof(TutorialControllerData);
    }

    public class TutorialControllerData : BaseDataController<TutorialData>
    {
        public TutorialControllerData(TutorialData data) : base(data) { }

        public bool IsTutorialCompleted(int tutorialId) { return this.Data.ListTutorialCompleted.Contains(tutorialId); }

        public void CompleteTutorial(int tutorialId)
        {
            if (this.Data.ListTutorialCompleted.Contains(tutorialId))
            {
                return;
            }

            this.Data.ListTutorialCompleted.Add(tutorialId);
        }
    }
}
#endif