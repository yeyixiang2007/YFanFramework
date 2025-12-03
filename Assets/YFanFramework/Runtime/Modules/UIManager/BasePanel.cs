using UnityEngine;
using YFan.Runtime.Base;
using YFan.Runtime.Base.Abstract;

namespace YFan.Runtime.Modules
{
    /// <summary>
    /// 基础面板类
    /// + 所有 UI 面板都应继承自该类
    /// + 面板的生命周期管理 (Init, Open, Close, Hide, Show)
    /// + 增强：支持遮罩配置、支持焦点管理
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

        /// <summary>
        /// 是否使用背景遮罩 (Blocker)
        /// 如果为 true，UIManager 会在面板下方显示黑色半透明遮罩
        /// </summary>
        public virtual bool UseMask => false;

        /// <summary>
        /// 点击遮罩是否关闭当前面板 (仅当 UseMask = true 时有效)
        /// </summary>
        public virtual bool CloseOnMaskClick => false;

        /// <summary>
        /// 打开面板时默认选中的 UI 元素 (用于手柄/键盘导航)
        /// 如果为空，则不自动设置焦点
        /// </summary>
        [SerializeField]
        private GameObject _defaultFocus;
        public GameObject DefaultFocus => _defaultFocus;

        #endregion

        #region 内部状态

        private CanvasGroup _canvasGroup; // 缓存 CanvasGroup 组件
        public CanvasGroup CanvasGroup => _canvasGroup ? _canvasGroup : (_canvasGroup = GetComponent<CanvasGroup>());

        public bool IsVisible { get; private set; } // 是否可见
        public bool IsInited { get; private set; } // 是否初始化

        #endregion

        #region 生命周期 (由 UIManager 调用)

        /// <summary>
        /// 初始化面板
        /// + 调用 OnInit 进行自定义初始化
        /// + 设置 IsInited 为 true
        /// </summary>
        public void Init()
        {
            if (IsInited) return;
            OnInit();
            IsInited = true;
        }

        /// <summary>
        /// 打开面板
        /// + 设置 IsVisible 为 true
        /// + 激活 GameObject
        /// + 设置 CanvasGroup 为可见 (alpha = 1, blocksRaycasts = true)
        /// + 调用 OnOpen 进行自定义打开逻辑
        /// </summary>
        /// <param name="data">打开面板时传递的数据</param>
        public void Open(UIPanelData data = null)
        {
            IsVisible = true;
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;
            OnOpen(data);
        }

        /// <summary>
        /// 关闭面板
        /// + 设置 IsVisible 为 false
        /// + 调用 OnClose 进行自定义关闭逻辑
        /// + 停用 GameObject
        /// </summary>
        public void Close()
        {
            IsVisible = false;
            OnClose();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 隐藏面板
        /// + 设置 IsVisible 为 false
        /// + 调用 OnHide 进行自定义隐藏逻辑
        /// + 设置 CanvasGroup 为不可见 (alpha = 0, blocksRaycasts = false)
        /// </summary>
        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            CanvasGroup.blocksRaycasts = false;
            // 栈式管理中，Hide 时通常只禁交互，或者完全隐藏，视需求而定
            // 这里选择设为不可见以优化 DrawCall
            CanvasGroup.alpha = 0;
            OnHide();
        }

        /// <summary>
        /// 显示面板
        /// + 设置 IsVisible 为 true
        /// + 调用 OnShow 进行自定义显示逻辑
        /// + 设置 CanvasGroup 为可见 (alpha = 1, blocksRaycasts = true)
        /// </summary>
        public void Show()
        {
            if (IsVisible) return;
            IsVisible = true;
            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;
            OnShow();
        }

        #endregion

        #region 子类实现接口

        protected virtual void OnInit() { }
        protected virtual void OnOpen(UIPanelData data) { }
        protected virtual void OnClose() { }
        protected virtual void OnHide() { }
        protected virtual void OnShow() { }

        #endregion

        /// <summary>
        /// 自身关闭的快捷方法
        /// </summary>
        protected void CloseSelf()
        {
            YFanApp.Interface.GetSystem<IUIManager>().ClosePanel(this.GetType().Name);
        }
    }
}
