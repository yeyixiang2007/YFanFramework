using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YFan.Runtime.Base;
using YFan.Runtime.Base.Abstract;
using YFan.Runtime.Utils;

namespace YFan.Runtime.Modules
{
    /// <summary>
    /// 基础面板类 (非泛型基类)
    /// + 负责生命周期管理、特性配置读取
    /// + 增加异步动画支持、暂停/恢复支持
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePanel : AbstractController
    {
        #region 配置 (优先读取 Attribute)

        private UIConfigAttribute _config;
        private UIConfigAttribute Config => _config ??= this.GetType().GetCustomAttribute<UIConfigAttribute>();

        public virtual UILayer Layer => Config?.Layer ?? UILayer.Mid;
        public virtual string AssetKey => Config?.AssetKey ?? this.GetType().Name;
        public virtual bool UseMask => Config?.UseMask ?? false;
        public virtual bool CloseOnMaskClick => Config?.CloseOnMaskClick ?? false;
        public virtual UICachePolicy CachePolicy => Config?.CachePolicy ?? UICachePolicy.Cache;

        /// <summary>
        /// 是否应用刘海屏安全区域适配
        /// </summary>
        public virtual bool ApplySafeArea => true;

        /// <summary>
        /// 是否允许系统返回键 (Android/PC Esc) 关闭此面板
        /// </summary>
        public virtual bool AllowSystemBack => true;

        /// <summary>
        /// 打开面板时默认选中的 UI 元素
        /// </summary>
        [SerializeField]
        private GameObject _defaultFocus;
        public GameObject DefaultFocus => _defaultFocus;

        #endregion

        #region 内部状态

        private CanvasGroup _canvasGroup;
        public CanvasGroup CanvasGroup => _canvasGroup ? _canvasGroup : (_canvasGroup = GetComponent<CanvasGroup>());

        public bool IsVisible { get; private set; }
        public bool IsInited { get; private set; }

        private RectTransform _rectTransform;
        public RectTransform RectTransform => _rectTransform ? _rectTransform : (_rectTransform = GetComponent<RectTransform>());

        #endregion

        #region 生命周期 (由 UIManager 调用)

        public void Init()
        {
            if (IsInited) return;

            // 安全区域适配
            if (ApplySafeArea)
            {
                ApplySafeAreaOffset();
            }

            BindUI();
            OnInit();
            IsInited = true;
        }

        /// <summary>
        /// 异步打开面板
        /// </summary>
        public async UniTask OpenAsync(UIPanelData data = null)
        {
            IsVisible = true;
            gameObject.SetActive(true);

            // 重置状态
            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;

            // 执行自定义打开逻辑 (动画)
            await OnOpenAsync(data);
        }

        /// <summary>
        /// 异步关闭面板
        /// </summary>
        public async UniTask CloseAsync()
        {
            CanvasGroup.blocksRaycasts = false;

            // 执行自定义关闭逻辑 (动画)
            await OnCloseAsync();

            IsVisible = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 暂停 (被压栈时调用)
        /// </summary>
        public void Pause()
        {
            if (!IsVisible) return;
            CanvasGroup.blocksRaycasts = false;
            OnPause();
        }

        /// <summary>
        /// 恢复 (重新成为栈顶时调用)
        /// </summary>
        public void Resume()
        {
            if (IsVisible) return; // 只有非 Visible 状态才需要 Resume
                                   // 注意：Resume 时不一定完全显示，取决于具体的实现，通常这里只是恢复交互
                                   // 但在栈式 UI 中，Pop 后上一个页面通常需要重新变为可见
            IsVisible = true;
            CanvasGroup.blocksRaycasts = true;
            OnResume();
        }

        /// <summary>
        /// 仅隐藏 (不走完整关闭流程)
        /// </summary>
        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.alpha = 0;
            OnHide();
        }

        /// <summary>
        /// 仅显示
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

        // 核心生命周期改为异步，支持动画等待
        protected virtual async UniTask OnOpenAsync(UIPanelData data) { await UniTask.CompletedTask; }
        protected virtual async UniTask OnCloseAsync() { await UniTask.CompletedTask; }

        protected virtual void OnPause() { }
        protected virtual void OnResume() { }
        protected virtual void OnHide() { }
        protected virtual void OnShow() { }

        #endregion

        #region 辅助功能

        protected void CloseSelf()
        {
            YFanApp.Interface.GetSystem<IUIManager>().ClosePanel(this.GetType().Name);
        }

        private void ApplySafeAreaOffset()
        {
            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // 检查是否有 SafeArea 组件或手动调整 Rect
            // 这里简单直接修改 RectTransform 的 Anchor
            // 注意：这假设 Panel 是全屏拉伸的
            RectTransform.anchorMin = anchorMin;
            RectTransform.anchorMax = anchorMax;
        }

        #endregion

        /// <summary>
        /// 自动绑定 UI 元素
        /// </summary>
        protected void BindUI() => AutoUIBinder.Bind(this, transform);
    }

    /// <summary>
    /// 泛型基础面板类 (推荐使用)
    /// + 提供强类型的参数传递
    /// </summary>
    /// <typeparam name="TData">参数类型</typeparam>
    public abstract class BasePanel<TData> : BasePanel where TData : UIPanelData
    {
        protected override async UniTask OnOpenAsync(UIPanelData data)
        {
            if (data is TData tData)
            {
                await OnOpen(tData);
            }
            else
            {
                // 如果没有传参或类型不对，传默认值
                await OnOpen(default(TData));
            }
        }

        protected abstract UniTask OnOpen(TData data);
    }
}
