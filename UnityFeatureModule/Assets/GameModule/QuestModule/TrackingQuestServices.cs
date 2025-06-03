namespace GameModule.QuestModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FeatureTemplate.Scripts.Services;
    using GameModule.QuestModule.Blueprints;
    using GameModule.QuestModule.Model;
    using GameModule.QuestModule.Provider;
    using GameModule.QuestModule.Signals;
    using UnityEngine;
    using UserData;
    using Zenject;

    public class TrackingQuestServices : IInitializable, IDisposable
    {
        private readonly QuestManager          questManager;
        private readonly ISignalBus            signalBus;
        private readonly QuestProviderServices questProviderServices;

        public TrackingQuestServices(QuestManager questManager,
            ISignalBus signalBus, QuestProviderServices questProviderServices)
        {
            this.questManager          = questManager;
            this.signalBus             = signalBus;
            this.questProviderServices = questProviderServices;
        }

        private void CheckToAddTrackingCached(List<string> requirementIds, string requirementType, int addedValue)
        {
            foreach (var requirementId in requirementIds)
            {
                if (this.questManager.QuestJournal.TrackingCached.TryGetValue(requirementType, out var requirementTypeDict))
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
                        this.questManager.QuestJournal.TrackingCached.Add(requirementType, new Dictionary<string, int>());
                        this.questManager.QuestJournal.TrackingCached[requirementType].Add(requirementId, addedValue);
                    }
                    else
                    {
                        this.questManager.QuestJournal.TrackingCached.Add(requirementType, new Dictionary<string, int>());
                        this.questManager.QuestJournal.TrackingCached[requirementType].Add("", addedValue);
                    }
                }
            }
        }

        private void UpdateTaskProgress(List<string> requirementList, string requirementType, int addedValue)
        {
            this.CheckToAddTrackingCached(requirementList, requirementType, addedValue);

            var questCompleted = new List<QuestLog>();

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
                            if (string.IsNullOrEmpty(item.RequirementId) || requirementList.Contains(item.RequirementId))
                            {
                                item.CurrentValue += addedValue;
                            }
                        }

                        requirementProgress = listRequirementProgress.FirstOrDefault(x =>
                            x.RequirementType.Equals(requirementType) && x.RequirementId.Equals(r.RequirementId));

                        if (requirementProgress == null) continue;

                        var isCompleted = requirementProgress.CurrentValue >= requirementProgress.RequiredValue;
                        var isFailed    = requirementProgress.CurrentValue < 0;

                        if (r.TrackingType == nameof(TrackingType.Total))
                        {
                            if (!this.questManager.QuestJournal.TrackingCached.ContainsKey(requirementType))
                            {
                                this.questManager.QuestJournal.TrackingCached.Add(requirementType, new Dictionary<string, int>());
                                this.questManager.QuestJournal.TrackingCached[requirementType].Add(r.RequirementId, requirementProgress.CurrentValue);
                            }
                            else if (!this.questManager.QuestJournal.TrackingCached[requirementType].ContainsKey(r.RequirementId))
                            {
                                this.questManager.QuestJournal.TrackingCached[requirementType].Add(r.RequirementId, requirementProgress.CurrentValue);
                            }

                            var valueInTotal = this.questManager.QuestJournal.TrackingCached[requirementType][r.RequirementId];

                            isCompleted = valueInTotal >= requirementProgress.RequiredValue;
                        }

                        if (isFailed)
                        {
                            this.questManager.UpdateTaskStatus(questInfo.QuestId, questInfo.ProviderId,
                                taskLog.TaskRecord.TaskId, QuestStatus.Failed);

                            continue;
                        }

                        if (!isCompleted) continue;

                        if (r.RequirementOption)
                        {
                            this.questManager.UpdateCountRequirementOption(questInfo.QuestId, questInfo.ProviderId,
                                taskLog.TaskRecord.TaskId);
                        }

                        this.questManager.CheckTaskCompleted(questInfo.QuestId, questInfo.ProviderId,
                            taskLog.TaskRecord.TaskId);
                    }
                }

                //find NextTask notStarted
                var nextTask = questInfo.TaskProgress.FirstOrDefault(task => task.TaskStatus != QuestStatus.Completed && task.TaskStatus != QuestStatus.Rewarded);

                if (nextTask is { TaskStatus: QuestStatus.NotStarted })
                {
                    this.questManager.UpdateTaskStatus(questInfo.QuestId, questInfo.ProviderId,
                        nextTask.TaskRecord.TaskId, QuestStatus.InProgress);

                    this.questProviderServices.SetupTaskContext(nextTask, questInfo.QuestProviderType);
                }

                // Check if all tasks are completed
                var allTasksCompleted = this.questManager.CheckAllTaskCompleted(questInfo.QuestId, questInfo.ProviderId);

                if (!allTasksCompleted) continue;
                // Set the quest status to Completed
                this.questManager.SetQuestStatus(questInfo.QuestId, questInfo.ProviderId, QuestStatus.Completed);
                questCompleted.Add(questInfo);
                this.LogMessage("Done Quest " + questInfo.QuestId, Color.red);
            }

            foreach (var questInfo in questCompleted)
            {
                this.signalBus.Fire(new QuestChangeStatusSignal(questInfo));
            }
        }

        public void Initialize() { this.signalBus.Subscribe<TrackingQuestSignal>(this.OnTrackingQuest); }

        private void OnTrackingQuest(TrackingQuestSignal obj) { this.UpdateTaskProgress(obj.RequirementIds, obj.RequirementType, obj.RequirementValue); }

        public void Dispose() { this.signalBus.Unsubscribe<TrackingQuestSignal>(this.OnTrackingQuest); }
    }
}