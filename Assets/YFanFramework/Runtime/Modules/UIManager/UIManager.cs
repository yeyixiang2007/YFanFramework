using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YFan.Attributes;
using YFan.Utils;

namespace YFan.Runtime.Modules
{
    [AutoRegister(typeof(IUIManager))]
    public class UIManager : AbstractSystem, IUIManager
    {
        #region 内部状态

        private GameObject _uiRoot;
        private Canvas _mainCanvas;

        // 层级根节点容器
        private readonly Dictionary<UILayer, Transform> _layers = new Dictionary<UILayer, Transform>();
        // 已加载的面板缓存
        private readonly Dictionary<string, BasePanel> _loadedPanels = new Dictionary<string, BasePanel>();
        // UI 栈
        private readonly Stack<BasePanel> _panelStack = new Stack<BasePanel>();

        // --- 遮罩管理 ---
        private GameObject _maskObj; // 遮罩物体
        private Button _maskBtn;     // 遮罩按钮 (处理点击关闭)
        private CanvasGroup _maskCG; // 控制遮罩显隐

        // --- 焦点管理 ---
        // 记录每次 Push 前的焦点物体，Pop 时还原
        private readonly Stack<GameObject> _focusHistory = new Stack<GameObject>();

        private IAssetUtil _assetUtil => this.GetUtility<IAssetUtil>();

        #endregion

        protected override void OnInit()
        {
            InitUIRoot();
            InitMask();
            YLog.Info("UIManager Initialized", "UIManager");
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

            // 确保 EventSystem 存在 (焦点管理必须)
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

                _layers.Add(layer, layerGo.transform);
            }
        }

        private void InitMask()
        {
            // 创建一个全局通用的遮罩
            _maskObj = new GameObject("UI_Blocker_Mask");
            // 初始隐藏，不挂在任何层级下，动态调整
            _maskObj.transform.SetParent(_uiRoot.transform, false);

            var img = _maskObj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.75f); // 黑色半透明

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
            panel.Open(data);

            // 普通 Open 暂不处理复杂的栈式焦点和遮罩逻辑
            // 如果希望普通 Open 也支持，需统一逻辑。这里建议 Open 用于非模态，Push 用于模态。

            return panel as T;
        }

        public void Close<T>() where T : BasePanel => ClosePanel(typeof(T).Name);

        public void ClosePanel(string panelName)
        {
            if (_loadedPanels.TryGetValue(panelName, out var panel))
            {
                // 如果是栈顶面板，走 Pop 流程以保证焦点和遮罩正确
                if (_panelStack.Count > 0 && _panelStack.Peek() == panel)
                {
                    Pop();
                }
                else
                {
                    panel.Close();
                    // 如果面板在栈中间（非法操作），这里暂不做移除，防止破坏栈结构
                }
            }
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
            // 1. 记录当前焦点
            RecordCurrentFocus();

            // 2. 暂停当前栈顶面板
            if (_panelStack.Count > 0)
            {
                var top = _panelStack.Peek();
                top.Hide(); // 暂停/隐藏旧面板
            }

            // 3. 加载新面板
            BasePanel nextPanel = await GetOrCreatePanel<T>();
            if (nextPanel == null) return null;

            _panelStack.Push(nextPanel);

            // 4. 处理遮罩 (关键步骤)
            // 将面板移到其 Layer 的最前方
            nextPanel.transform.SetAsLastSibling();
            RefreshMaskState(nextPanel);

            // 5. 打开面板
            nextPanel.Open(data);

            // 6. 设置新焦点
            SetPanelFocus(nextPanel);

            return nextPanel as T;
        }

        public void Pop()
        {
            if (_panelStack.Count == 0) return;

            // 1. 关闭当前栈顶
            var current = _panelStack.Pop();
            current.Close();

            // 2. 恢复上一个面板
            BasePanel prev = null;
            if (_panelStack.Count > 0)
            {
                prev = _panelStack.Peek();
                prev.Show(); // Resume
            }

            // 3. 刷新遮罩 (如果上一个面板需要遮罩，遮罩移到它下面；如果不需要或栈空，隐藏遮罩)
            RefreshMaskState(prev);

            // 4. 恢复焦点
            RestorePreviousFocus();
        }

        public void ClearStack()
        {
            while (_panelStack.Count > 0)
            {
                var p = _panelStack.Pop();
                p.Close();
            }
            _focusHistory.Clear();
            RefreshMaskState(null);
            EventSystem.current.SetSelectedGameObject(null);
        }

        #endregion

        #region 辅助逻辑 (Focus & Mask)

        private void RefreshMaskState(BasePanel activePanel)
        {
            if (activePanel != null && activePanel.UseMask)
            {
                // 启用遮罩
                _maskObj.SetActive(true);
                _maskCG.alpha = 1;
                _maskCG.blocksRaycasts = true;

                // 将遮罩移动到 activePanel 的同一个父节点下
                _maskObj.transform.SetParent(activePanel.transform.parent, false);

                // 设置顺序：activePanel 的索引 - 1
                int index = activePanel.transform.GetSiblingIndex();
                _maskObj.transform.SetSiblingIndex(Mathf.Max(0, index - 1));

                // 确保 activePanel 在遮罩之上 (防止 index 计算误差)
                // activePanel.transform.SetAsLastSibling(); // 不需要，因为上面刚 Set 过了，这里只要 Mask 足够靠后即可
            }
            else
            {
                // 隐藏遮罩
                _maskCG.alpha = 0;
                _maskCG.blocksRaycasts = false;
                _maskObj.SetActive(false);
                // 移回 Root 防止干扰层级
                _maskObj.transform.SetParent(_uiRoot.transform, false);
            }
        }

        private void OnMaskClick()
        {
            // 只有栈顶面板允许点击遮罩关闭
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
                // 延迟一帧设置，确保 UI 已经 Active 且 Layout 重建完成
                UniTask.Create(async () =>
                {
                    await UniTask.Yield(); // 等待一帧
                    if (panel.IsVisible && panel.DefaultFocus != null)
                    {
                        EventSystem.current.SetSelectedGameObject(panel.DefaultFocus);
                    }
                });
            }
            else
            {
                // 如果没有指定焦点，为了防止手柄失控，最好清除选中
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        #endregion

        #region 内部加载逻辑

        private async UniTask<BasePanel> GetOrCreatePanel<T>() where T : BasePanel
        {
            string panelName = typeof(T).Name;
            if (_loadedPanels.TryGetValue(panelName, out var cachedPanel)) return cachedPanel;

            GameObject panelGo = await _assetUtil.InstantiateAsync(panelName);
            if (panelGo == null)
            {
                YLog.Error($"无法加载 UI 面板: {panelName}", "UIManager");
                return null;
            }

            T panel = panelGo.GetComponent<T>();
            if (panel == null)
            {
                YLog.Error($"Prefab [{panelName}] 上缺少脚本 [{typeof(T).Name}]", "UIManager");
                UnityEngine.Object.Destroy(panelGo);
                return null;
            }

            if (_layers.TryGetValue(panel.Layer, out var layerRoot))
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
