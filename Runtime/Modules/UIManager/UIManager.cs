using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YFan.Runtime.Attributes;
using YFan.Runtime.Utils;

namespace YFan.Runtime.Modules
{
    [AutoRegister(typeof(IUIManager))]
    public class UIManager : AbstractSystem, IUIManager
    {
        #region 内部状态

        private GameObject _uiRoot;
        private Canvas _mainCanvas;

        private readonly Dictionary<UILayer, Transform> _layers = new Dictionary<UILayer, Transform>();
        private readonly Dictionary<string, BasePanel> _loadedPanels = new Dictionary<string, BasePanel>();
        private readonly Stack<BasePanel> _panelStack = new Stack<BasePanel>();

        // --- 遮罩管理 ---
        private GameObject _maskObj;
        private Button _maskBtn;
        private CanvasGroup _maskCG;

        // --- 焦点管理 ---
        private readonly Stack<GameObject> _focusHistory = new Stack<GameObject>();

        private IAssetUtil _assetUtil => this.GetUtility<IAssetUtil>();
        private IMonoUtil _monoUtil => this.GetUtility<IMonoUtil>(); // 用于监听 Update

        #endregion

        protected override void OnInit()
        {
            InitUIRoot();
            InitMask();

            // 注册返回键监听
            _monoUtil.AddUpdateListener(OnUpdate);

            YLog.Info("UIManager Initialized", "UIManager");
        }

        protected override void OnDeinit()
        {
            _monoUtil.RemoveUpdateListener(OnUpdate);
        }

        private void OnUpdate()
        {
            // 处理 Android 返回键或 PC ESC 键
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_panelStack.Count > 0)
                {
                    var topPanel = _panelStack.Peek();
                    // 仅当面板可见、允许返回键、且交互未被阻挡时
                    if (topPanel.IsVisible && topPanel.CanvasGroup.blocksRaycasts && topPanel.AllowSystemBack)
                    {
                        Pop();
                    }
                }
            }
        }

        #region 初始化

        private void InitUIRoot()
        {
            if (_uiRoot != null) return;

            _uiRoot = new GameObject("UIRoot");
            UnityEngine.Object.DontDestroyOnLoad(_uiRoot);

            _mainCanvas = _uiRoot.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mainCanvas.sortingOrder = 0;

            var scaler = _uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _uiRoot.AddComponent<GraphicRaycaster>();

            if (EventSystem.current == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.transform.SetParent(_uiRoot.transform);
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var layerGo = new GameObject(layer.ToString());
                layerGo.transform.SetParent(_uiRoot.transform, false);
                var rect = layerGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                // 可以在这里挂载 SafeArea 脚本

                _layers.Add(layer, layerGo.transform);
            }
        }

        private void InitMask()
        {
            _maskObj = new GameObject("UI_Blocker_Mask");
            _maskObj.transform.SetParent(_uiRoot.transform, false);

            var img = _maskObj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0f);

            _maskBtn = _maskObj.AddComponent<Button>();
            _maskBtn.transition = Selectable.Transition.None;
            _maskBtn.onClick.AddListener(OnMaskClick);

            _maskCG = _maskObj.AddComponent<CanvasGroup>();
            _maskCG.alpha = 0;
            _maskCG.blocksRaycasts = false;

            var rect = _maskObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion

        #region 普通操作

        public async UniTask<T> Open<T>(UIPanelData data = null) where T : BasePanel
        {
            string panelName = typeof(T).Name;
            BasePanel panel = await GetOrCreatePanel<T>();
            if (panel == null) return null;

            panel.transform.SetAsLastSibling();

            // 等待动画完成
            await panel.OpenAsync(data);

            return panel as T;
        }

        public void Close<T>() where T : BasePanel => ClosePanel(typeof(T).Name);

        public void ClosePanel(string panelName)
        {
            if (_loadedPanels.TryGetValue(panelName, out var panel))
            {
                if (_panelStack.Count > 0 && _panelStack.Peek() == panel)
                {
                    Pop();
                }
                else
                {
                    // 使用 FireAndForget 执行关闭动画
                    ClosePanelInternal(panel).Forget();
                }
            }
        }

        private async UniTaskVoid ClosePanelInternal(BasePanel panel)
        {
            await panel.CloseAsync();
            CheckDestroy(panel);
        }

        public T GetPanel<T>() where T : BasePanel
        {
            string name = typeof(T).Name;
            if (_loadedPanels.TryGetValue(name, out var panel)) return panel as T;
            return null;
        }

        #endregion

        #region 栈操作 (Push / Pop)

        public async UniTask<T> Push<T>(UIPanelData data = null) where T : BasePanel
        {
            RecordCurrentFocus();

            // 1. 暂停当前栈顶
            if (_panelStack.Count > 0)
            {
                var top = _panelStack.Peek();
                top.Pause(); // 使用 Pause 而不是 Hide，逻辑更准确
            }

            // 2. 加载新面板
            BasePanel nextPanel = await GetOrCreatePanel<T>();
            if (nextPanel == null) return null;

            _panelStack.Push(nextPanel);

            // 3. 处理遮罩
            nextPanel.transform.SetAsLastSibling();
            RefreshMaskState(nextPanel);

            // 4. 异步打开
            await nextPanel.OpenAsync(data);

            // 5. 设置焦点
            SetPanelFocus(nextPanel);

            return nextPanel as T;
        }

        public void Pop()
        {
            if (_panelStack.Count == 0) return;

            // 弹出逻辑异步化处理
            PopInternal().Forget();
        }

        private async UniTaskVoid PopInternal()
        {
            // 1. 关闭当前栈顶
            var current = _panelStack.Pop();

            // 等待关闭动画
            await current.CloseAsync();

            // 检查是否销毁
            CheckDestroy(current);

            // 2. 恢复上一个面板
            BasePanel prev = null;
            if (_panelStack.Count > 0)
            {
                prev = _panelStack.Peek();
                prev.Show(); // 先确保 Visible = true
                prev.Resume(); // 再恢复交互
            }

            // 3. 刷新遮罩
            RefreshMaskState(prev);

            // 4. 恢复焦点
            RestorePreviousFocus();
        }

        public void ClearStack()
        {
            while (_panelStack.Count > 0)
            {
                var p = _panelStack.Pop();
                p.CloseAsync().Forget();
                CheckDestroy(p);
            }
            _focusHistory.Clear();
            RefreshMaskState(null);
            EventSystem.current.SetSelectedGameObject(null);
        }

        #endregion

        #region 资源管理与销毁

        /// <summary>
        /// 检查面板的缓存策略，决定是否销毁
        /// </summary>
        private void CheckDestroy(BasePanel panel)
        {
            if (panel.CachePolicy == UICachePolicy.DestroyOnClose)
            {
                string name = panel.GetType().Name;
                if (_loadedPanels.ContainsKey(name))
                {
                    _loadedPanels.Remove(name);

                    // 释放 Addressable 引用
                    _assetUtil.Release(panel.AssetKey);

                    UnityEngine.Object.Destroy(panel.gameObject);
                }
            }
        }

        #endregion

        #region 辅助逻辑

        private void RefreshMaskState(BasePanel activePanel)
        {
            if (activePanel != null && activePanel.UseMask)
            {
                _maskObj.SetActive(true);
                _maskCG.alpha = 1;
                _maskCG.blocksRaycasts = true;
                _maskObj.transform.SetParent(activePanel.transform.parent, false);
                int index = activePanel.transform.GetSiblingIndex();
                _maskObj.transform.SetSiblingIndex(Mathf.Max(0, index - 1));
            }
            else
            {
                _maskCG.alpha = 0;
                _maskCG.blocksRaycasts = false;
                _maskObj.SetActive(false);
                _maskObj.transform.SetParent(_uiRoot.transform, false);
            }
        }

        private void OnMaskClick()
        {
            if (_panelStack.Count > 0)
            {
                var top = _panelStack.Peek();
                if (top.UseMask && top.CloseOnMaskClick)
                {
                    Pop();
                }
            }
        }

        private void RecordCurrentFocus()
        {
            if (EventSystem.current != null)
            {
                _focusHistory.Push(EventSystem.current.currentSelectedGameObject);
            }
            else
            {
                _focusHistory.Push(null);
            }
        }

        private void RestorePreviousFocus()
        {
            if (_focusHistory.Count > 0)
            {
                var lastFocus = _focusHistory.Pop();
                if (lastFocus != null && lastFocus.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(lastFocus);
                }
            }
        }

        private void SetPanelFocus(BasePanel panel)
        {
            if (panel.DefaultFocus != null)
            {
                UniTask.Create(async () =>
                {
                    await UniTask.Yield();
                    if (panel.IsVisible && panel.DefaultFocus != null)
                    {
                        EventSystem.current.SetSelectedGameObject(panel.DefaultFocus);
                    }
                });
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        #endregion

        #region 内部加载逻辑

        private async UniTask<BasePanel> GetOrCreatePanel<T>() where T : BasePanel
        {
            string panelName = typeof(T).Name;
            if (_loadedPanels.TryGetValue(panelName, out var cachedPanel)) return cachedPanel;

            // TODO:为了获取配置 (AssetKey)，我们可能需要先反射一下 T，或者实例化后再读取
            // 这里为了简单，假设 T 上挂了 Attribute，可以直接读取 Key
            // 如果 AssetKey 是动态的，这里需要调整逻辑
            var attr = typeof(T).GetCustomAttribute<UIConfigAttribute>();
            string assetKey = attr?.AssetKey ?? panelName;
            UILayer targetLayer = attr?.Layer ?? UILayer.Mid;

            GameObject panelGo = await _assetUtil.InstantiateAsync(assetKey);
            if (panelGo == null)
            {
                YLog.Error($"无法加载 UI 面板: {assetKey}", "UIManager");
                return null;
            }

            T panel = panelGo.GetComponent<T>();
            if (panel == null)
            {
                YLog.Error($"Prefab [{assetKey}] 上缺少脚本 [{typeof(T).Name}]", "UIManager");
                UnityEngine.Object.Destroy(panelGo);
                return null;
            }

            if (_layers.TryGetValue(targetLayer, out var layerRoot))
            {
                panel.transform.SetParent(layerRoot, false);
            }
            else
            {
                panel.transform.SetParent(_layers[UILayer.Mid], false);
            }

            panel.Init();
            _loadedPanels.Add(panelName, panel);
            return panel;
        }

        #endregion
    }
}
