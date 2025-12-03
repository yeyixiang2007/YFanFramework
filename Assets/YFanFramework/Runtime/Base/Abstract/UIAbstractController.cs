using YFan.Utils;

namespace YFan.Runtime.Base.Abstract
{
    /// <summary>
    /// 抽象 UI 控制器
    /// 所有 UI 控制器都需要继承该类
    /// </summary>
    public abstract class UIAbstractController : AbstractController
    {
        void Awake()
        {
            BindUI();
            OnAwake();
        }

        /// <summary>
        /// 自动绑定 UI 组件和事件
        /// </summary>
        protected void BindUI() => AutoUIBinder.Bind(this, transform);

        /// <summary>
        /// 初始化 (Awake 时调用)
        /// </summary>
        protected virtual void OnAwake() { }
    }
}
