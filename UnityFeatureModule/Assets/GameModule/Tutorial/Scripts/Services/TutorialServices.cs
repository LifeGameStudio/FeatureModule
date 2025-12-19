#if TUTORIAL_ENABLE
namespace GameModule.Tutorial.Scripts.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using GameModule.Tutorial.Scripts.Blueprints;
    using GameModule.Tutorial.Scripts.DataState;
    using GameModule.Tutorial.Scripts.LocalData;
    using GameModule.Tutorial.Scripts.Signals;
    using GameModule.Tutorial.Scripts.TaskStateFlow.Requirement;
    using UnityEngine;
    using UnityEngine.Scripting;
    using Zenject;

    public class TutorialServices : IInitializable, ITickable, IDisposable
    {
        private readonly TutorialDataState      tutorialDataState;
        private readonly ISignalBus             signalBus;
        private readonly TutorialConfig         tutorialConfig;
        private readonly TutorialControllerData tutorialControllerData;
        private readonly FeatureDataState       featureDataState;
        private readonly TutorialBlueprint      tutorialBlueprint;

        private Dictionary<string, ITaskRequirement> cacheTaskRequirements = new();

        [Preserve]
        public TutorialServices(TutorialDataState tutorialDataState, List<ITaskRequirement> taskRequirements, ISignalBus signalBus, TutorialConfig tutorialConfig,
            TutorialControllerData tutorialControllerData,
            FeatureDataState featureDataState,
            TutorialBlueprint tutorialBlueprint)
        {
            this.tutorialDataState      = tutorialDataState;
            this.signalBus              = signalBus;
            this.tutorialConfig         = tutorialConfig;
            this.tutorialControllerData = tutorialControllerData;
            this.featureDataState       = featureDataState;
            this.tutorialBlueprint      = tutorialBlueprint;

            foreach (var requirement in taskRequirements)
            {
                this.cacheTaskRequirements.Add(requirement.Id, requirement);
            }
        }

        public void Initialize()
        {
            this.signalBus.Subscribe<TaskCompleteSignal>(this.OnTaskComplete);
            this.LoadAndStartTutorial().Forget();
        }

        private void OnTaskComplete(TaskCompleteSignal obj)
        {
            this.tutorialDataState.CurrentTutorial.CurrentCompleteTask++;
            this.tutorialDataState.CurrentTutorial.TaskIndex++;
            var taskIndex    = this.tutorialDataState.CurrentTutorial.TaskIndex;
            var maxTaskIndex = this.tutorialDataState.CurrentTutorial.TaskDataStates.Count;

            //Check Complete Tutorial
            var allTaskCompleted = this.tutorialDataState.CurrentTutorial.TaskDataStates
                .Where(x => !x.TaskRecord.IsTaskOptional)
                .All(x => x.TaskState == TutorialState.Completed);

            if (allTaskCompleted && this.tutorialDataState.CurrentTutorial.TutorialState is not TutorialState.Completed)
            {
                this.tutorialDataState.CurrentTutorial.TutorialState = TutorialState.Completed;
                this.tutorialControllerData.CompleteTutorial(this.tutorialDataState.CurrentTutorial.TutorialRecord.Id);
                this.LogMessage($"Tutorial {this.tutorialDataState.CurrentTutorial.TutorialRecord.Id} completed.", Color.red);
            }

            if (taskIndex < maxTaskIndex)
            {
                this.StartTask(this.tutorialDataState.CurrentTutorial, taskIndex);
            }
            else
            {
                //Move to next Tutorial
                var currentTutorialIndex = this.tutorialDataState.TutorialElementsDataState.IndexOf(this.tutorialDataState.CurrentTutorial);
                var nextTutorialIndex    = currentTutorialIndex + 1;

                if (nextTutorialIndex < this.tutorialDataState.TutorialElementsDataState.Count)
                {
                    this.tutorialDataState.CurrentTutorial = this.tutorialDataState.TutorialElementsDataState[nextTutorialIndex];
                    this.StartTask(this.tutorialDataState.CurrentTutorial, 0);
                }
            }
        }

        public void Dispose() { }

        private async UniTaskVoid LoadAndStartTutorial()
        {
            await UniTask.WaitUntil(() => this.featureDataState.IsBlueprintAndLocalDataLoaded);

            if (!this.tutorialConfig.EnableTutorials) return;
            var records = this.tutorialBlueprint.Values.OrderBy(x => x.Order).ToList();

            foreach (var tutorial in records)
            {
                if (this.tutorialControllerData.IsTutorialCompleted(tutorial.Id)) continue;

                var taskListState = new List<TutorialTaskDataState>();

                foreach (var taskRecord in tutorial.TaskRecords)
                {
                    taskListState.Add(new TutorialTaskDataState()
                    {
                        TaskRecord = taskRecord.Value,
                        TaskState  = TutorialState.NotStarted
                    });
                }

                var element = new TutorialElementDataState()
                {
                    TutorialRecord      = tutorial,
                    TaskIndex           = 0,
                    CurrentCompleteTask = 0,
                    TargetTaskToComplete = tutorial.TaskRecords
                        .Count(x => !x.Value.IsTaskOptional),
                    TaskDataStates = taskListState,
                    TutorialState  = TutorialState.NotStarted
                };

                this.tutorialDataState.TutorialElementsDataState.Add(element);
            }

            if (this.tutorialDataState.TutorialElementsDataState.Count == 0)
            {
                this.LogMessage($"All Tutorials are completed.", Color.green);

                return;
            }

            this.tutorialDataState.CurrentTutorial = this.tutorialDataState.TutorialElementsDataState[0];

            if (this.tutorialDataState.CurrentTutorial.TaskDataStates.Count == 0)
                return;

            this.StartTask(this.tutorialDataState.CurrentTutorial, 0);
        }

        private void StartTask(TutorialElementDataState taskDataState, int indexTask)
        {
            var currentTask = taskDataState.TaskDataStates[indexTask];

            this.LogMessage($"Start Task {currentTask.TaskRecord.TaskId} of Tutorial {taskDataState.TutorialRecord.Id}", Color.cyan);
            var currentRequirement = currentTask.TaskRecord.TaskRequirement.IsNullOrEmpty() ? TutorialStaticValue.NextTask : currentTask.TaskRecord.TaskRequirement;

            if (this.cacheTaskRequirements.TryGetValue(currentRequirement, out var taskRequirement))
            {
                taskRequirement.Execute(currentTask, currentTask.TaskRecord.TaskRequirementData).Forget();
            }
        }

        public void Tick() { }
    }
}
#endif