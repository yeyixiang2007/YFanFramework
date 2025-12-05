using QFramework;
using DG.Tweening;

namespace YFan.Runtime.Modules
{
    /// <summary>
    /// 缓动系统接口
    /// </summary>
    public interface ITweenSystem : ISystem
    {
        /// <summary>
        /// 设置全局默认缓动曲线
        /// </summary>
        void SetDefaultEase(Ease ease);

        /// <summary>
        /// 杀死所有 Tween (通常在切换场景时调用)
        /// </summary>
        void KillAll(bool complete = false);

        /// <summary>
        /// 暂停所有
        /// </summary>
        void PauseAll();

        /// <summary>
        /// 恢复所有
        /// </summary>
        void ResumeAll();
    }
}
