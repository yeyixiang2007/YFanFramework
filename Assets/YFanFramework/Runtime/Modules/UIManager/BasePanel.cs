using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using YFan.Runtime.Base;
using YFan.Runtime.Base.Abstract;

namespace YFan.Runtime.Modules
{
    /// <summary>
    /// 基础面板类
    /// + 所有 UI 面板都应继承自该类
    /// + 面板的生命周期管理 (Init, Open, Close, Hide, Show)
    /// + 继承自 UIAbstractController，可获取架构，可自动绑定 UI 控件
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePanel : UIAbstractController
    {
        #region 配置

        /// <summary>
        /// 指定该面板所属层级
        /// </summary>
        public abstract UILayer Layer { get; }

        /// <summary>
        /// 指定 Addressable Key (默认使用类名)
        /// </summary>
        public virtual string AssetKey => this.GetType().Name;

        #endregion

        #region 内部状态

        private CanvasGroup _canvasGroup; // 缓存 CanvasGroup 组件
        public CanvasGroup CanvasGroup => _canvasGroup ? _canvasGroup : (_canvasGroup = GetComponent<CanvasGroup>());

        public bool IsVisible { get; private set; } // 是否可见 (是否调用了 Open/Push)
        public bool IsInited { get; private set; } // 是否初始化 (是否调用了 Init)

        #endregion

        #region 生命周期 (由 UIManager 调用)

        /// <summary>
        /// 初始化 (仅一次)
        /// </summary>
        public void Init()
        {
            if (IsInited) return;
            OnInit();
            IsInited = true;
        }

        /// <summary>
        /// 打开 (每次 Open/Push 时调用)
        /// </summary>
        public void Open(UIPanelData data = null)
        {
            IsVisible = true;
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;
            OnOpen(data);
        }

        /// <summary>
        /// 关闭 (Close/Pop 时调用)
        /// </summary>
        public void Close()
        {
            IsVisible = false;
            OnClose();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 隐藏 (被 Push 的新页面覆盖时调用)
        /// </summary>
        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            CanvasGroup.blocksRaycasts = false; // 阻挡点击但保持可见(可选)
            // 如果想完全不可见:
            // gameObject.SetActive(false);
            OnHide();
        }

        /// <summary>
        /// 恢复 (上层页面 Pop 后，当前页面恢复时调用)
        /// </summary>
        public void Show()
        {
            if (IsVisible) return;
            IsVisible = true;
            // gameObject.SetActive(true);
            CanvasGroup.blocksRaycasts = true;
            OnShow();
        }

        #endregion

        #region 子类实现接口

        protected virtual void OnInit() { }
        protected virtual void OnOpen(UIPanelData data) { }
        protected virtual void OnClose() { }

        // 栈操作专用
        protected virtual void OnHide() { }
        protected virtual void OnShow() { }

        #endregion

        /// <summary>
        /// 自身关闭的快捷方法
        /// </summary>
        protected void CloseSelf()
        {
            // 这里的 CloseSelf 需要调用 Manager，实际上可以通过 Architecture 获取 Manager
            // 或者直接使用 SendEvent，为了简单，这里假设 Manager 单例或通过 Controller 获取
            YFanApp.Interface.GetSystem<IUIManager>().ClosePanel(this.GetType().Name);
        }
    }
}
