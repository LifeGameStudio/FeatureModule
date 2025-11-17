namespace GameModule.QuestModule
{
    using System;
    using System.Collections.Generic;
    using FeatureTemplate.Scripts.Services;
    using GameModule.QuestModule.Blueprints.Base;
    using GameModule.QuestModule.Blueprints.Base.Interfaces;
    using GameModule.QuestModule.Model;
    using GameModule.QuestModule.Provider;
    using GameModule.QuestModule.QuestMatcher;
    using GameModule.QuestModule.Signals;
    using UnityEngine;
    using UnityEngine.Scripting;
    using Zenject;
    using ListPool = UnityEngine.Pool;

    public class TrackingQuestServices : IInitializable, IDisposable, ITickable
    {
        #region Nested types

        /// <summary>
        /// Trỏ tới 1 task trong 1 quest – dùng để cache theo requirementType.
        /// </summary>
        private struct TaskPointer
        {
            public QuestLog Quest;
            public TaskLog  Task;
        }

        #endregion

        private readonly QuestManager          questManager;
        private readonly ISignalBus            signalBus;
        private readonly QuestProviderServices questProviderServices;

        private readonly Dictionary<string, ITrackingQuestRequirementMatcher> cachedRequirementMatchers = new();

        // Cache task theo requirementType trong 1 lần flush
        private readonly Dictionary<string, List<TaskPointer>> taskIndexByRequirementType =
            new (StringComparer.Ordinal);

        private          SignalBatchQueue<TrackingQuestSignal> signalBatchQueue;
        private          float                                 lastFlushTime;
        private readonly float                                 flushInterval = 0.1f;

        [Preserve]
        public TrackingQuestServices(
            List<ITrackingQuestRequirementMatcher> questRequirementMatchers,
            QuestManager questManager,
            ISignalBus signalBus,
            QuestProviderServices questProviderServices)
        {
            this.questManager          = questManager;
            this.signalBus             = signalBus;
            this.questProviderServices = questProviderServices;

            foreach (var matcher in questRequirementMatchers)
            {
                this.cachedRequirementMatchers.Add(matcher.Id, matcher);
            }
        }

        #region Initialize / Dispose / Tick

        public void Initialize()
        {
            // Khi queue xử lý xong (empty lại sau khi có processing),
            // fire RefreshQuestViewSignal đúng 1 lần
            this.signalBatchQueue = new SignalBatchQueue<TrackingQuestSignal>(
                this.UpdateTaskProgress,
                () => this.signalBus.Fire(new RefreshQuestViewSignal()),
                maxPerFlush: 20,
                initialCapacity: 64);

            this.signalBus.Subscribe<TrackingQuestSignal>(this.OnTrackingQuest);
        }

        private void OnTrackingQuest(TrackingQuestSignal obj) { this.signalBatchQueue.Enqueue(obj); }

        public void Tick()
        {
            // flush mỗi flushInterval giây
            if (Time.realtimeSinceStartup - this.lastFlushTime < this.flushInterval)
            {
                return;
            }

            this.lastFlushTime = Time.realtimeSinceStartup;

            if (this.signalBatchQueue == null || this.signalBatchQueue.Count == 0)
            {
                return;
            }

            // Mỗi lần flush bắt đầu: clear task-index cũ (nếu có)
            this.ClearTaskIndex();

            // Flush batch các signal
            this.signalBatchQueue.Flush();
        }

        public void Dispose()
        {
            this.signalBus.Unsubscribe<TrackingQuestSignal>(this.OnTrackingQuest);
            this.ClearTaskIndex();
            this.signalBatchQueue?.Clear();
        }

        #endregion

        #region Task index by requirementType

        /// <summary>
        /// Xóa cache task index và release về ListPool.
        /// Được gọi mỗi lần bắt đầu flush mới hoặc khi Dispose.
        /// </summary>
        private void ClearTaskIndex()
        {
            foreach (var pair in this.taskIndexByRequirementType)
            {
                ListPool.ListPool<TaskPointer>.Release(pair.Value);
            }

            this.taskIndexByRequirementType.Clear();
        }

        /// <summary>
        /// Trả về danh sách TaskPointer có chứa requirementType tương ứng.
        /// Danh sách này được cache trong 1 lần flush.
        /// </summary>
        private List<TaskPointer> GetOrBuildTasksForRequirementType(string requirementType)
        {
            if (this.taskIndexByRequirementType.TryGetValue(requirementType, out var cachedList))
            {
                return cachedList;
            }

            var list = ListPool.ListPool<TaskPointer>.Get();
            list.Clear();

            var journalQuests = this.questManager.QuestJournal.Quests;

            foreach (var kvp in journalQuests)
            {
                var quest = kvp.Value;

                if (quest.QuestStatus != QuestStatus.InProgress)
                {
                    continue;
                }

                var tasks = quest.TaskProgress;

                if (tasks == null || tasks.Count == 0)
                {
                    continue;
                }

                for (var i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];

                    if (task.TaskStatus != QuestStatus.InProgress)
                    {
                        continue;
                    }

                    var requirements = task.TaskRecord.RequirementRecords();

                    if (requirements == null || requirements.Count == 0)
                    {
                        continue;
                    }

                    for (var j = 0; j < requirements.Count; j++)
                    {
                        var req = requirements[j];

                        if (req.GetRequirementType() == requirementType)
                        {
                            // Mỗi task chỉ cần add 1 lần theo requirementType
                            list.Add(new TaskPointer
                            {
                                Quest = quest,
                                Task  = task
                            });

                            break;
                        }
                    }
                }
            }

            this.taskIndexByRequirementType[requirementType] = list;

            return list;
        }

        #endregion

        #region Main tracking logic

        private void CheckToAddTrackingCached(List<string> requirementIds, string requirementType, int addedValue)
        {
            var trackingCached = this.questManager.QuestJournal.TrackingCached;

            foreach (var requirementId in requirementIds)
            {
                if (!trackingCached.TryGetValue(requirementType, out var requirementTypeDict))
                {
                    requirementTypeDict             = new Dictionary<string, int>();
                    trackingCached[requirementType] = requirementTypeDict;
                }

                // tăng theo id cụ thể (nếu có)
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
                }

                // tăng cho key "" (tổng)
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

        private void UpdateTaskProgress(TrackingQuestSignal signal)
        {
            var requirementType = signal.RequirementType;
            var requirementIds  = signal.RequirementIds;
            var addedValue      = signal.RequirementValue;

            // update cache tổng tracking
            this.CheckToAddTrackingCached(requirementIds, requirementType, addedValue);

            var questCompleted = ListPool.ListPool<QuestLog>.Get();
            questCompleted.Clear();

            // Lấy danh sách task có requirementType tương ứng
            var relatedTasks = this.GetOrBuildTasksForRequirementType(requirementType);

            if (relatedTasks.Count == 0)
            {
                ListPool.ListPool<QuestLog>.Release(questCompleted);

                return;
            }

            for (var i = 0; i < relatedTasks.Count; i++)
            {
                var pointer = relatedTasks[i];
                var quest   = pointer.Quest;
                var task    = pointer.Task;

                // Có thể trạng thái đã thay đổi trong cùng 1 batch
                if (quest.QuestStatus != QuestStatus.InProgress ||
                    task.TaskStatus != QuestStatus.InProgress)
                {
                    continue;
                }

                this.ProcessSingleTask(signal, quest, task);

                if (this.questManager.CheckAllTaskCompleted(quest.QuestId, quest.ProviderId))
                {
                    this.questManager.SetQuestStatus(quest.QuestId, quest.ProviderId, QuestStatus.Completed);
                    questCompleted.Add(quest);
                   this.LogMessage($"[Quest] Done Quest {quest.QuestId}");
                }
            }

            // Fire signal đổi trạng thái các quest đã hoàn thành
            for (var i = 0; i < questCompleted.Count; i++)
            {
                this.signalBus.Fire(new QuestChangeStatusSignal(questCompleted[i]));
            }

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

                if (req.GetRequirementType() != requirementType || !this.IsRequirementMatch(req, signal))
                {
                    continue;
                }

                var progressList = ListPool.ListPool<RequirementProgress>.Get();
                progressList.Clear();

                this.CollectOrCreateProgress(taskLog, req, requirementType, addedValue, progressList);

                for (var j = 0; j < progressList.Count; j++)
                {
                    var prog = progressList[j];

                    if (string.IsNullOrEmpty(prog.RequirementId))
                    {
                        // tổng
                        prog.CurrentValue += addedValue;
                    }
                    else
                    {
                        // chỉ cộng cho id có mặt trong signal
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
                    this.questManager.UpdateTaskStatus(quest.QuestId, quest.ProviderId,
                        taskLog.TaskRecord.TaskId, QuestStatus.Failed);

                    ListPool.ListPool<RequirementProgress>.Release(progressList);

                    return;
                }

                if (isCompleted)
                {
                    if (req.RequirementOption)
                    {
                        this.questManager.UpdateCountRequirementOption(quest.QuestId, quest.ProviderId,
                            taskLog.TaskRecord.TaskId);
                    }

                    this.questManager.CheckTaskCompleted(quest.QuestId, quest.ProviderId,
                        taskLog.TaskRecord.TaskId);
                }

                ListPool.ListPool<RequirementProgress>.Release(progressList);
            }

            // Tìm next task để auto chuyển sang InProgress
            TaskLog nextTask = null;
            var     tasks    = quest.TaskProgress;

            for (var i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];

                if (t.TaskStatus != QuestStatus.Completed && t.TaskStatus != QuestStatus.Rewarded)
                {
                    nextTask = t;

                    break;
                }
            }

            if (nextTask != null && nextTask.TaskStatus == QuestStatus.NotStarted)
            {
                this.questManager.UpdateTaskStatus(quest.QuestId, quest.ProviderId,
                    nextTask.TaskRecord.TaskId, QuestStatus.InProgress);

                this.questProviderServices.SetupTaskContext(nextTask, quest.QuestProviderType);
            }
        }

        private void CollectOrCreateProgress(
            TaskLog taskLog,
            IQuestRequirement requirement,
            string type,
            int addedValue,
            List<RequirementProgress> outList)
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
                return this.cachedRequirementMatchers.TryGetValue(
                           questRequirementWithCondition.RequirementConditionId,
                           out var matcher) &&
                       matcher.IsMatch(questRequirementWithCondition, obj);
            }

            return true;
        }

        #endregion
    }
}