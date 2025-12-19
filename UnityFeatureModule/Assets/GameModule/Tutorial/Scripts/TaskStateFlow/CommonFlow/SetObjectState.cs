namespace GameModule.Tutorial.Scripts.TaskStateFlow.CommonFlow
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Handle;
    using FeatureTemplate.Scripts.Services;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameModule.Tutorial.Scripts.DataState;
    using GameModule.Tutorial.Scripts.Services;
    using GameModule.Tutorial.Scripts.TaskStateFlow.Effects;
    using GameModule.Tutorial.Scripts.TaskStateFlow.MonoUltilities;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.UI;
    using Zenject;

    [Flags]
    public enum ObjectStateType
    {
        Normal        = 1 << 0,
        Activate      = 1 << 1,
        Deactivate    = 1 << 2,
        Force         = 1 << 3,
        Block         = 1 << 4,
        ForceSoftMask = 1 << 5,
        None          = 1 << 6,
    }

    [Serializable]
    public class ObjectStateInfo
    {
        public string          gameObjectPath;
        public ObjectStateType state;

        public bool isResetOnComplete;

        [Tooltip("Delay time to apply effects after setting state")]
        public float delayApplyTime;

        [ShowIf("CanApplyEffects")] [SerializeReference]
        public List<IFtueEffect> effects;

        internal GameObject        ResolvedObject;
        internal GameObjectWrapper WrappedObject;
        internal IDisposable       DelayTimeObserver;

        public bool CanApplyEffects() { return (this.state & ObjectStateType.Deactivate) == 0; }
    }

    public class SetObjectStateData : IActionData
    {
        public List<ObjectStateInfo> ObjectStates = new();
    }

    public class SetObjectState : BaseTaskFlow<SetObjectStateData>, ITickable
    {
        private readonly ScreenManager screenManager;

        private GameObject                                       tutorialDarkMask;
        private TutorialSoftMask                                 tutorialSoftMask;
        private Dictionary<string, ObjectStateInfo>              cachedObjectStates = new();
        private Tuple<TutorialTaskDataState, SetObjectStateData> CurrentProcessingTask;
        public SetObjectState(ISignalBus signalBus, ScreenManager screenManager) : base(signalBus) { this.screenManager = screenManager; }

        public override string Id => "set_object_state";

        public void Tick()
        {
            if (this.CurrentProcessingTask?.Item1.TaskState is not TutorialState.Completed)
            {
                return;
            }

            foreach (var objectStateInfo in this.CurrentProcessingTask.Item2.ObjectStates)
            {
                if (!objectStateInfo.isResetOnComplete) continue;

                this.CleanupCachedObjectState(objectStateInfo.gameObjectPath);
            }
            this.CurrentProcessingTask=null;
        }

        protected override UniTask ProcessInternal(TutorialTaskDataState taskDataState, SetObjectStateData tutorialTaskData)
        {
            this.CurrentProcessingTask = new Tuple<TutorialTaskDataState, SetObjectStateData>(taskDataState, tutorialTaskData);

            foreach (var objectStateInfo in tutorialTaskData.ObjectStates)
            {
                if (this.cachedObjectStates.TryGetValue(objectStateInfo.gameObjectPath, out var cachedObjectState))
                {
                    //skip if state is the same and no effects to apply
                    if (cachedObjectState.state == objectStateInfo.state && (cachedObjectState.effects == null && objectStateInfo.effects == null))
                    {
                        cachedObjectState.isResetOnComplete = objectStateInfo.isResetOnComplete;

                        continue;
                    }

                    //cleanup old state
                    this.CleanupCachedObjectState(cachedObjectState.gameObjectPath);

                    objectStateInfo.ResolvedObject = cachedObjectState.ResolvedObject;
                }
                else
                {
                    if (!FeatureObjectCollectionServices.Instance.GetObjectInstanceByPath(objectStateInfo.gameObjectPath, out var targetObject)) continue;
                    objectStateInfo.ResolvedObject = targetObject;
                }

                this.cachedObjectStates[objectStateInfo.gameObjectPath] = objectStateInfo;

                // Debug.Log($"Setup object state {objectStateInfo.GameObjectPath} - {objectStateInfo.State}");

                if (objectStateInfo.state.HasFlag(ObjectStateType.Deactivate))
                {
                    objectStateInfo.ResolvedObject.SetActive(false);

                    continue;
                }

                if (objectStateInfo.state.HasFlag(ObjectStateType.Activate))
                {
                    objectStateInfo.ResolvedObject.SetActive(true);
                }
                else if (!objectStateInfo.state.HasFlag(ObjectStateType.None))
                {
                    if (!objectStateInfo.ResolvedObject.activeInHierarchy)
                    {
                        //ensure object is active for other state changes
                        objectStateInfo.ResolvedObject.SetActive(true);
                    }
                }

                this.SetupForceObject(objectStateInfo);

                if (objectStateInfo.state.HasFlag(ObjectStateType.Block))
                {
                    this.SetBlockObject(objectStateInfo.ResolvedObject, true);
                }

                //setup highlight effects
                this.ApplyEffects(objectStateInfo);
            }

            return UniTask.CompletedTask;
        }

        private void UnForceObject(ObjectStateInfo objectStateInfo)
        {
            if (objectStateInfo.state.HasFlag(ObjectStateType.Force))
            {
                if (this.tutorialDarkMask != null) this.tutorialDarkMask.SetActive(false);

                if (objectStateInfo.WrappedObject == null) return;
                objectStateInfo.WrappedObject.SetOriginParent();
                objectStateInfo.WrappedObject = null;
            }
            else if (objectStateInfo.state.HasFlag(ObjectStateType.ForceSoftMask))
            {
                if (this.tutorialSoftMask == null) return;
                this.tutorialSoftMask.Cleanup();
                this.tutorialSoftMask.gameObject.SetActive(false);
            }
        }

        private void SetBlockObject(GameObject targetObject, bool isBlock)
        {
            var childButtonObj = targetObject.GetComponentsInChildren<Button>();

            foreach (var button in childButtonObj)
            {
                button.interactable = !isBlock;
            }
        }

        private void CleanupCachedObjectState(string gameObjectPath)
        {
            try
            {
                var objectStateInfo = this.cachedObjectStates[gameObjectPath];

                if (objectStateInfo.state.HasFlag(ObjectStateType.Deactivate))
                {
                    objectStateInfo.ResolvedObject.SetActive(true);
                }

                if (objectStateInfo.state.HasFlag(ObjectStateType.Activate))
                {
                    objectStateInfo.ResolvedObject.SetActive(false);
                }

                this.UnForceObject(objectStateInfo);

                if (objectStateInfo.state.HasFlag(ObjectStateType.Block))
                {
                    this.SetBlockObject(objectStateInfo.ResolvedObject, false);
                }

                //cleanup highlight effects
                if (objectStateInfo.effects != null)
                {
                    objectStateInfo.DelayTimeObserver?.Dispose();

                    foreach (var effectInfo in objectStateInfo.effects)
                    {
                        effectInfo.Cleanup();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error cleanup object state {gameObjectPath} - {e}");
            }
            finally
            {
                this.cachedObjectStates.Remove(gameObjectPath);
            }
        }

        private void SetupForceObject(ObjectStateInfo objectStateInfo)
        {
            if (objectStateInfo.state.HasFlag(ObjectStateType.Force))
            {
                //setup highlight effects
                objectStateInfo.WrappedObject = new GameObjectWrapper(objectStateInfo.ResolvedObject);

                if (this.tutorialDarkMask == null)
                {
                    this.tutorialDarkMask = Addressables.InstantiateAsync("TutorialDarkMask", Vector3.zero, Quaternion.identity).WaitForCompletion();
                    this.tutorialDarkMask.transform.SetParent(this.screenManager.CurrentOverlayRoot, false);
                }

                this.tutorialDarkMask.SetActive(true);
                this.tutorialDarkMask.transform.SetAsLastSibling();
                objectStateInfo.WrappedObject.SetNewParent(this.tutorialDarkMask.transform);
            }
            else if (objectStateInfo.state.HasFlag(ObjectStateType.ForceSoftMask))
            {
                if (this.tutorialSoftMask == null)
                {
                    this.tutorialSoftMask = Addressables.InstantiateAsync("TutorialSoftMask", Vector3.zero, Quaternion.identity).WaitForCompletion().GetComponent<TutorialSoftMask>();
                    this.tutorialSoftMask.transform.SetParent(this.screenManager.RootUICanvas.transform, false);
                }

                this.tutorialSoftMask.gameObject.SetActive(true);
                this.tutorialSoftMask.ForceObject(objectStateInfo.ResolvedObject);
            }
        }

        private void ApplyEffects(ObjectStateInfo objectStateInfo)
        {
            if (objectStateInfo.effects == null) return;

            if (objectStateInfo.delayApplyTime > 0)
            {
                objectStateInfo.DelayTimeObserver = Observable.Timer(TimeSpan.FromSeconds(objectStateInfo.delayApplyTime))
                    .Subscribe(_ => { ApplyEffectsInternal(); });
            }
            else
            {
                ApplyEffectsInternal();
            }

            return;

            void ApplyEffectsInternal()
            {
                foreach (var effectInfo in objectStateInfo.effects)
                {
                    effectInfo.Initialize(objectStateInfo.ResolvedObject);
                }
            }
        }
    }
}