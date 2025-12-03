using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.InputSystem;
using YFan.Attributes;
using YFan.Base;
using YFan.Utils;

namespace YFan.Modules
{
    [AutoRegister(typeof(IInputSystem))]
    public class InputSystem : AbstractSystem, IInputSystem
    {
        #region 配置与字段

        private InputActionAsset _actionAsset; // 输入资产
        private InputActionMap _currentMap; // 当前输入映射
        private InputSettingsData _settingsData; // 输入设置数据
        private bool _isReady = false; // 是否初始化完成

        // 缓存 Action 查找
        private readonly Dictionary<string, InputAction> _actionCache = new Dictionary<string, InputAction>();

        // 依赖工具
        private IAssetUtil _assetUtil; // 资产工具

        public bool IsReady => _isReady; // 是否初始化完成

        #endregion

        protected override void OnInit()
        {
            _assetUtil = this.GetUtility<IAssetUtil>();

            // 异步初始化
            InitAsync().Forget();
        }

        private async UniTaskVoid InitAsync()
        {
            // 加载 InputAsset
            _actionAsset = await _assetUtil.LoadAsync<InputActionAsset>(ConfigKeys.InputAssetKey);
            if (_actionAsset == null)
            {
                YLog.Error($"无法加载 InputAsset: {ConfigKeys.InputAssetKey}", "InputSystem");
                return;
            }

            // 加载用户存档 (改键信息)
            LoadInputSettings();

            // 激活默认 Map
            SwitchActionMap(ConfigKeys.DefaultInputMapName);
            EnableInput(true);

            _isReady = true;
            YLog.Info("InputSystem 初始化完成", "InputSystem");
        }

        #region 存档与加载 (SaveUtil集成)

        private void LoadInputSettings()
        {
            // 使用 SaveUtil 读取配置
            _settingsData = SaveUtil.Load<InputSettingsData>(ConfigKeys.InputSettingSaveSlot);

            // 如果没有存档，创建默认
            if (_settingsData == null)
            {
                _settingsData = new InputSettingsData();
            }

            // 应用改键信息 (如果有)
            if (!string.IsNullOrEmpty(_settingsData.OverridesJson))
            {
                try
                {
                    _actionAsset.LoadBindingOverridesFromJson(_settingsData.OverridesJson);
                }
                catch (Exception e)
                {
                    YLog.Error($"应用按键配置失败: {e.Message}", "InputSystem");
                }
            }
        }

        public void SaveInputSettings()
        {
            if (_actionAsset == null) return;

            // 导出当前改键为 JSON
            _settingsData.OverridesJson = _actionAsset.SaveBindingOverridesAsJson();

            // 使用 SaveUtil 保存
            SaveUtil.Save(ConfigKeys.InputSettingSaveSlot, _settingsData, ConfigKeys.InputSettingSaveNote);

            YLog.Info("按键配置已保存", "InputSystem");
        }

        #endregion

        #region 基础控制

        public void EnableInput(bool enable)
        {
            if (_actionAsset == null) return;
            if (enable) _actionAsset.Enable();
            else _actionAsset.Disable();
        }

        public void SwitchActionMap(string mapName)
        {
            if (_actionAsset == null) return;

            var map = _actionAsset.FindActionMap(mapName);
            if (map != null)
            {
                if (_currentMap != null) _currentMap.Disable();
                _currentMap = map;
                _currentMap.Enable();
                _actionCache.Clear(); // 切换 Map 后缓存失效
            }
            else
            {
                YLog.Warn($"未找到输入映射: {mapName}", "InputSystem");
            }
        }

        private InputAction GetActionInternal(string name)
        {
            if (_currentMap == null) return null;
            if (_actionCache.TryGetValue(name, out var action)) return action;

            action = _currentMap.FindAction(name);
            if (action != null) _actionCache[name] = action;
            return action;
        }

        #endregion

        #region 轮询 API

        public Vector2 GetVector2(string actionName)
        {
            var action = GetActionInternal(actionName);
            return action != null ? action.ReadValue<Vector2>() : Vector2.zero;
        }

        public float GetFloat(string actionName)
        {
            var action = GetActionInternal(actionName);
            return action != null ? action.ReadValue<float>() : 0f;
        }

        public bool GetButton(string actionName)
        {
            var action = GetActionInternal(actionName);
            return action != null && action.IsPressed();
        }

        public bool GetButtonDown(string actionName)
        {
            var action = GetActionInternal(actionName);
            return action != null && action.WasPressedThisFrame();
        }

        public bool GetButtonUp(string actionName)
        {
            var action = GetActionInternal(actionName);
            return action != null && action.WasReleasedThisFrame();
        }

        #endregion

        #region 事件 API

        public void BindAction(string actionName, Action<InputAction.CallbackContext> onPerformed, Action<InputAction.CallbackContext> onCanceled = null)
        {
            var action = GetActionInternal(actionName);
            if (action == null) return;
            if (onPerformed != null) action.performed += onPerformed;
            if (onCanceled != null) action.canceled += onCanceled;
        }

        public void UnbindAction(string actionName, Action<InputAction.CallbackContext> onPerformed, Action<InputAction.CallbackContext> onCanceled = null)
        {
            var action = GetActionInternal(actionName);
            if (action == null) return;
            if (onPerformed != null) action.performed -= onPerformed;
            if (onCanceled != null) action.canceled -= onCanceled;
        }

        #endregion

        #region 改键 API (Rebinding)

        public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
        {
            var action = GetActionInternal(actionName);
            if (action == null) return "";

            // 获取显示名称 (自动处理手柄/键盘图标的文字描述)
            return action.GetBindingDisplayString(bindingIndex);
        }

        public void StartRebind(string actionName, int bindingIndex, Action<string> onComplete, Action onCancel)
        {
            var action = GetActionInternal(actionName);
            if (action == null)
            {
                onCancel?.Invoke();
                return;
            }

            // 改键前必须禁用 Action
            action.Disable();

            var rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                //.WithControlsExcluding("Mouse") // 可选：如果不允许绑定鼠标
                .OnMatchWaitForAnother(0.1f) // 防止连击
                .OnComplete(operation =>
                {
                    // 改键完成
                    action.Enable();
                    operation.Dispose(); // 必须释放内存

                    // 自动保存
                    SaveInputSettings();

                    // 回调新的按键名称
                    string newBindName = action.GetBindingDisplayString(bindingIndex);
                    onComplete?.Invoke(newBindName);

                    YLog.Info($"改键成功: {actionName} -> {newBindName}", "InputSystem");
                })
                .OnCancel(operation =>
                {
                    // 改键取消
                    action.Enable();
                    operation.Dispose();
                    onCancel?.Invoke();
                });

            rebindOperation.Start();
        }

        public void ResetAllBindings()
        {
            if (_actionAsset == null) return;

            // 移除所有覆盖
            _actionAsset.RemoveAllBindingOverrides();

            // 保存空配置
            SaveInputSettings();

            YLog.Info("已重置所有按键绑定", "InputSystem");
        }

        #endregion
    }
}
