using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

namespace TuanjieAI.UI
{
    /// <summary>
    /// ItemInfo 转场效果：Scale 从 0 到 1 的过程中，播放马粒子消散特效作为转场遮罩。
    /// 支持两种特效源：序列帧动画（SequenceFrameAnimation）或 VideoPlayer。
    /// 流程：马特效出现 → ItemInfo 从 0 缩放到 1 → 马特效消散 → 转场完成。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ItemTransitionEffect : MonoBehaviour
    {
        [Header("转场时长")]
        [Tooltip("Scale 动画总时长（秒）")]
        public float transitionDuration = 2f;

        [Tooltip("马特效在 Scale 动画开始前的提前播放时间（秒）")]
        public float vfxLeadTime = 0.3f;

        [Tooltip("Scale 动画完成后马特效继续播放的消散时间（秒）")]
        public float vfxTrailTime = 1.5f;

        [Header("缩放曲线")]
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("马特效引用")]
        [Tooltip("马特效的 GameObject（用于控制可见性）")]
        public GameObject horseVFXObject;

        [Tooltip("序列帧动画组件（优先使用，若不为 null 则忽略 VideoPlayer）")]
        public SequenceFrameAnimation horseFrameAnimation;

        [Tooltip("马粒子特效的 VideoPlayer（备选，当 horseFrameAnimation 为空时使用）")]
        public VideoPlayer horseVideoPlayer;

        [Tooltip("马特效的 RectTransform（可选）")]
        public RectTransform horseTransform;

        [Header("自动播放")]
        public bool playOnStart = true;

        private RectTransform _rectTransform;
        private Vector3 _targetScale;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _targetScale = _rectTransform.localScale;
            _rectTransform.localScale = Vector3.zero;

            if (horseVFXObject != null)
                horseVFXObject.SetActive(false);
        }

        private void Start()
        {
            if (playOnStart)
                PlayTransition();
        }

        /// <summary>播放完整转场效果。</summary>
        public void PlayTransition()
        {
            StartCoroutine(TransitionCoroutine());
        }

        private IEnumerator TransitionCoroutine()
        {
            // ── 阶段 0：准备并开始马特效 ──
            if (horseVFXObject != null)
                horseVFXObject.SetActive(true);

            if (horseFrameAnimation != null)
            {
                // 序列帧方式：直接 Play，无延迟
                horseFrameAnimation.Play();
            }
            else if (horseVideoPlayer != null)
            {
                // VideoPlayer 方式：需要等待初始化和启动
                yield return null;
                yield return null;

                horseVideoPlayer.frame = 0;
                horseVideoPlayer.isLooping = false;
                horseVideoPlayer.Play();

                float waitTimer = 3f;
                while (!horseVideoPlayer.isPlaying && waitTimer > 0f)
                {
                    waitTimer -= Time.deltaTime;
                    yield return null;
                }
            }

            if (vfxLeadTime > 0f)
                yield return new WaitForSeconds(vfxLeadTime);

            // ── 阶段 1：Scale 从 0 到 1 ──
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float curveValue = scaleCurve.Evaluate(t);
                _rectTransform.localScale = _targetScale * curveValue;
                yield return null;
            }

            _rectTransform.localScale = _targetScale;

            // ── 阶段 2：马特效消散 ──
            if (vfxTrailTime > 0f)
                yield return new WaitForSeconds(vfxTrailTime);

            // ── 阶段 3：隐藏马特效 ──
            if (horseVFXObject != null)
                horseVFXObject.SetActive(false);

            if (horseFrameAnimation != null)
                horseFrameAnimation.Pause();

            if (horseVideoPlayer != null)
                horseVideoPlayer.Stop();
        }
    }
}

