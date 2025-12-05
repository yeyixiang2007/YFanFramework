using QFramework;
using DG.Tweening;
using YFan.Runtime.Attributes;
using YFan.Runtime.Utils;

namespace YFan.Runtime.Modules
{
    [AutoRegister(typeof(ITweenSystem))]
    public class TweenSystem : AbstractSystem, ITweenSystem
    {
        protected override void OnInit()
        {
            // DOTween 初始化
            // recycleAllByDefault: true (对象池复用)
            // useSafeMode: true (安全模式，防止报错)
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly)
                .SetCapacity(200, 50);

            DOTween.defaultAutoPlay = AutoPlay.All;
            DOTween.defaultEaseType = Ease.OutQuad;

            YLog.Info("DOTween Initialized", "TweenSystem");
        }

        public void SetDefaultEase(Ease ease)
        {
            DOTween.defaultEaseType = ease;
        }

        public void KillAll(bool complete = false)
        {
            DOTween.KillAll(complete);
            YLog.Info($"Kill All Tweens (Complete: {complete})", "TweenSystem");
        }

        public void PauseAll()
        {
            DOTween.PauseAll();
        }

        public void ResumeAll()
        {
            DOTween.PlayAll();
        }

        protected override void OnDeinit()
        {
            KillAll();
        }
    }
}
