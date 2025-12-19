namespace GameModule.Tutorial.Scripts.TaskStateFlow.Effects
{
    using Coffee.UIEffects;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.Extension;
    using Sirenix.OdinInspector;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    public class UIEffectInfo : IFtueEffect
    {
        public                bool           IsApplyToChildren = false;
        [InlineEditor] public UIEffectPreset EffectPreset;

#if UNITY_EDITOR
        [Button]
        [PropertyOrder(-1)]
        private void CreateNewPreset()
        {
            UIEffectPreset newPreset = ScriptableObject.CreateInstance<UIEffectPreset>();
            string         path      = "Assets/ScriptableObjects/UIEffectPreset/NewUIEffectPreset.asset";
            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();
            this.EffectPreset = newPreset;
        }
#endif

        private UIEffect uiEffectInstance;

        public UniTask Initialize(GameObject targetObject)
        {
            this.uiEffectInstance = targetObject.GetComponent<UIEffect>();

            if (this.uiEffectInstance == null)
            {
                this.uiEffectInstance = targetObject.AddComponent<UIEffect>();
            }
            else
            {
                // todo cache existing effect to avoid losing settings on
            }

            this.uiEffectInstance.LoadPreset(this.EffectPreset);

            if (this.IsApplyToChildren)
            {
                var imagesInChildren = targetObject.GetComponentsInChildren<Image>();

                foreach (var child in imagesInChildren)
                {
                    if (child.gameObject == targetObject) continue;
                    var childReplica = child.gameObject.GetOrAddComponent<UIEffectReplica>();
                    childReplica.target             = this.uiEffectInstance;
                    childReplica.useTargetTransform = true;
                }
            }

            return UniTask.CompletedTask;
        }

        public void Cleanup()
        {
            if (this.uiEffectInstance != null)
            {
                if (this.IsApplyToChildren)
                {
                    var replicas = this.uiEffectInstance.GetComponentsInChildren<UIEffectReplica>();

                    foreach (var replica in replicas)
                    {
                        Object.Destroy(replica);
                    }
                }

                Object.Destroy(this.uiEffectInstance);
                this.uiEffectInstance = null;
            }
        }
    }
}