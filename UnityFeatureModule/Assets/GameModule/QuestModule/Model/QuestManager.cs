namespace GameModule.QuestModule.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using ClaimReward;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.InterfacesAndEnumCommon;
    using FeatureTemplate.Scripts.RewardHandle;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.QuestModule.Signals;
    using global::Blueprints;
    using global::QuestModule.Provider;
    using Zenject;
    using RewardRecord = FeatureTemplate.Scripts.RewardHandle.RewardRecord;

    public class QuestManager : IFeatureControllerData
    {
        private readonly FeatureRewardHandler featureRewardHandler;
        private readonly FeatureDataState     featureDataState;
        private readonly QuestJournal         data;
        private readonly ISignalBus            signalBus;
        private readonly ScreenManager        screenManager;
        public           QuestJournal         QuestJournal => this.data;

        public Dictionary<string, Dictionary<string, int>> TrackingCached => this.data.TrackingCached;

        public QuestManager(FeatureRewardHandler featureRewardHandler, FeatureDataState featureDataState, QuestJournal data, ISignalBus signalBus, ScreenManager screenManager)
        {
            this.featureRewardHandler = featureRewardHandler;
            this.featureDataState     = featureDataState;
            this.data                 = data;
            this.signalBus            = signalBus;
            this.screenManager        = screenManager;
        }

        public async void LoadRecord(IQuestProvider questProvider)
        {
            await UniTask.WaitUntil((() => this.featureDataState.IsBlueprintAndLocalDataLoaded));

            foreach (var q in this.data.Quests.Where(x => x.Value.QuestProviderType == questProvider.QuestProviderType))
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

        /// <summary>
        /// Check Quest Accepted (for side quest)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        public bool CheckQuestAccepted(string id, string providerId)
        {
            return this.data.Quests.Where(x => x.Value.QuestId.Equals(id)
                                               && x.Value.ProviderId.Equals(providerId)).ToList().Count > 0;
        }

        /// <summary>
        /// Check Quest Completed but not receiving reward
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        public bool CheckCompleteNotReceivingRewardQuest(string id, string providerId)
        {
            return this.data.Quests.Where(x
                => x.Value.QuestId.Equals(id)
                   && x.Value.ProviderId.Equals(providerId)
                   && x.Value.QuestStatus == QuestStatus.Completed).ToList().Count > 0;
        }

        /// <summary>
        /// Check Quest Done
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        public bool CheckQuestDone(string id, string providerId)
        {
            return this.data.Quests.Where(x
                => x.Value.QuestId.Equals(id)
                   && x.Value.ProviderId.Equals(providerId)
                   && x.Value.QuestStatus == QuestStatus.Rewarded).ToList().Count > 0;
        }

        /// <summary>
        /// Get Quest from quest journal
        /// </summary>
        /// <param name="questId"></param>
        /// <param name="provideId"></param>
        /// <returns></returns>
        public QuestLog GetQuest(string questId, string provideId) { return this.data.Quests.FirstOrDefault(q => q.Key.Equals(questId) && q.Value.ProviderId.Equals(provideId)).Value; }

        /// <summary>
        /// Check To Add new quest to quest journal, if quest is not exist, create new quest and get first task is in progress (ussualy for main quest)
        /// </summary>
        /// <param name="questId"></param>
        /// <param name="provideId"></param>
        /// <param name="questProviderType"></param>
        /// <param name="questRecord"></param>
        /// <returns></returns>
        public QuestLog CheckToAddNewQuest(string questId, string provideId, QuestProviderType questProviderType, QuestRecord questRecord)
        {
            if (!this.data.Quests.TryGetValue(questId, out var questInfo))
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
                        Progress   = requirementProgress,
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

            this.data.Quests[questId] = questInfo;

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

        /// <summary>
        /// Set Quest status
        /// </summary>
        /// <param name="provideId"></param>
        /// <param name="questId"></param>
        /// <param name="questStatus"></param>
        public void UpdateQuestStatus(string provideId, string questId, QuestStatus questStatus)
        {
            var questInfo = this.GetQuest(questId, provideId);

            questInfo.QuestStatus = questStatus;
        }

        /// <summary>
        /// Set task status
        /// </summary>
        /// <param name="questInfoProviderId"></param>
        /// <param name="questInfoQuestId"></param>
        /// <param name="taskRecordTaskId"></param>
        /// <param name="questStatus"></param>
        public void UpdateTaskStatus(string questInfoProviderId, string questInfoQuestId, string taskRecordTaskId, QuestStatus questStatus)
        {
            var taskLog = this.GetQuest(questInfoQuestId, questInfoProviderId).TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskRecordTaskId));
            taskLog.TaskStatus = questStatus;
        }

        /// <summary>
        /// Give all task reward to quest and set task status to rewarded
        /// </summary>
        /// <param name="questInfoProviderId"></param>
        /// <param name="questInfoQuestId"></param>
        public void GiveAllTaskRewardToQuest(string questInfoProviderId, string questInfoQuestId, QuestStatus status = QuestStatus.Rewarded)
        {
            var quest    = this.GetQuest(questInfoQuestId, questInfoProviderId);
            var taskLogs = quest.TaskProgress.Where(t => t.TaskStatus == QuestStatus.Completed).ToList();

            foreach (var taskLog in taskLogs)
            {
                taskLog.TaskStatus = status;

                var listAsset = taskLog.TaskRecord.RewardRecords.Select(x => new RewardRecord()
                {
                    RewardId    = x.TaskRewardId,
                    RewardValue = x.TaskRewardValue,
                    RewardType  = x.TaskRewardType
                });

                this.Payout(listAsset.ToList<IRewardRecord>(), quest.QuestProviderType == QuestProviderType.Side);
            }
        }

        /// <summary>
        /// Get first task complete of quest and payout reward
        /// </summary>
        /// <param name="questInfoProviderId"></param>
        /// <param name="questInfoQuestId"></param>
        public void TryToGetFirstTaskCompleteOfQuestAndPayoutReward(string questInfoProviderId, string questInfoQuestId)
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

            this.Payout(listAsset.ToList<IRewardRecord>(), quest.QuestProviderType == QuestProviderType.Side);
        }

        /// <summary>
        /// GetAll task reward from quest and set task status to rewarded
        /// </summary>
        /// <param name="questLogProviderId"></param>
        /// <param name="questId"></param>
        /// <returns></returns>
        public List<IRewardRecord> GetTaskReward(string questLogProviderId, string questId)
        {
            var quest     = this.GetQuest(questId, questLogProviderId);
            var taskLogs  = quest.TaskProgress.Where(t => t.TaskStatus == QuestStatus.Completed).ToList();
            var listAsset = new List<IRewardRecord>();

            foreach (var taskLog in taskLogs)
            {
                taskLog.TaskStatus = QuestStatus.Rewarded;

                listAsset.AddRange(taskLog.TaskRecord.RewardRecords.Select(x => new RewardRecord()
                {
                    RewardId    = x.TaskRewardId,
                    RewardValue = x.TaskRewardValue,
                    RewardType  = x.TaskRewardType
                }));
            }

            return listAsset;
        }

        /// <summary>
        /// Get Current Task reward of quest and set task status to rewarded
        /// </summary>
        /// <param name="questLogProviderId"></param>
        /// <param name="questId"></param>
        /// <returns></returns>
        public List<IRewardRecord> GetCurrentTaskReward(string questLogProviderId, string questId)
        {
            var quest     = this.GetQuest(questId, questLogProviderId);
            var taskLog   = quest.TaskProgress.FirstOrDefault(t => t.TaskStatus == QuestStatus.Completed);
            var listAsset = new List<IRewardRecord>();

            if (taskLog == null) return listAsset;
            taskLog.TaskStatus = QuestStatus.Rewarded;

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
        /// <param name="status"></param>
        /// <returns></returns>
        public List<IRewardRecord> GetAllQuestRewardAndSetStatus(string questLogProviderId, string questLogQuestId, QuestStatus status = QuestStatus.Rewarded)
        {
            var questInfo = this.GetQuest(questLogQuestId, questLogProviderId);

            var result = new List<IRewardRecord>();

            if (questInfo.QuestStatus != QuestStatus.Completed) return result;
            questInfo.QuestStatus = status;

            result.AddRange(questInfo.QuestRecord.QuestRewardRecords.Select(x => new RewardRecord()
            {
                RewardId    = x.QuestRewardId,
                RewardValue = x.QuestRewardValue,
                RewardType  = x.QuestRewardType
            }));

            return result;
        }

        private void Payout(List<IRewardRecord> assets, bool isSideQuest = false) { this.featureRewardHandler.AddRewards(assets, null); }

        /// <summary>
        /// Use For sidequest only
        /// </summary>
        /// <param name="questInfoProviderId"></param>
        /// <param name="questInfoQuestId"></param>
        public void CheckToCompleteQuest(string questInfoProviderId, string questInfoQuestId)
        {
            var questInfo = this.GetQuest(questInfoQuestId, questInfoProviderId);

            //if (questInfo.TaskProgress.Any(t => t.TaskStatus != QuestStatus.Completed)) return;
            if (questInfo.QuestStatus != QuestStatus.Completed) return;
            questInfo.QuestStatus = QuestStatus.Rewarded;

            var listAsset = questInfo.QuestRecord.QuestRewardRecords.Select(x => new RewardRecord()
            {
                RewardId    = x.QuestRewardId,
                RewardValue = x.QuestRewardValue,
                RewardType  = x.QuestRewardType
            });

            var listIRewardRecord = listAsset.ToList<IRewardRecord>();

            this.Payout(listIRewardRecord, questInfo.QuestProviderType == QuestProviderType.Side);
            this.signalBus.Fire<RefreshQuestViewSignal>();

            if (questInfo.QuestProviderType == QuestProviderType.Side)
            {
                this.signalBus.Fire(new TrackingQuestSignal(StaticValue.RequirementStaticValue.CompleteASideQuest, questInfo.QuestId, 1));
            }
        }

        /// <summary>
        /// Get All quest type of quest already received
        /// </summary>
        /// <param name="questProviderType"></param>
        /// <returns></returns>
        public List<QuestLog> GetAllQuestsType(QuestProviderType questProviderType)
        {
            return this.data.Quests.Where(x => x.Value.QuestProviderType == questProviderType).Select(x => x.Value).ToList();
        }

        /// <summary>
        /// Get All quest category of a quest provider
        /// </summary>
        /// <param name="questProviderId"></param>
        /// <returns></returns>
        public List<string> GetAllQuestsCategoryOfAQuestProvider(QuestProviderType questProviderId)
        {
            var quests = this.GetAllQuestsType(questProviderId);

            return quests.Select(q => q.QuestType).Distinct().ToList();
        }

        /// <summary>
        /// Get All Quest of a quest category
        /// </summary>
        /// <param name="questProviderType"></param>
        /// <param name="questCategory"></param>
        /// <returns></returns>
        public List<QuestLog> GetAllQuests(QuestProviderType questProviderType, string questCategory)
        {
            var quests = this.GetAllQuestsType(questProviderType);

            return quests.Where(q => q.QuestType.Equals(questCategory)).ToList();
        }

        /// <summary>
        /// Get Current Main Quest
        /// </summary>
        /// <returns></returns>
        public QuestLog GetCurrentMainQuest()
        {
            var mainQuestInprogress = this.data.Quests.FirstOrDefault(x => x.Value.QuestProviderType == QuestProviderType.Main && x.Value.QuestStatus == QuestStatus.InProgress).Value;

            if (mainQuestInprogress != null)
            {
                return mainQuestInprogress;
            }

            var mainQuestCompleted = this.data.Quests.FirstOrDefault(x => x.Value.QuestProviderType == QuestProviderType.Main && x.Value.QuestStatus == QuestStatus.Completed).Value;

            return mainQuestCompleted;
        }

        public void SetQuestStatus(string questInfoProviderId, string questInfoQuestId, QuestStatus status)
        {
            var questInfo = this.GetQuest(questInfoQuestId, questInfoProviderId);
            questInfo.QuestStatus = status;
        }

        public void RemoveQuest(string modelProviderId, string modelQuestId)
        {
            var quest = this.GetQuest(modelQuestId, modelProviderId);
            this.data.Quests.Remove(quest.QuestId);
        }

        public void ShowPopupClaimReward(QuestLog questLog, bool isMainOnly = false, bool isCurrentTaskOnly = false)
        {
            var listAsset = new List<IRewardRecord>();

            if (isCurrentTaskOnly)
            {
                listAsset = this.GetCurrentTaskReward(questLog.ProviderId, questLog.QuestId);
            }
            else if (isMainOnly)
            {
                listAsset.AddRange(this.GetAllQuestRewardAndSetStatus(questLog.ProviderId, questLog.QuestId));
            }
            else
            {
                listAsset.AddRange(this.GetTaskReward(questLog.ProviderId, questLog.QuestId));
                listAsset.AddRange(this.GetAllQuestRewardAndSetStatus(questLog.ProviderId, questLog.QuestId));
            }

            this.screenManager.OpenScreen<ClaimRewardPopupPresenter, ClaimRewardPopupModel>(new ClaimRewardPopupModel()
            {
                RewardResult = listAsset
            }).Forget();
        }

        public void ResetQuest(string questId, string providerId)
        {
            var quest = this.GetQuest(questId, providerId);
            quest.TaskProgress.ForEach(x => x.Progress.ForEach(y => y.CurrentValue = 0));
            quest.TaskProgress.ForEach(x => x.TaskStatus = QuestStatus.NotStarted);
            quest.QuestStatus = QuestStatus.NotStarted;
        }

        public void UpdateCountRequirementOption(string questInfoProviderId, string questInfoQuestId, string taskRecordTaskId)
        {
            var taskLog = this.GetQuest(questInfoQuestId, questInfoProviderId).TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskRecordTaskId));
            taskLog.CountRequirementOption++;
        }

        public void CheckTaskCompleted(string questInfoProviderId, string questInfoQuestId, string taskRecordTaskId)
        {
            var questInfo = this.GetQuest(questInfoQuestId, questInfoProviderId);
            var taskLog   = questInfo.TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskRecordTaskId));

            var allRequirementPremiseCompleted = taskLog.Progress.All(x => x.CurrentValue >= x.RequiredValue && !x.IsOptional);

            if (!allRequirementPremiseCompleted && taskLog.Progress.All(x => x.IsOptional))
            {
                allRequirementPremiseCompleted = true;
            }

            if (taskLog.CountRequirementOption < taskLog.TaskRecord.CoutRequirementOption || !allRequirementPremiseCompleted) return;
            this.UpdateTaskStatus(questInfoProviderId, questInfoQuestId, taskLog.TaskRecord.TaskId, QuestStatus.Completed);
            this.CountingTaskOption(questInfoProviderId, questInfoQuestId, taskRecordTaskId);
        }

        public void CountingTaskOption(string questInfoProviderId, string questInfoQuestId, string taskId)
        {
            var quest   = this.GetQuest(questInfoQuestId, questInfoProviderId);
            var taskLog = this.GetQuest(questInfoQuestId, questInfoProviderId).TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskId));

            if (taskLog.TaskRecord.TaskOption)
            {
                quest.CountTaskOption++;
            }
        }

        public bool CheckAllTaskCompleted(string questInfoProviderId, string questInfoQuestId)
        {
            var questInfo              = this.GetQuest(questInfoQuestId, questInfoProviderId);
            var allPremiseTaskComplete = questInfo.TaskProgress.All(task => task.TaskStatus == QuestStatus.Completed && !task.TaskRecord.TaskOption);

            if (!allPremiseTaskComplete && questInfo.TaskProgress.All(x => x.TaskRecord.TaskOption))
            {
                allPremiseTaskComplete = true;
            }

            var checkCountTaskOption = questInfo.CountTaskOption >= questInfo.QuestRecord.CountTaskOption;

            return allPremiseTaskComplete && checkCountTaskOption;
        }

        public TaskLog GetTaskLogOfQuest(string questInfoProviderId, string questInfoQuestId, string taskId)
        {
            return this.GetQuest(questInfoQuestId, questInfoProviderId).TaskProgress.First(task => task.TaskRecord.TaskId.Equals(taskId));
        }
    }
}