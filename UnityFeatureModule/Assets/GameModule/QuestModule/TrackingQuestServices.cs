namespace GameModule.QuestModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameModule.QuestModule.Model;
    using GameModule.QuestModule.Signals;
    using global::Blueprints;
    using global::QuestModule.Provider;
    using UnityEngine;
    using Zenject;

    public class TrackingQuestServices : IInitializable, IDisposable
    {
        private readonly QuestManager          questManager;
        private readonly ISignalBus             signalBus;
        private readonly QuestProviderServices questProviderServices;

        public TrackingQuestServices(QuestManager questManager,
            ISignalBus signalBus, QuestProviderServices questProviderServices)
        {
            this.questManager          = questManager;
            this.signalBus             = signalBus;
            this.questProviderServices = questProviderServices;
        }

        private void CheckToAddTrackingCached(string requirementId, string requirementType, int addedValue)
        {
            if (this.questManager.TrackingCached.TryGetValue(requirementType, out var requirementTypeDict))
            {
                if (!string.IsNullOrEmpty(requirementId))
                {
                    if (requirementTypeDict.TryGetValue(requirementId, out var currentValue))
                    {
                        requirementTypeDict[requirementId] = currentValue + addedValue;
                    }
                    else
                    {
                        requirementTypeDict.Add(requirementId, addedValue);
                    }

                    //add for null
                    if (requirementTypeDict.TryGetValue("", out var valueInTotal))
                    {
                        requirementTypeDict[""] = valueInTotal + addedValue;
                    }
                    else
                    {
                        requirementTypeDict.Add("", addedValue);
                    }
                }
                else
                {
                    if (requirementTypeDict.TryGetValue("", out var valueInTotal))
                    {
                        requirementTypeDict[""] = valueInTotal + addedValue;
                    }
                    else
                    {
                        requirementTypeDict.Add("", addedValue);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(requirementId))
                {
                    this.questManager.TrackingCached.Add(requirementType, new Dictionary<string, int>());
                    this.questManager.TrackingCached[requirementType].Add(requirementId, addedValue);
                }
                else
                {
                    this.questManager.TrackingCached.Add(requirementType, new Dictionary<string, int>());
                    this.questManager.TrackingCached[requirementType].Add("", addedValue);
                }
            }
        }

        private void UpdateTaskProgress(string requirementId, string requirementType, int addedValue)
        {
            this.CheckToAddTrackingCached(requirementId, requirementType, addedValue);

            var listQuestCompleted = new List<QuestLog>();

            foreach (var (id, questInfo) in this.questManager.QuestJournal.Quests)
            {
                if (questInfo.QuestStatus != QuestStatus.InProgress) continue;

                foreach (var taskLog in questInfo.TaskProgress)
                {
                    if (taskLog.TaskStatus != QuestStatus.InProgress) continue;

                    var requirementsRecords =
                        taskLog.TaskRecord.RequirementRecords.FindAll(r => r.RequirementType.Equals(requirementType));

                    if (!string.IsNullOrEmpty(requirementType))
                    {
                        requirementsRecords =
                            requirementsRecords.FindAll(r => r.RequirementType.Equals(requirementType));
                    }

                    if (requirementsRecords.Count == 0)
                    {
                        continue;
                    }

                    foreach (var r in requirementsRecords)
                    {
                        var listRequirementProgress = new List<RequirementProgress>();

                        var requirementProgress = taskLog.Progress.FirstOrDefault(x =>
                            x.RequirementType.Equals(requirementType) && string.IsNullOrEmpty(x.RequirementId));

                        if (requirementProgress == null)
                        {
                            requirementProgress = new RequirementProgress()
                            {
                                RequirementType = requirementType,
                                RequirementId   = "",
                                CurrentValue    = addedValue,
                                RequiredValue   = r.RequirementValue,
                                IsOptional      = r.RequirementOption
                            };

                            listRequirementProgress.Add(requirementProgress);
                            taskLog.Progress.Add(requirementProgress);
                        }
                        else listRequirementProgress.Add(requirementProgress);

                        if (!string.IsNullOrEmpty(r.RequirementId))
                        {
                            requirementProgress = taskLog.Progress.FirstOrDefault(x =>
                                x.RequirementType.Equals(requirementType) && !string.IsNullOrEmpty(x.RequirementId));

                            if (requirementProgress == null)
                            {
                                listRequirementProgress.Add(new RequirementProgress()
                                {
                                    RequirementType = requirementType,
                                    RequirementId   = r.RequirementId,
                                    CurrentValue    = addedValue,
                                    RequiredValue   = r.RequirementValue,
                                    IsOptional      = r.RequirementOption
                                });

                                taskLog.Progress.Add(requirementProgress);
                            }
                            else listRequirementProgress.Add(requirementProgress);
                        }

                        foreach (var item in listRequirementProgress)
                        {
                            if (string.IsNullOrEmpty(item.RequirementId))
                            {
                                item.CurrentValue += addedValue;
                            }
                            else
                            {
                                if (item.RequirementId.Equals(requirementId))
                                {
                                    item.CurrentValue += addedValue;
                                }
                            }
                        }

                        requirementProgress = listRequirementProgress.FirstOrDefault(x =>
                            x.RequirementType.Equals(requirementType) && x.RequirementId.Equals(r.RequirementId));

                        if (requirementProgress == null) continue;

                        var isCompleted = requirementProgress.CurrentValue >= requirementProgress.RequiredValue;
                        var isFailed    = requirementProgress.CurrentValue < 0;

                        if (r.TrackingType == TrackingType.Total.ToString())
                        {
                            if (!this.questManager.TrackingCached.ContainsKey(requirementType))
                            {
                                this.questManager.TrackingCached.Add(requirementType, new Dictionary<string, int>());
                                this.questManager.TrackingCached[requirementType].Add(r.RequirementId, requirementProgress.CurrentValue);
                            }
                            else if (!this.questManager.TrackingCached[requirementType].ContainsKey(r.RequirementId))
                            {
                                this.questManager.TrackingCached[requirementType].Add(r.RequirementId, requirementProgress.CurrentValue);
                            }

                            var valueInTotal = this.questManager.TrackingCached[requirementType][r.RequirementId];

                            isCompleted = valueInTotal >= requirementProgress.RequiredValue;
                        }

                        if (isFailed)
                        {
                            this.questManager.UpdateTaskStatus(questInfo.ProviderId, questInfo.QuestId,
                                taskLog.TaskRecord.TaskId, QuestStatus.Failed);

                            continue;
                        }

                        if (!isCompleted) continue;

                        if (r.RequirementOption)
                        {
                            this.questManager.UpdateCountRequirementOption(questInfo.ProviderId, questInfo.QuestId,
                                taskLog.TaskRecord.TaskId);
                        }

                        this.questManager.CheckTaskCompleted(questInfo.ProviderId, questInfo.QuestId,
                            taskLog.TaskRecord.TaskId);
                    }
                }

                //find NextTask notStarted
                var nextTask = questInfo.TaskProgress.FirstOrDefault(task => task.TaskStatus == QuestStatus.NotStarted);

                if (nextTask != null)
                {
                    this.questManager.UpdateTaskStatus(questInfo.ProviderId, questInfo.QuestId,
                        nextTask.TaskRecord.TaskId, QuestStatus.InProgress);
                }

                // Check if all tasks are completed
                var allTasksCompleted = this.questManager.CheckAllTaskCompleted(questInfo.ProviderId, questInfo.QuestId);

                if (!allTasksCompleted) continue;
                // Set the quest status to Completed
                this.questManager.SetQuestStatus(questInfo.ProviderId, questInfo.QuestId, QuestStatus.Completed);
                listQuestCompleted.Add(questInfo);

                Debug.Log("Done Quest " + questInfo.QuestId);
            }

            foreach (var questInfo in listQuestCompleted)
            {
                this.signalBus.Fire(new QuestDoneSignal(questInfo.QuestId, questInfo.ProviderId, questInfo.QuestProviderType));
            }
        }

        public void Initialize() { this.signalBus.Subscribe<TrackingQuestSignal>(this.OnTrackingQuest); }

        private async void OnFakeQuest()
        {
            await UniTask.WaitUntil(() => this.questManager.QuestJournal != null);
            Debug.Log($"Fake quest Here");

            var mainQuest = this.questManager.QuestJournal.Quests.FirstOrDefault(x =>
                x.Value.QuestProviderType == QuestProviderType.Main && x.Value.QuestId.Equals("quest_1")).Value;

            if (mainQuest == null)
            {
                this.questProviderServices.GiveQuestToUser("quest_1", "", QuestProviderType.Main);
                this.questProviderServices.StartQuest(QuestProviderType.Main, "quest_1", "");
            }
        }

        private void OnTrackingQuest(TrackingQuestSignal obj) { this.UpdateTaskProgress(obj.RequirementId, obj.RequirementType, obj.RequirementValue); }

        public void Dispose() { this.signalBus.Unsubscribe<TrackingQuestSignal>(this.OnTrackingQuest); }
    }
}