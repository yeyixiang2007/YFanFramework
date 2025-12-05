using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace YFan.Runtime.Modules
{
    /// <summary>
    /// Tween 扩展工具类
    /// 基于 DOTween + UniTask 封装，提供 async/await 支持
    /// </summary>
    public static class TweenExtensions
    {
        #region Transform (Move / Rotate / Scale)

        /// <summary>
        /// 移动 (World Position)
        /// </summary>
        public static async UniTask MoveAsync(this Transform target, Vector3 endValue, float duration, Ease ease = Ease.OutQuad, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOMove(endValue, duration)
                .SetEase(ease)
                .SetLink(target.gameObject) // 绑定生命周期
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 移动 (Local Position)
        /// </summary>
        public static async UniTask LocalMoveAsync(this Transform target, Vector3 endValue, float duration, Ease ease = Ease.OutQuad, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOLocalMove(endValue, duration)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 旋转 (Euler Angles)
        /// </summary>
        public static async UniTask RotateAsync(this Transform target, Vector3 endValue, float duration, Ease ease = Ease.OutQuad, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DORotate(endValue, duration, RotateMode.FastBeyond360)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 缩放
        /// </summary>
        public static async UniTask ScaleAsync(this Transform target, Vector3 endValue, float duration, Ease ease = Ease.OutBack, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOScale(endValue, duration)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 缩放 (统一倍率)
        /// </summary>
        public static async UniTask ScaleAsync(this Transform target, float endValue, float duration, Ease ease = Ease.OutBack, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOScale(Vector3.one * endValue, duration)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        #endregion

        #region RectTransform (UI Move)

        /// <summary>
        /// UI 锚点移动 (AnchoredPosition)
        /// </summary>
        public static async UniTask AnchorMoveAsync(this RectTransform target, Vector2 endValue, float duration, Ease ease = Ease.OutQuad, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOAnchorPos(endValue, duration)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// UI 震动效果 (Shake)
        /// </summary>
        public static async UniTask ShakeAsync(this RectTransform target, float duration, float strength = 10f, int vibrato = 10, float randomness = 90f, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOShakeAnchorPos(duration, strength, vibrato, randomness)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        #endregion

        #region Color & Alpha (Visuals)

        /// <summary>
        /// CanvasGroup 淡入淡出
        /// </summary>
        public static async UniTask FadeAsync(this CanvasGroup target, float endAlpha, float duration, Ease ease = Ease.Linear, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOFade(endAlpha, duration)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Graphic (Image/Text/RawImage) 颜色变化
        /// </summary>
        public static async UniTask ColorAsync(this Graphic target, Color endColor, float duration, Ease ease = Ease.Linear, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOColor(endColor, duration)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Graphic (Image/Text) 淡入淡出 (只改 Alpha)
        /// </summary>
        public static async UniTask FadeAsync(this Graphic target, float endAlpha, float duration, Ease ease = Ease.Linear, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOFade(endAlpha, duration)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// SpriteRenderer 颜色变化
        /// </summary>
        public static async UniTask ColorAsync(this SpriteRenderer target, Color endColor, float duration, Ease ease = Ease.Linear, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOColor(endColor, duration)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// SpriteRenderer 淡入淡出
        /// </summary>
        public static async UniTask FadeAsync(this SpriteRenderer target, float endAlpha, float duration, Ease ease = Ease.Linear, CancellationToken cancellationToken = default)
        {
            if (target == null) return;
            await target.DOFade(endAlpha, duration)
                .SetEase(ease)
                .SetLink(target.gameObject)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        #endregion

        #region Sequence Wrapper

        /// <summary>
        /// 便捷执行一个 Sequence 并等待完成
        /// </summary>
        public static async UniTask PlaySequenceAsync(Sequence sequence, CancellationToken cancellationToken = default)
        {
            if (sequence == null) return;
            await sequence.ToUniTask(cancellationToken: cancellationToken);
        }

        #endregion
    }
}
