namespace GameModule.QuestModule.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Models.Controllers;
    using FeatureTemplate.Scripts.RewardHandle;
    using FeatureTemplate.Scripts.Services;
    using GameModule.QuestModule.Blueprints.Base;
    using GameModule.QuestModule.Blueprints.Base.Interfaces;
    using GameModule.QuestModule.Provider;
    using GameModule.QuestModule.Signals;
    using UnityEngine;
    using UnityEngine.Scripting;
    using Zenject;
    using RewardRecord = FeatureTemplate.Scripts.RewardHandle.RewardRecord;

    public class QuestManager : BaseDataController<QuestJournal>
    {
        private readonly FeatureRewardHandler featureRewardHandler;
        private readonly ISignalBus           signalBus;

        [Preserve]
        public QuestManager(QuestJournal data, FeatureRewardHandler featureRewardHandler, ISignalBus signalBus) : base(data)
        {
            this.Data                 = data;
            this.featureRewardHandler = featureRewardHandler;
            this.signalBus            = signalBus;
        }

        public QuestJournal QuestJournal => this.Data;

        public async UniTask LoadRecord(IQuestProvider questProvider)
        {
            await UniTask.WaitUntil(() => this.Data != null);
            var quests = this.GetAllQuestsType(questProvider.QuestProviderType, true);

            foreach (var q in quests)
            {
                var questRecord = questProvider.GetQuestRecord(q.QuestId, q.ProviderId);

                for (var index = 0; index < q.TaskProgress.Count; index++)
                {
                    var taskLog = q.TaskProgress[index];
                    taskLog.TaskRecord = questRecord.Tasks()[index];
                }

                q.BaseQuestRecord = questRecord;
            }
        }

        public void ClearAllSideQuest()
        {
            var count = this.Data.Quests.Count;

            for (var i = 0; i < count; i++)
            {
                var quest = this.Data.Quests.ElementAt(i);

                if (quest.Value.QuestProviderType == QuestProviderType.Side)
                {
                    this.Data.Quests.Remove(quest.Key);
                }
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
            return this.Data.QuestRewarded.Where(x
                => x.Value.QuestId.Equals(questId)
                   && x.Value.ProviderId.Equals(providerId)
                   && x.Value.QuestStatus == QuestStatus.Rewarded).ToList().Count > 0;
        }

        public QuestLog GetQuest(string questId, string provideId) { return this.Data.Quests.FirstOrDefault(q => q.Key.Equals(questId) && q.Value.ProviderId.Equals(provideId)).Value; }

        private QuestLog GetQuestRewarded(string questId, string providerId)
        {
            return this.Data.QuestRewarded.FirstOrDefault(q => q.Key.Equals(questId) && q.Value.ProviderId.Equals(providerId)).Value;
        }

        public QuestLog CheckToAddNewQuest(string questId, string provideId, QuestProviderType questProviderType, IBaseQuestRecord baseQuestRecord)
        {
            if (this.IsQuestRewarded(questId, provideId, questProviderType))
            {
                this.LogMessage($"Quest {questId} is already completed for provider {provideId}", Color.red);

                return null;
            }

            if (!this.Data.Quests.TryGetValue(questId, out var questInfo))
            {
                var listTaskProgress = new List<TaskLog>();

                foreach (var taskRecord in baseQuestRecord.Tasks())
                {
                    var requirementProgress = new List<RequirementProgress>();

                    foreach (var requirementRecord in taskRecord.RequirementRecords())
                    {
                        requirementProgress.Add(new RequirementProgress()
                        {
                            RequirementId   = requirementRecord.GetRequirementId(),
                            RequirementType = requirementRecord.GetRequirementType(),
                            CurrentValue    = 0,
                            RequiredValue   = requirementRecord.GetRequirementValue(),
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
                    BaseQuestRecord   = baseQuestRecord,
                    QuestProviderType = questProviderType,
                    QuestType         = baseQuestRecord.QuestType,
                };
            }

            this.Data.Quests[questId] = questInfo;

            return questInfo;
        }

        public void UpdateTaskStatus(string questId, string providerId, string taskRecordTaskId, QuestStatus questStatus)
        {
            var questLog = this.GetQuest(questId, providerId) ?? this.GetQuestRewarded(questId, providerId);
            var taskLog  = questLog.TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskRecordTaskId));
            taskLog.TaskStatus = questStatus;
            this.signalBus.Fire(new TaskChangeStatusSignal(questLog, taskLog));
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

            result.AddRange(questInfo.BaseQuestRecord.QuestRewardRecords.Select(x => new RewardRecord()
            {
                RewardId    = x.QuestRewardId,
                RewardValue = x.QuestRewardValue,
                RewardType  = x.QuestRewardType
            }));

            return result;
        }

        private void Payout(IEnumerable<RewardRecord> assets, bool isSideQuest = false) { this.featureRewardHandler.AddRewards(assets, null); }

        /// <summary>
        /// Will change finish quest status to rewarded and payout all reward of quest
        /// </summary>
        /// <param name="questId"></param>
        /// <param name="providerId"></param>
        public void CheckToCompleteQuest(string questId, string providerId)
        {
            var questInfo = this.GetQuest(questId, providerId);

            //if (questInfo.TaskProgress.Any(t => t.TaskStatus != QuestStatus.Completed)) return;
            if (questInfo.QuestStatus != QuestStatus.Completed) return;

            this.SetQuestStatus(questInfo.QuestId, questInfo.ProviderId, QuestStatus.Rewarded);

            var listAsset = questInfo.BaseQuestRecord.QuestRewardRecords.Select(x => new RewardRecord()
            {
                RewardId    = x.QuestRewardId,
                RewardValue = x.QuestRewardValue,
                RewardType  = x.QuestRewardType
            });

            this.Payout(listAsset, questInfo.QuestProviderType == QuestProviderType.Side);
            this.signalBus.Fire<RefreshQuestViewSignal>();

            if (questInfo.QuestProviderType == QuestProviderType.Side)
            {
                this.signalBus.Fire(new TrackingQuestSignal("complete_side_quest", new List<string>()
                {
                    questInfo.QuestId
                }, 1));
            }
        }

        public List<QuestLog> GetAllQuestsType(QuestProviderType questProviderType, bool containQuestReward = false)
        {
            var result = new List<QuestLog>();
            result.AddRange(this.Data.Quests.Where(x => x.Value.QuestProviderType == questProviderType).Select(x => x.Value));

            if (containQuestReward)
            {
                result.AddRange(this.GetAllQuestRewarded(questProviderType));
            }

            return result;
        }

        public List<string> GetAllQuestsCategoryOfAQuestProvider(QuestProviderType questProviderId, bool containQuestReward = false)
        {
            var quests = this.GetAllQuestsType(questProviderId, containQuestReward);

            return quests.Select(q => q.QuestType).Distinct().ToList();
        }

        public List<QuestLog> GetAllQuests(QuestProviderType questProviderType, string questCategory, bool containQuestReward = false)
        {
            var quests = this.GetAllQuestsType(questProviderType);

            if (containQuestReward)
            {
                quests.AddRange(this.GetAllQuestRewarded(questProviderType));
            }

            return quests.Where(q => q.QuestType.Equals(questCategory)).ToList();
        }

        public QuestLog GetCurrentMainQuestNotFinish()
        {
            var totalMainQuests = this.GetAllQuestsType(QuestProviderType.Main).OrderBy(x => x.BaseQuestRecord.QuestIndex).ToList();

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

            if (questInfo.QuestStatus == QuestStatus.Rewarded)
            {
                this.QuestJournal.Quests.Remove(questInfo.QuestId);
                this.QuestJournal.QuestRewarded.Add(questInfo.QuestId, questInfo);
            }
        }

        public void RemoveQuest(string questId, string providerId)
        {
            var quest = this.GetQuest(questId, providerId);

            if (quest == null)
            {
                quest = this.GetQuestRewarded(questId, providerId);

                if (quest != null)
                {
                    this.Data.QuestRewarded.Remove(quest.QuestId);
                }

                return;
            }

            this.Data.Quests.Remove(quest.QuestId);
        }

        public void ResetQuest(string questId, string providerId)
        {
            var quest = this.GetQuest(questId, providerId);

            if (quest != null)
            {
                quest.TaskProgress.ForEach(x => x.Progress.ForEach(y => y.CurrentValue = 0));
                quest.TaskProgress.ForEach(x => x.TaskStatus = QuestStatus.NotStarted);
                quest.QuestStatus = QuestStatus.NotStarted;
            }
            else
            {
                var questRewarded = this.GetQuestRewarded(questId, providerId);

                if (questRewarded == null) return;
                this.Data.QuestRewarded.Remove(questId);

                questRewarded.TaskProgress.ForEach(x => x.Progress.ForEach(y => y.CurrentValue = 0));
                questRewarded.TaskProgress.ForEach(x => x.TaskStatus = QuestStatus.NotStarted);
                questRewarded.QuestStatus = QuestStatus.NotStarted;
                this.Data.Quests.Add(questRewarded.QuestId, questRewarded);
            }
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
            this.LogMessage($"Quest {questId}, done Task {taskLog.TaskRecord.TaskId},{taskLog.TaskRecord.TaskName}", Color.red);
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
            var allPremiseTaskComplete = questInfo.TaskProgress.All(task => task.TaskStatus is QuestStatus.Completed or QuestStatus.Rewarded && !task.TaskRecord.TaskOption);

            var checkCountTaskOption = questInfo.CountTaskOption >= questInfo.BaseQuestRecord.CountTaskOption;

            if (!allPremiseTaskComplete && questInfo.BaseQuestRecord.Tasks().Count(x => !x.TaskOption) == 0)
            {
                this.SetQuestStatus(questId, providerId, QuestStatus.Completed);
            }

            return allPremiseTaskComplete && checkCountTaskOption;
        }

        public TaskLog GetTaskLogOfQuest(string questId, string providerId, string taskId)
        {
            return this.GetQuest(questId, providerId).TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskId));
        }

        public List<QuestLog> GetAllQuestRewarded() => this.Data.QuestRewarded.Select(x => x.Value).ToList();

        public List<QuestLog> GetAllQuestRewarded(QuestProviderType providerType) => this.Data.QuestRewarded.Select(x => x.Value)
            .Where(x => x.QuestProviderType == providerType).ToList();

        public List<QuestLog> GetAllQuestCompleted() { return this.Data.Quests.Where(x => x.Value.QuestStatus == QuestStatus.Completed).Select(y => y.Value).ToList(); }

        public bool IsQuestRewarded(string questId, string provideId, QuestProviderType questProviderType) => this.Data.IsQuestRewarded(questId, provideId, questProviderType);
    }
}