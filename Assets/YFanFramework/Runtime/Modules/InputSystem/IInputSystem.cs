using System;
using UnityEngine;
using UnityEngine.InputSystem;
using QFramework;

namespace YFan.Modules
{
    public interface IInputSystem : ISystem
    {
        bool IsReady { get; }

        // --- 基础控制 ---

        /// <summary>
        /// 启用或禁用输入系统
        /// </summary>
        /// <param name="enable"></param>
        void EnableInput(bool enable);

        /// <summary>
        /// 切换当前操作映射表
        /// </summary>
        /// <param name="mapName"></param>
        void SwitchActionMap(string mapName);

        // --- 轮询 API (Update中使用) ---

        /// <summary>
        /// 获取 Vector2 类型的输入值 (如鼠标移动)
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        Vector2 GetVector2(string actionName);

        /// <summary>
        /// 获取 Float 类型的输入值 (如轴输入)
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        float GetFloat(string actionName);

        /// <summary>
        /// 获取 Boolean 类型的输入值 (如按钮输入)
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        bool GetButton(string actionName);

        /// <summary>
        /// 获取 Boolean 类型的输入值 (如按钮按下)
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        bool GetButtonDown(string actionName);

        /// <summary>
        /// 获取 Boolean 类型的输入值 (如按钮抬起)
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        bool GetButtonUp(string actionName);

        // --- 事件 API ---

        /// <summary>
        /// 绑定 Action 事件
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="onPerformed"></param>
        /// <param name="onCanceled"></param>
        void BindAction(string actionName, Action<InputAction.CallbackContext> onPerformed, Action<InputAction.CallbackContext> onCanceled = null);

        /// <summary>
        /// 解绑 Action 事件
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="onPerformed"></param>
        /// <param name="onCanceled"></param>
        void UnbindAction(string actionName, Action<InputAction.CallbackContext> onPerformed, Action<InputAction.CallbackContext> onCanceled = null);

        // --- 改键/设置 API ---

        /// <summary>
        /// 开始改键操作
        /// </summary>
        /// <param name="actionName">Action名称 (如 "Jump")</param>
        /// <param name="bindingIndex">绑定索引 (通常 0，如果有键盘/手柄多套绑定需指定)</param>
        /// <param name="onComplete">改键成功回调 (返回新的显示名称，如 "Space")</param>
        /// <param name="onCancel">取消回调</param>
        void StartRebind(string actionName, int bindingIndex, Action<string> onComplete, Action onCancel);

        /// <summary>
        /// 获取某个 Action 当前绑定的显示名称 (用于 UI 显示，如 "W", "Space")
        /// </summary>
        string GetBindingDisplayString(string actionName, int bindingIndex = 0);

        /// <summary>
        /// 重置所有按键到默认配置
        /// </summary>
        void ResetAllBindings();

        /// <summary>
        /// 保存当前改键设置
        /// </summary>
        void SaveInputSettings();
    }
}
