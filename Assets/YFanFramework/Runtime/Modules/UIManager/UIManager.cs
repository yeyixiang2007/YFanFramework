using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using YFan.Attributes;
using YFan.Utils;

namespace YFan.Runtime.Modules
{
    [AutoRegister(typeof(IUIManager))]
    public class UIManager : AbstractSystem, IUIManager
    {
        #region 内部状态

        private GameObject _uiRoot; // UI 根节点 (Canvas 父节点)
        private Canvas _mainCanvas; // 主 Canvas 组件

        // 层级根节点容器
        private readonly Dictionary<UILayer, Transform> _layers = new Dictionary<UILayer, Transform>();

        // 已加载的面板缓存 [Key: ClassName, Value: Instance]
        private readonly Dictionary<string, BasePanel> _loadedPanels = new Dictionary<string, BasePanel>();

        // UI 栈 (用于 Push/Pop)
        private readonly Stack<BasePanel> _panelStack = new Stack<BasePanel>();

        // 资产加载工具
        private IAssetUtil _assetUtil => this.GetUtility<IAssetUtil>();

        #endregion

        protected override void OnInit()
        {
            InitUIRoot();
            YLog.Info("UIManager Initialized", "UIManager");
        }

        #region 初始化根节点

        private void InitUIRoot()
        {
            if (_uiRoot != null) return;

            // 创建 UI Root
            _uiRoot = new GameObject("UIRoot");
            UnityEngine.Object.DontDestroyOnLoad(_uiRoot);

            // 配置 Canvas
            _mainCanvas = _uiRoot.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mainCanvas.sortingOrder = 0;

            var scaler = _uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _uiRoot.AddComponent<GraphicRaycaster>();

            // 创建层级节点 (按枚举顺序创建，保证 Hierarchy 顺序即渲染顺序)
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var layerGo = new GameObject(layer.ToString());
                layerGo.transform.SetParent(_uiRoot.transform, false);

                // 铺满
                var rect = layerGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                // 只有 System 层需要极高的 Order，或者简单利用 Hierarchy 顺序 (Sibling Index)
                // 这里我们利用 Sibling Index，因为我们是按 Enum 顺序创建的

                _layers.Add(layer, layerGo.transform);
            }
        }

        #endregion

        #region 普通操作 (Open / Close)

        public async UniTask<T> Open<T>(UIPanelData data = null) where T : BasePanel
        {
            string panelName = typeof(T).Name;
            BasePanel panel = await GetOrCreatePanel<T>();

            if (panel == null) return null;

            // 确保显示在最前 (同一层级内)
            panel.transform.SetAsLastSibling();

            panel.Open(data);
            return panel as T;
        }

        public void Close<T>() where T : BasePanel
        {
            ClosePanel(typeof(T).Name);
        }

        public void ClosePanel(string panelName)
        {
            if (_loadedPanels.TryGetValue(panelName, out var panel))
            {
                // 如果该面板在栈中，这是非法操作，应该走 Pop
                // 但为了健壮性，这里仅仅是 Close 并不移除栈引用（可能会导致栈逻辑混乱，严谨项目需校验）
                if (_panelStack.Contains(panel))
                {
                    YLog.Warn($"尝试直接 Close 一个栈内面板 [{panelName}]，建议使用 Pop()", "UIManager");
                }

                panel.Close();
            }
        }

        public T GetPanel<T>() where T : BasePanel
        {
            string name = typeof(T).Name;
            if (_loadedPanels.TryGetValue(name, out var panel))
            {
                return panel as T;
            }
            return null;
        }

        #endregion

        #region 栈操作 (Push / Pop)

        public async UniTask<T> Push<T>(UIPanelData data = null) where T : BasePanel
        {
            // 暂停栈顶面板
            if (_panelStack.Count > 0)
            {
                var top = _panelStack.Peek();
                top.Hide();
            }

            // 加载并打开新面板
            BasePanel nextPanel = await GetOrCreatePanel<T>();
            if (nextPanel == null) return null;

            nextPanel.Open(data);

            // 入栈
            _panelStack.Push(nextPanel);

            return nextPanel as T;
        }

        public void Pop()
        {
            if (_panelStack.Count == 0) return;

            // 弹出并关闭当前
            var current = _panelStack.Pop();
            current.Close();

            // 恢复上一个
            if (_panelStack.Count > 0)
            {
                var prev = _panelStack.Peek();
                prev.Show(); // Resume
            }
        }

        public void ClearStack()
        {
            while (_panelStack.Count > 0)
            {
                var p = _panelStack.Pop();
                p.Close();
            }
        }

        #endregion

        #region 内部加载逻辑

        private async UniTask<BasePanel> GetOrCreatePanel<T>() where T : BasePanel
        {
            string panelName = typeof(T).Name;

            // 检查缓存
            if (_loadedPanels.TryGetValue(panelName, out var cachedPanel))
            {
                return cachedPanel;
            }

            // 异步加载 Prefab
            // 注意：这里 AssetKey 默认取类名，这要求 Prefab 名字和类名一致，且 Addressable Key 也是这个名字
            // 如果需要自定义 Key，需实例化一个 T 获取属性，比较麻烦，通常约定优于配置

            // 临时实例化一个 prefab，但 AssetUtil.InstantiateAsync 已经帮我们做了实例化
            // 我们需要知道 Key。由于 T 是泛型，我们无法直接访问 T.AssetKey 静态属性。
            // 约定：Prefab Addressable Name == 类名

            GameObject panelGo = await _assetUtil.InstantiateAsync(panelName);
            if (panelGo == null)
            {
                YLog.Error($"无法加载 UI 面板: {panelName}", "UIManager");
                return null;
            }

            // 获取组件
            T panel = panelGo.GetComponent<T>();
            if (panel == null)
            {
                YLog.Error($"Prefab [{panelName}] 上缺少脚本 [{typeof(T).Name}]", "UIManager");
                UnityEngine.Object.Destroy(panelGo);
                return null;
            }

            // 设置层级
            if (_layers.TryGetValue(panel.Layer, out var layerRoot))
            {
                panel.transform.SetParent(layerRoot, false);
            }
            else
            {
                panel.transform.SetParent(_layers[UILayer.Mid], false); // 默认放 Mid
            }

            // 初始化
            panel.Init();

            // 加入缓存
            _loadedPanels.Add(panelName, panel);

            return panel;
        }

        #endregion

        // 记得在 Dispose 时卸载 Addressables 引用（如果 AssetUtil 没有自动管理，这里可能需要手动清理）
        // 不过目前的 AssetUtil 是 LoadAsync 时计数，InstantiateAsync 内部也是 Load。
        // UIManager 这里没有 Release 的逻辑，意味着 UI 打开过一次就常驻内存。
        // 如果需要 Unload，需要增加 Unload<T>() 方法调用 AssetUtil.Release(Key) 并 Destroy(go)。
    }
}
