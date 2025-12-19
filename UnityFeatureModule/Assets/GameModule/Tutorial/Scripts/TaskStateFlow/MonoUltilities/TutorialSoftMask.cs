namespace GameModule.Tutorial.Scripts.TaskStateFlow.MonoUltilities
{
    using Coffee.UISoftMask;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using GameFoundation.Scripts.Utilities.Extension;
    using UnityEngine;
    using UnityEngine.UI;

    public class TutorialSoftMask : MonoBehaviour
    {
        [SerializeField] private Canvas              currentCanvas;
        [SerializeField] private RectTransformFitter rectTransformFitter;
        [SerializeField] private Image               maskingShape;
        [SerializeField] private Sprite              defaultMaskSprite;
        [SerializeField] private GameObject          blockInputCanvas;

        [Header("Animation Config")]
        [SerializeField] private bool useAnimation = true;
        [SerializeField] private float fromScale         = 5f;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease  animationEase     = Ease.OutFlash;


        private Tweener fitTween;

        private void Awake()
        {
            this.GetCurrentContainer().Inject(this);
        }

        public async void ForceObject(GameObject targetObject)
        {
            this.blockInputCanvas.SetActive(true);
            var targetCanvas = targetObject.GetComponentInParent<Canvas>();
            if (targetCanvas != null && targetCanvas != this.currentCanvas)
            {
                this.currentCanvas.renderMode    = targetCanvas.renderMode == RenderMode.WorldSpace ? RenderMode.ScreenSpaceCamera : targetCanvas.renderMode;
                this.currentCanvas.worldCamera   = targetCanvas.worldCamera;
                this.currentCanvas.planeDistance = targetCanvas.planeDistance;
            }

            this.fitTween?.Kill(true);
            this.rectTransformFitter.enabled = true;
            this.rectTransformFitter.target  = targetObject.GetComponent<RectTransform>();
            var targetImage = targetObject.GetComponentInChildren<Image>();
            this.maskingShape.sprite = targetImage ? targetImage.sprite : this.defaultMaskSprite;


            if (!this.useAnimation) return;
            await UniTask.DelayFrame(2);
            this.rectTransformFitter.enabled = false;
            this.fitTween                    = this.rectTransformFitter.transform.DOScale(this.rectTransformFitter.transform.localScale, this.animationDuration).From(this.fromScale).SetEase(this.animationEase);
            this.fitTween.OnComplete(() => this.blockInputCanvas.SetActive(false));
        }


        public void Cleanup()
        {
            this.rectTransformFitter.target = null;
            this.maskingShape.sprite        = this.defaultMaskSprite;
            this.fitTween.Kill(true);
        }
    }
}
