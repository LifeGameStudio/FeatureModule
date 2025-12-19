namespace GameModule.Tutorial.Scripts.TaskStateFlow.Effects
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.UI;
    using Object = UnityEngine.Object;

    public class DotweenAnimationEffect : IFtueEffect
    {
        public bool PlayInSequence;

        [SerializeReference] public List<TweenAnimationInfo> Animations;

        private Sequence    currentSequence;
        private List<Tween> currentTweens = new List<Tween>();

        public UniTask Initialize(GameObject targetObject)
        {
            if (this.PlayInSequence)
            {
                this.currentSequence = DOTween.Sequence();

                foreach (var anim in this.Animations)
                {
                    var tween = anim.CreateTween(targetObject);

                    if (tween == null) continue;
                    this.currentSequence.Append(tween);
                }

                this.currentSequence.Play();
            }
            else
            {
                this.currentTweens.Clear();

                foreach (var anim in this.Animations)
                {
                    var tween = anim.CreateTween(targetObject);

                    if (tween == null) continue;
                    tween.Play();
                    this.currentTweens.Add(tween);
                }
            }

            return UniTask.CompletedTask;
        }

        public void Cleanup()
        {
            if (this.PlayInSequence)
            {
                if (this.currentSequence != null)
                {
                    this.currentSequence.Kill();
                    this.currentSequence = null;
                }
            }
            else
            {
                foreach (var tween in this.currentTweens)
                {
                    tween.Kill();
                }

                this.currentTweens.Clear();
            }
        }
    }

    [Serializable]
    public abstract class TweenAnimationInfo
    {
        public float    delay;
        public float    duration = 1;
        public Ease     easeType = Ease.OutQuad;
        public LoopType loopType = LoopType.Restart;
        public int      loops    = 1;
        public string   id       = "";
        public bool     isFrom;

        public abstract Tween CreateTween(GameObject target);

        public T SetCommonSettings<T>(T tween) where T : Tween
        {
            return tween
                .SetDelay(this.delay)
                .SetEase(this.easeType)
                .SetLoops(this.loops, this.loopType)
                .SetId(this.id);
        }
    }

    public class FadeTweenAnimationInfo : TweenAnimationInfo
    {
        [ShowIf("isFrom")]   public float startAlpha = 0;
        [ShowIf("@!isFrom")] public float endAlpha   = 1;

        public override Tween CreateTween(GameObject target)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

            var tween = canvasGroup.DOFade(this.isFrom ? this.startAlpha : this.endAlpha, this.duration);
            this.SetCommonSettings(tween).OnKill(() => Object.Destroy(canvasGroup));

            return this.isFrom ? tween.From() : tween;
        }
    }

    public class ScaleTweenAnimationInfo : TweenAnimationInfo
    {
        [ShowIf("isFrom")]   public Vector3 startScale = Vector3.zero;
        [ShowIf("@!isFrom")] public Vector3 endScale   = Vector3.one;

        public override Tween CreateTween(GameObject target)
        {
            var tween = target.transform.DOScale(this.isFrom ? this.startScale : this.endScale, this.duration);

            return this.isFrom ? this.SetCommonSettings(tween).From() : this.SetCommonSettings(tween);
        }
    }

    public class SliderValueTweenAnimationInfo : TweenAnimationInfo
    {
        [ShowIf("isFrom")]   public float startValue = 0;
        [ShowIf("@!isFrom")] public float endValue   = 1;

        public override Tween CreateTween(GameObject target)
        {
            var slider = target.GetComponent<Slider>();

            if (slider == null)
            {
                Debug.LogError("SliderValueTweenAnimationInfo requires a Slider component on the target object.", target);

                return null;
            }

            var tween = DOTween.To(() => slider.value, x => slider.value = x, this.isFrom ? this.startValue : this.endValue, this.duration);

            return this.isFrom ? this.SetCommonSettings(tween).From() : this.SetCommonSettings(tween);
        }
    }
}