namespace GameModule.Tutorial.Scripts.Services
{
    using FeatureTemplate.Scripts.Services;
    using UnityEngine;
    using UnityEngine.UI;

    public class GameObjectWrapper
    {
        public readonly GameObject Value;

        private Transform     OriginParent       { get; }
        private int           OriginSiblingIndex { get; }
        private Vector3       OriginLocalPos     { get; }
        private Quaternion    OriginLocalRot     { get; }
        private Vector3       OriginLocalScale   { get; }
        private RectTransform substituteTransform;
        private Canvas        OriginCanvas { get; }

        public GameObjectWrapper(string name) : this(FeatureObjectCollectionServices.Instance.GetObjectInstanceByPath(name)) { }

        public GameObjectWrapper(GameObject originObj)
        {
            this.Value = originObj;
            if (originObj == null) return;

            this.OriginSiblingIndex = this.Value.transform.GetSiblingIndex();
            this.OriginParent       = this.Value.transform.parent;
            this.OriginLocalPos     = this.Value.transform.localPosition;
            this.OriginLocalRot     = this.Value.transform.localRotation;
            this.OriginLocalScale   = this.Value.transform.localScale;
            this.OriginCanvas       = this.Value.GetComponentInParent<Canvas>();
        }

        public void SetActive(bool isActive) { this.Value.SetActive(isActive); }

        public T GetComponent<T>() { return this.Value.GetComponent<T>(); }

        public void SetNewParent(Transform parent)
        {
            this.Value.transform.SetParent(parent);

            this.Value.transform.SetAsLastSibling();
            if (this.OriginParent.gameObject.activeInHierarchy && this.OriginParent.GetComponent<LayoutGroup>())
            {
                var rectTransform = this.Value.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    this.substituteTransform = new GameObject($"{this.Value.name}_").AddComponent<RectTransform>();
                    this.substituteTransform.SetParent(this.OriginParent);
                    this.substituteTransform.localScale = this.OriginLocalScale;
                    this.substituteTransform.sizeDelta  = rectTransform.sizeDelta;
                    this.substituteTransform.SetSiblingIndex(this.OriginSiblingIndex);

                    var layoutElement = this.Value.GetComponent<LayoutElement>();
                    if (layoutElement != null)
                    {
                        //copy to substitute
                        var substituteLayoutElement = this.substituteTransform.gameObject.AddComponent<LayoutElement>();
                        substituteLayoutElement.preferredWidth  = layoutElement.preferredWidth;
                        substituteLayoutElement.preferredHeight = layoutElement.preferredHeight;
                        substituteLayoutElement.flexibleWidth   = layoutElement.flexibleWidth;
                        substituteLayoutElement.flexibleHeight  = layoutElement.flexibleHeight;
                        substituteLayoutElement.minWidth        = layoutElement.minWidth;
                        substituteLayoutElement.minHeight       = layoutElement.minHeight;
                        substituteLayoutElement.layoutPriority  = layoutElement.layoutPriority;
                    }
                }
            }
        }

        public void SetOriginParent()
        {
            if (this.substituteTransform != null)
                Object.Destroy(this.substituteTransform.gameObject);

            if (this.Value == null) return;
            this.Value.transform.SetParent(this.OriginParent);
            this.Value.transform.localPosition = this.OriginLocalPos;
            this.Value.transform.localRotation = this.OriginLocalRot;
            this.Value.transform.localScale    = this.OriginLocalScale;
            this.Value.transform.SetSiblingIndex(this.OriginSiblingIndex);
        }

    }
}
