namespace UserData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.InterfacesAndEnumCommon;
    using FeatureTemplate.Scripts.RewardHandle;
    using GameModule.QuestModule.Blueprints;
    using GameModule.QuestModule.Model;
    using GameModule.QuestModule.Provider;
    using GameModule.QuestModule.Signals;
    using Zenject;

    public class QuestManager : IFeatureControllerData
    {
        private readonly QuestJournal Data;
        private readonly ISignalBus   signalBus;
        public           QuestJournal QuestJournal => this.Data;

        public QuestManager(QuestJournal questJournal, ISignalBus signalBus)
        {
            this.Data      = questJournal;
            this.signalBus = signalBus;
        }

        public async UniTask LoadRecord(IQuestProvider questProvider)
        {
            await UniTask.WaitUntil((() => this.Data != null));

            foreach (var q in this.Data.Quests.Where(x => x.Value.QuestProviderType == questProvider.QuestProviderType))
            {
                var questRecord = questProvider.GetQuestRecord(q.Value.QuestId, q.Value.ProviderId);

                for (var index = 0; index < q.Value.TaskProgress.Count; index++)
                {
                    var taskLog = q.Value.TaskProgress[index];
                    taskLog.TaskRecord = questRecord.Tasks[index];
                }

                q.Value.QuestRecord = questRecord;
            }
        }

        public void ClearSideQuest()
        {
            var keyToRemove = new List<string>();

            foreach (var q in this.Data.Quests)
            {
                if (q.Value.QuestProviderType == QuestProviderType.Side)
                {
                    keyToRemove.Add(q.Key);
                }
            }

            foreach (var key in keyToRemove)
            {
                this.Data.Quests.Remove(key);
            }
        }

        public bool CheckQuestAccepted(string questId, string providerId)
        {
            return this.Data.Quests.Where(x => x.Value.QuestId.Equals(questId)
                                               && x.Value.ProviderId.Equals(providerId)).ToList().Count > 0;
        }

        public bool CheckCompleteNotReceivingRewardQuest(string questId, string providerId)
        {
            return this.Data.Quests.Where(x
                => x.Value.QuestId.Equals(questId)
                   && x.Value.ProviderId.Equals(providerId)
                   && x.Value.QuestStatus == QuestStatus.Completed).ToList().Count > 0;
        }

        public bool CheckQuestDone(string questId, string providerId)
        {
            return this.Data.Quests.Where(x
                => x.Value.QuestId.Equals(questId)
                   && x.Value.ProviderId.Equals(providerId)
                   && x.Value.QuestStatus == QuestStatus.Rewarded).ToList().Count > 0;
        }

        public QuestLog GetQuest(string questId, string provideId) { return this.Data.Quests.FirstOrDefault(q => q.Key.Equals(questId) && q.Value.ProviderId.Equals(provideId)).Value; }

        public QuestLog CheckToAddNewQuest(string questId, string provideId, QuestProviderType questProviderType, QuestRecord questRecord)
        {
            if (!this.Data.Quests.TryGetValue(questId, out var questInfo))
            {
                var listTaskProgress = new List<TaskLog>();

                foreach (var taskRecord in questRecord.Tasks)
                {
                    var requirementProgress = new List<RequirementProgress>();

                    foreach (var requirementRecord in taskRecord.RequirementRecords)
                    {
                        requirementProgress.Add(new RequirementProgress()
                        {
                            RequirementId   = requirementRecord.RequirementId,
                            RequirementType = requirementRecord.RequirementType,
                            CurrentValue    = 0,
                            RequiredValue   = requirementRecord.RequirementValue,
                            IsOptional      = requirementRecord.RequirementOption
                        });
                    }

                    listTaskProgress.Add(new TaskLog()
                    {
                        TaskRecord = taskRecord,
                        TaskStatus = QuestStatus.NotStarted,
                        Progress   = requirementProgress
                    });
                }

                listTaskProgress[0].TaskStatus = QuestStatus.InProgress;

                questInfo = new QuestLog()
                {
                    QuestId           = questId,
                    QuestStatus       = QuestStatus.NotStarted,
                    ProviderId        = provideId,
                    TaskProgress      = listTaskProgress,
                    QuestRecord       = questRecord,
                    QuestProviderType = questProviderType,
                    QuestType         = questRecord.QuestType,
                };
            }

            this.Data.Quests[questId] = questInfo;

            return questInfo;
        }

        /// <summary>
        /// Get first task progress that has not been completed
        /// </summary>
        /// <param name="questId"></param>
        /// <param name="taskId"></param>
        /// <param name="provideId"></param>
        /// <returns></returns>
        public TaskLog GetCurrentTaskProgress(string questId, string provideId)
        {
            var questInfo = this.GetQuest(questId, provideId);
            var taskId    = questInfo.TaskProgress.FirstOrDefault(task => task.Progress.All(requirement => requirement.CurrentValue < requirement.RequiredValue))?.TaskRecord.TaskId;

            return questInfo.TaskProgress.FirstOrDefault(task => task.TaskRecord.TaskId.Equals(taskId));
        }

        public void UpdateQuestStatus(string provideId, string questId, QuestStatus questStatus)
        {
            var questInfo = this.GetQuest(questId, provideId);

            if (questInfo == null)
            {
                throw new Exception($"Quest {questId} not found");
            }

            questInfo.QuestStatus = questStatus;

            this.signalBus.Fire(new QuestChangeStatusSignal(questInfo));
        }

        public void UpdateTaskStatus(string questId, string providerId, string taskRecordTaskId, QuestStatus questStatus)
        {
            var taskLog = this.GetQuest(questId, providerId).TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskRecordTaskId));
            taskLog.TaskStatus = questStatus;
        }

        public void GiveRewardToCurrentTaskQuest(string questInfoProviderId, string questInfoQuestId)
        {
            var quest   = this.GetQuest(questInfoQuestId, questInfoProviderId);
            var taskLog = quest.TaskProgress.FirstOrDefault(t => t.TaskStatus == QuestStatus.Completed);

            if (taskLog == null) return;
            taskLog.TaskStatus = QuestStatus.Rewarded;

            var listAsset = taskLog.TaskRecord.RewardRecords.Select(x => new RewardRecord()
            {
                RewardId    = x.TaskRewardId,
                RewardValue = x.TaskRewardValue,
                RewardType  = x.TaskRewardType
            });

            if (quest.TaskProgress.IndexOf(taskLog) == quest.TaskProgress.Count - 1) quest.QuestStatus = QuestStatus.Completed;
            this.Payout(listAsset.ToList(), quest.QuestProviderType == QuestProviderType.Side);
        }

        /// <summary>
        /// GetAll task reward from quest and set task status to rewarded
        /// </summary>
        /// <param name="questLogProviderId"></param>
        /// <param name="questId"></param>
        /// <returns></returns>
        public List<RewardRecord> GetTaskReward(string questLogProviderId, string questId)
        {
            var quest     = this.GetQuest(questId, questLogProviderId);
            var taskLogs  = quest.TaskProgress.Where(t => t.TaskStatus == QuestStatus.Completed).ToList();
            var listAsset = new List<RewardRecord>();

            foreach (var taskLog in taskLogs)
            {
                listAsset.AddRange(taskLog.TaskRecord.RewardRecords.Select(x => new RewardRecord()
                {
                    RewardId    = x.TaskRewardId,
                    RewardValue = x.TaskRewardValue,
                    RewardType  = x.TaskRewardType
                }));
            }

            return listAsset;
        }

        public List<RewardRecord> GetTaskRewardWithTaskId(string questId, string questLogProviderId, string taskId)
        {
            var quest     = this.GetQuest(questId, questLogProviderId);
            var taskLog   = quest.TaskProgress.FirstOrDefault(t => t.TaskRecord.TaskId.Equals(taskId));
            var listAsset = new List<RewardRecord>();

            if (taskLog == null) return listAsset;

            listAsset.AddRange(taskLog.TaskRecord.RewardRecords.Select(x => new RewardRecord()
            {
                RewardId    = x.TaskRewardId,
                RewardValue = x.TaskRewardValue,
                RewardType  = x.TaskRewardType
            }));

            return listAsset;
        }

        /// <summary>
        /// GetAll reward from quest and set quest status to rewarded
        /// </summary>
        /// <param name="questLogProviderId"></param>
        /// <param name="questLogQuestId"></param>
        /// <returns></returns>
        public List<RewardRecord> GetQuestReward(string questLogProviderId, string questLogQuestId)
        {
            var questInfo = this.GetQuest(questLogQuestId, questLogProviderId);

            var result = new List<RewardRecord>();

            if (questInfo.QuestStatus != QuestStatus.Completed) return result;

            result.AddRange(questInfo.QuestRecord.QuestRewardRecords.Select(x => new RewardRecord()
            {
                RewardId    = x.QuestRewardId,
                RewardValue = x.QuestRewardValue,
                RewardType  = x.QuestRewardType
            }));

            return result;
        }

        private async void Payout(List<RewardRecord> assets, bool isSideQuest = false) { }

        public void CheckToCompleteQuest(string questId, string providerId)
        {
            var questInfo = this.GetQuest(questId, providerId);

            //if (questInfo.TaskProgress.Any(t => t.TaskStatus != QuestStatus.Completed)) return;
            if (questInfo.QuestStatus != QuestStatus.Completed) return;
            questInfo.QuestStatus = QuestStatus.Rewarded;

            var listAsset = questInfo.QuestRecord.QuestRewardRecords.Select(x => new RewardRecord()
            {
                RewardId    = x.QuestRewardId,
                RewardValue = x.QuestRewardValue,
                RewardType  = x.QuestRewardType
            });

            this.Payout(listAsset.ToList(), questInfo.QuestProviderType == QuestProviderType.Side);
            this.signalBus.Fire<RefreshQuestViewSignal>();

            if (questInfo.QuestProviderType == QuestProviderType.Side)
            {
                this.signalBus.Fire(new TrackingQuestSignal("complete_side_quest", new List<string>()
                {
                    questInfo.QuestId
                }, 1));
            }
        }

        public List<QuestLog> GetAllQuestsType(QuestProviderType questProviderType)
        {
            return this.Data.Quests.Where(x => x.Value.QuestProviderType == questProviderType).Select(x => x.Value).ToList();
        }

        public List<string> GetAllQuestsCategoryOfAQuestProvider(QuestProviderType questProviderId)
        {
            var quests = this.GetAllQuestsType(questProviderId);

            return quests.Select(q => q.QuestType).Distinct().ToList();
        }

        public List<QuestLog> GetAllQuests(QuestProviderType questProviderType, string questCategory)
        {
            var quests = this.GetAllQuestsType(questProviderType);

            return quests.Where(q => q.QuestType.Equals(questCategory)).ToList();
        }

        public QuestLog GetCurrentMainQuestNotFinish()
        {
            var totalMainQuests = this.GetAllQuestsType(QuestProviderType.Main).OrderBy(x => x.QuestRecord.QuestIndex).ToList();

            foreach (var item in totalMainQuests.Where(x => x.QuestProviderType == QuestProviderType.Main))
            {
                foreach (var taskLog in item.TaskProgress)
                {
                    if (taskLog.TaskStatus is QuestStatus.InProgress or QuestStatus.NotStarted or QuestStatus.Completed)
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        public void SetQuestStatus(string questId, string questInfoProviderId, QuestStatus status)
        {
            var questInfo = this.GetQuest(questId, questInfoProviderId);
            questInfo.QuestStatus = status;
        }

        public void RemoveQuest(string questId, string providerId)
        {
            var quest = this.GetQuest(questId, providerId);

            if (quest == null) return;
            this.Data.Quests.Remove(quest.QuestId);
        }

        public void ResetQuest(string questId, string providerId)
        {
            var quest = this.GetQuest(questId, providerId);
            quest.TaskProgress.ForEach(x => x.Progress.ForEach(y => y.CurrentValue = 0));
            quest.TaskProgress.ForEach(x => x.TaskStatus = QuestStatus.NotStarted);
            quest.QuestStatus = QuestStatus.NotStarted;
        }

        public void UpdateCountRequirementOption(string questId, string providerId, string taskRecordTaskId)
        {
            var taskLog = this.GetQuest(questId, providerId).TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskRecordTaskId));
            taskLog.CountRequirementOption++;
        }

        public void CheckTaskCompleted(string questId, string providerId, string taskRecordTaskId)
        {
            var questInfo = this.GetQuest(questId, providerId);
            var taskLog   = questInfo.TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskRecordTaskId));

            var allRequirementPremiseCompleted = taskLog.Progress.All(x => x.CurrentValue >= x.RequiredValue && !x.IsOptional);

            if (!allRequirementPremiseCompleted && taskLog.Progress.All(x => x.IsOptional))
            {
                allRequirementPremiseCompleted = true;
            }

            if (taskLog.CountRequirementOption < taskLog.TaskRecord.CoutRequirementOption || !allRequirementPremiseCompleted) return;
            this.UpdateTaskStatus(questId, providerId, taskLog.TaskRecord.TaskId, QuestStatus.Completed);
            this.CountingTaskOption(questId, providerId, taskRecordTaskId);
        }

        public void CountingTaskOption(string questId, string providerId, string taskId)
        {
            var quest   = this.GetQuest(questId, providerId);
            var taskLog = this.GetQuest(questId, providerId).TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskId));

            if (taskLog.TaskRecord.TaskOption)
            {
                quest.CountTaskOption++;
            }
        }

        public bool CheckAllTaskCompleted(string questId, string providerId)
        {
            var questInfo              = this.GetQuest(questId, providerId);
            var allPremiseTaskComplete = questInfo.TaskProgress.All(task => task.TaskStatus == QuestStatus.Completed && !task.TaskRecord.TaskOption);

            var checkCountTaskOption = questInfo.CountTaskOption >= questInfo.QuestRecord.CountTaskOption;

            if (!allPremiseTaskComplete && questInfo.QuestRecord.Tasks.Count(x => !x.TaskOption) == 0)
            {
                questInfo.QuestStatus = QuestStatus.Completed;
            }

            return allPremiseTaskComplete && checkCountTaskOption;
        }

        public TaskLog GetTaskLogOfQuest(string questId, string providerId, string taskId)
        {
            return this.GetQuest(questId, providerId).TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskId));
        }

        public List<QuestLog> GetAllQuestCompleted() { return this.Data.Quests.Where(x => x.Value.QuestStatus == QuestStatus.Completed).Select(y => y.Value).ToList(); }
    }
}

public class CheckFTUEQuestSignal
{
    public string Id;
}