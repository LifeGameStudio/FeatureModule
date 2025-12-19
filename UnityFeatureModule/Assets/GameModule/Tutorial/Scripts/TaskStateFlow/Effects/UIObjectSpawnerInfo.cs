namespace GameModule.Tutorial.Scripts.TaskStateFlow.Effects
{
    using System;
    using BlueprintFlow.BlueprintReader.Converter.TypeConversion;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using GameModule.Tutorial.Scripts.Helper;
    using UnityEngine;

    [Serializable]
    public class UIObjectSpawnerInfo : IFtueEffect
    {
        public string ObjectAddressablePath;
        public bool   IsFistSiblingInTransform;

        public AnchorPreset Anchor = AnchorPreset.MiddleCenter;
        public string       Offset="1|1|1";
        public string       Rotation="0|0|0";
        public string       ScaleFactor="1|1|1";

        internal GameObject LoadedObject;

        private UnityVector2Converter vector2Converter = new();
        private UnityVector3Converter vector3Converter = new();

        public virtual async UniTask Initialize(GameObject targetObject)
        {
            var vOffset      = (Vector2)this.vector2Converter.ConvertFromString(this.Offset, typeof(Vector2));
            var vRotation    = (Vector3)this.vector3Converter.ConvertFromString(this.Rotation, typeof(Vector3));
            var vScale       = (Vector3)this.vector3Converter.ConvertFromString(this.ScaleFactor, typeof(Vector3));
            var effectObject = await ObjectPoolManager.Instance.Spawn(this.ObjectAddressablePath);

            if (effectObject == null) return;

            this.LoadedObject = effectObject;
            var effectObjTransform = effectObject.GetComponent<RectTransform>();
            effectObjTransform.SetParent(targetObject.transform, false);

            if (this.IsFistSiblingInTransform) effectObjTransform.SetAsFirstSibling();
            effectObjTransform.SetAnchor(this.Anchor).SetAnchoredPosition(vOffset);
            effectObjTransform.localEulerAngles = vRotation;
            effectObjTransform.localScale       = vScale;
        }

        public virtual void Cleanup()
        {
            if (this.LoadedObject != null)
            {
                // Debug.Log($"Recycle Object {this.LoadedObject.name} ");
                this.LoadedObject.Recycle();
                this.LoadedObject = null;
            }
        }
    }
}