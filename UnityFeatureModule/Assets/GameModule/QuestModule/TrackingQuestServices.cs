namespace GameModule.QuestModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GameModule.QuestModule.Blueprints.Base;
    using GameModule.QuestModule.Blueprints.Base.Interfaces;
    using GameModule.QuestModule.Model;
    using GameModule.QuestModule.Provider;
    using GameModule.QuestModule.QuestMatcher;
    using GameModule.QuestModule.Signals;
    using UnityEngine;
    using UnityEngine.Scripting;
    using UserData;
    using Zenject;
    using ListPool = UnityEngine.Pool;

    public class TrackingQuestServices : IInitializable, IDisposable, ITickable
    {
        private readonly QuestManager          questManager;
        private readonly ISignalBus            signalBus;
        private readonly QuestProviderServices questProviderServices;

        private readonly Dictionary<string, ITrackingQuestRequirementMatcher> cachedRequirementMatchers = new();

        private          SignalBatchQueue<TrackingQuestSignal> signalBatchQueue;
        private          float                                 lastFlushTime;
        private readonly float                                 flushInterval = 0.1f;

        [Preserve]
        public TrackingQuestServices(List<ITrackingQuestRequirementMatcher> questRequirementMatchers, QuestManager questManager,
            ISignalBus signalBus, QuestProviderServices questProviderServices)
        {
            this.questManager          = questManager;
            this.signalBus             = signalBus;
            this.questProviderServices = questProviderServices;

            foreach (var matcher in questRequirementMatchers)
            {
                this.cachedRequirementMatchers.Add(matcher.Id, matcher);
            }
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

        private void UpdateTaskProgress(TrackingQuestSignal signal)
        {
            var requirementType = signal.RequirementType;
            var requirementIds  = signal.RequirementIds;
            var addedValue      = signal.RequirementValue;

            this.CheckToAddTrackingCached(requirementIds, requirementType, addedValue);

            var questCompleted = ListPool.ListPool<QuestLog>.Get();
            var relatedTasks   = ListPool.ListPool<(QuestLog quest, TaskLog task)>.Get();

            // Step 1: Collect all tasks that match requirementType
            foreach (var (_, quest) in this.questManager.QuestJournal.Quests)
            {
                if (quest.QuestStatus != QuestStatus.InProgress) continue;

                for (var i = 0; i < quest.TaskProgress.Count; i++)
                {
                    var task = quest.TaskProgress[i];

                    if (task.TaskStatus != QuestStatus.InProgress) continue;

                    var requirements = task.TaskRecord.RequirementRecords();

                    for (var j = 0; j < requirements.Count; j++)
                    {
                        var req = requirements[j];

                        if (req.GetRequirementType() == requirementType && this.IsRequirementMatch(req, signal))
                        {
                            relatedTasks.Add((quest, task));
                        }
                    }
                }
            }

            for (var i = 0; i < relatedTasks.Count; i++)
            {
                var (quest, task) = relatedTasks[i];

                this.ProcessSingleTask(signal, quest, task);

                if (this.questManager.CheckAllTaskCompleted(quest.QuestId, quest.ProviderId))
                {
                    this.questManager.SetQuestStatus(quest.QuestId, quest.ProviderId, QuestStatus.Completed);
                    questCompleted.Add(quest);
                    Debug.Log($"Done Quest {quest.QuestId}");
                }
            }

            for (var i = 0; i < questCompleted.Count; i++)
            {
                this.signalBus.Fire(new QuestChangeStatusSignal(questCompleted[i]));
            }

            this.signalBus.Fire(new RefreshQuestViewSignal());
            ListPool.ListPool<(QuestLog, TaskLog)>.Release(relatedTasks);
            ListPool.ListPool<QuestLog>.Release(questCompleted);
        }

        private void ProcessSingleTask(TrackingQuestSignal signal, QuestLog quest, TaskLog taskLog)
        {
            var requirementType = signal.RequirementType;
            var requirementIds  = signal.RequirementIds;
            var addedValue      = signal.RequirementValue;

            var requirements = taskLog.TaskRecord.RequirementRecords();

            for (var i = 0; i < requirements.Count; i++)
            {
                var req = requirements[i];

                if (req.GetRequirementType() != requirementType || !this.IsRequirementMatch(req, signal)) continue;

                var progressList = ListPool.ListPool<RequirementProgress>.Get();
                this.CollectOrCreateProgress(taskLog, req, requirementType, addedValue, progressList);

                for (var j = 0; j < progressList.Count; j++)
                {
                    var prog = progressList[j];

                    if (string.IsNullOrEmpty(prog.RequirementId))
                    {
                        prog.CurrentValue += addedValue;
                    }
                    else if (!string.IsNullOrEmpty(prog.RequirementId))
                    {
                        if (requirementIds.Contains(prog.RequirementId))
                        {
                            prog.CurrentValue += addedValue;
                        }
                    }
                }

                var matchedProgress = this.FindProgress(progressList, requirementType, req.GetRequirementId());

                if (matchedProgress == null)
                {
                    ListPool.ListPool<RequirementProgress>.Release(progressList);

                    continue;
                }

                var isCompleted = matchedProgress.CurrentValue >= matchedProgress.RequiredValue;
                var isFailed    = matchedProgress.CurrentValue < 0;

                if (req.TrackingType == nameof(TrackingType.Total))
                {
                    var total = this.UpdateTrackingTotal(requirementType, req.GetRequirementId(), matchedProgress.CurrentValue);
                    isCompleted = total >= matchedProgress.RequiredValue;
                }

                if (isFailed)
                {
                    this.questManager.UpdateTaskStatus(quest.QuestId, quest.ProviderId, taskLog.TaskRecord.TaskId, QuestStatus.Failed);
                    ListPool.ListPool<RequirementProgress>.Release(progressList);

                    return;
                }

                if (isCompleted)
                {
                    if (req.RequirementOption)
                    {
                        this.questManager.UpdateCountRequirementOption(quest.QuestId, quest.ProviderId, taskLog.TaskRecord.TaskId);
                    }

                    this.questManager.CheckTaskCompleted(quest.QuestId, quest.ProviderId, taskLog.TaskRecord.TaskId);
                }

                ListPool.ListPool<RequirementProgress>.Release(progressList);
            }

            var nextTask = quest.TaskProgress.FirstOrDefault(task => task.TaskStatus != QuestStatus.Completed && task.TaskStatus != QuestStatus.Rewarded);

            if (nextTask is { TaskStatus: QuestStatus.NotStarted })
            {
                this.questManager.UpdateTaskStatus(quest.QuestId, quest.ProviderId,
                    nextTask.TaskRecord.TaskId, QuestStatus.InProgress);

                this.questProviderServices.SetupTaskContext(nextTask, quest.QuestProviderType);
            }
        }

        private void CollectOrCreateProgress(TaskLog taskLog, IQuestRequirement requirement, string type, int addedValue, List<RequirementProgress> outList)
        {
            var reqId = requirement.GetRequirementId();

            var baseProgress = this.FindProgress(taskLog.Progress, type, "");

            if (baseProgress == null)
            {
                baseProgress = new RequirementProgress
                {
                    RequirementType = type,
                    RequirementId   = "",
                    CurrentValue    = 0,
                    RequiredValue   = requirement.GetRequirementValue(),
                    IsOptional      = requirement.RequirementOption
                };

                taskLog.Progress.Add(baseProgress);
            }

            outList.Add(baseProgress);

            if (!string.IsNullOrEmpty(reqId))
            {
                var specificProgress = this.FindProgress(taskLog.Progress, type, reqId);

                if (specificProgress == null)
                {
                    specificProgress = new RequirementProgress
                    {
                        RequirementType = type,
                        RequirementId   = reqId,
                        CurrentValue    = 0,
                        RequiredValue   = requirement.GetRequirementValue(),
                        IsOptional      = requirement.RequirementOption
                    };

                    taskLog.Progress.Add(specificProgress);
                }

                outList.Add(specificProgress);
            }
        }

        private RequirementProgress FindProgress(List<RequirementProgress> list, string type, string id)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var progress = list[i];

                if (progress.RequirementType == type && progress.RequirementId == id)
                {
                    return progress;
                }
            }

            return null;
        }

        private int UpdateTrackingTotal(string type, string id, int value)
        {
            if (!this.questManager.QuestJournal.TrackingCached.TryGetValue(type, out var dict))
            {
                dict                                                = new Dictionary<string, int>();
                this.questManager.QuestJournal.TrackingCached[type] = dict;
            }

            if (!dict.TryAdd(id, value))
            {
                dict[id] += value;
            }

            return dict[id];
        }

        private bool IsRequirementMatch(IQuestRequirement questRequirement, TrackingQuestSignal obj)
        {
            if (questRequirement is IQuestRequirementWithCondition questRequirementWithCondition)
            {
                return this.cachedRequirementMatchers.TryGetValue(questRequirementWithCondition.RequirementConditionId, out var matcher) &&
                       matcher.IsMatch(questRequirementWithCondition, obj);
            }

            return true;
        }

        public void Initialize()
        {
            this.signalBatchQueue = new SignalBatchQueue<TrackingQuestSignal>(this.UpdateTaskProgress, 20);

            this.signalBus.Subscribe<TrackingQuestSignal>(this.OnTrackingQuest);
        }

        private void OnTrackingQuest(TrackingQuestSignal obj) { this.signalBatchQueue.Enqueue(obj); }

        public void Tick()
        {
            if (!(Time.realtimeSinceStartup - this.lastFlushTime >= this.flushInterval)) return;
            this.signalBatchQueue.Flush();
            this.lastFlushTime = Time.realtimeSinceStartup;
        }

        public void Dispose() { this.signalBus.Unsubscribe<TrackingQuestSignal>(this.OnTrackingQuest); }
    }
}