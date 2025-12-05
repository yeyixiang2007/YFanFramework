using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using YFan.Runtime.Attributes;

namespace YFan.Runtime.Utils
{
    /// <summary>
    /// 自动绑定 UI 组件和事件
    /// + 支持 [UIBind] 字段绑定
    /// + 支持 [BindEvent] 方法绑定
    /// </summary>
    public static class AutoUIBinder
    {
        /// <summary>
        /// 自动绑定目标对象的所有 [UIBind] 字段和 [BindEvent] 方法
        /// </summary>
        /// <param name="target">通常传入 this</param>
        /// <param name="root">UI 根节点 (通常传入 transform)</param>
        public static void Bind(object target, Transform root)
        {
            // 预先建立 名字->节点 的索引缓存 (性能优化关键)
            // 这样后续的查找都是 O(1) 或 O(logN)，避免反复递归 GetComponentsInChildren
            var nodeMap = MapAllChildren(root);

            Type type = target.GetType();

            // 处理字段绑定 ([UIBind])
            BindFields(target, root, type, nodeMap);

            // 处理方法绑定 ([BindClick] 等)
            BindMethods(target, root, type, nodeMap);
        }

        #region 字段绑定

        /// <summary>
        /// 处理字段绑定 ([UIBind])
        /// </summary>
        /// <param name="target"></param>
        /// <param name="root"></param>
        /// <param name="type"></param>
        /// <param name="map"></param>
        private static void BindFields(object target, Transform root, Type type, Dictionary<string, Transform> map)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<UIBindAttribute>();
                if (attr == null) continue;

                // 确定查找路径：优先用 Attribute 里的参数，没有则用字段名
                string path = string.IsNullOrEmpty(attr.Path) ? field.Name : attr.Path;

                // 查找节点
                Transform node = FindNode(root, map, path);
                if (node == null)
                {
                    YLog.Error($"[UIBind] 未找到节点: {path} (字段: {field.Name})", "UIAutoBinder");
                    continue;
                }

                // 赋值
                try
                {
                    if (field.FieldType == typeof(GameObject))
                    {
                        field.SetValue(target, node.gameObject);
                    }
                    else if (field.FieldType == typeof(Transform) || field.FieldType == typeof(RectTransform))
                    {
                        field.SetValue(target, node);
                    }
                    else
                    {
                        // 自动 GetComponent
                        var component = node.GetComponent(field.FieldType);
                        if (component != null)
                        {
                            field.SetValue(target, component);
                        }
                        else
                        {
                            YLog.Error($"[UIBind] 节点 {node.name} 上缺少组件 {field.FieldType.Name}", "UIAutoBinder");
                        }
                    }
                }
                catch (Exception e)
                {
                    YLog.Error($"[UIBind] 字段赋值失败 {field.Name}: {e.Message}", "UIAutoBinder");
                }
            }
        }

        #endregion

        #region 方法绑定

        /// <summary>
        /// 处理方法绑定 ([BindEvent])
        /// </summary>
        /// <param name="target"></param>
        /// <param name="root"></param>
        /// <param name="type"></param>
        /// <param name="map"></param>
        private static void BindMethods(object target, Transform root, Type type, Dictionary<string, Transform> map)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(UIBindEventAttribute), true);
                foreach (UIBindEventAttribute attr in attributes)
                {
                    Transform node = FindNode(root, map, attr.ComponentName);
                    if (node == null)
                    {
                        YLog.Error($"[BindEvent] 未找到节点: {attr.ComponentName} (方法: {method.Name})", "UIAutoBinder");
                        continue;
                    }

                    try
                    {
                        if (attr is BindClickAttribute) BindButton(target, method, node);
                        else if (attr is BindToggleAttribute) BindToggle(target, method, node);
                        else if (attr is BindFloatAttribute) BindFloat(target, method, node);
                        else if (attr is BindInputAttribute inputAttr) BindInput(target, method, node, inputAttr.EventType);
                    }
                    catch (Exception ex)
                    {
                        YLog.Error($"[BindEvent] 绑定事件失败 {method.Name}: {ex.Message}", "UIAutoBinder");
                    }
                }
            }
        }

        /// <summary>
        /// 绑定按钮点击事件
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="node"></param>
        private static void BindButton(object target, MethodInfo method, Transform node)
        {
            var btn = node.GetComponent<Button>();
            if (btn == null) { YLog.Error($"节点 {node.name} 无 Button 组件", "UIAutoBinder"); return; }

            // 封装 UnityAction 以支持 Invoke
            btn.onClick.AddListener(() => method.Invoke(target, null));
        }

        /// <summary>
        /// 绑定切换按钮事件
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="node"></param>
        private static void BindToggle(object target, MethodInfo method, Transform node)
        {
            var toggle = node.GetComponent<Toggle>();
            if (toggle == null) { YLog.Error($"节点 {node.name} 无 Toggle 组件", "UIAutoBinder"); return; }
            toggle.onValueChanged.AddListener((val) => method.Invoke(target, new object[] { val }));
        }

        /// <summary>
        /// 绑定滑动条事件
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="node"></param>
        private static void BindFloat(object target, MethodInfo method, Transform node)
        {
            var slider = node.GetComponent<Slider>();
            if (slider != null)
            {
                slider.onValueChanged.AddListener((val) => method.Invoke(target, new object[] { val }));
                return;
            }
            // 支持 Scrollbar
            var scrollbar = node.GetComponent<Scrollbar>();
            if (scrollbar != null)
            {
                scrollbar.onValueChanged.AddListener((val) => method.Invoke(target, new object[] { val }));
                return;
            }
            YLog.Error($"节点 {node.name} 无 Slider/Scrollbar 组件", "UIAutoBinder");
        }

        /// <summary>
        /// 绑定输入框事件
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="node"></param>
        /// <param name="evtType"></param>
        private static void BindInput(object target, MethodInfo method, Transform node, BindInputAttribute.InputEvent evtType)
        {
            var input = node.GetComponent<InputField>();
            // 兼容 TMP: var input = node.GetComponent<TMPro.TMP_InputField>();

            if (input == null) { YLog.Error($"节点 {node.name} 无 InputField 组件", "UIAutoBinder"); return; }

            if (evtType == BindInputAttribute.InputEvent.OnValueChanged)
                input.onValueChanged.AddListener((val) => method.Invoke(target, new object[] { val }));
            else
                input.onEndEdit.AddListener((val) => method.Invoke(target, new object[] { val }));
        }

        #endregion

        #region 辅助工具

        /// <summary>
        /// 建立全子节点索引 Map
        /// </summary>
        private static Dictionary<string, Transform> MapAllChildren(Transform root)
        {
            var map = new Dictionary<string, Transform>();
            // includeInactive = true 确保隐藏的物体也能被找到
            var allTrans = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTrans)
            {
                // 如果有重名，优先保留层级较浅的（遍历顺序通常是按层级），或者直接覆盖
                if (!map.ContainsKey(t.name))
                {
                    map.Add(t.name, t);
                }
            }
            return map;
        }

        /// <summary>
        /// 智能查找：支持 路径 和 名字
        /// </summary>
        private static Transform FindNode(Transform root, Dictionary<string, Transform> map, string nameOrPath)
        {
            // 如果包含 '/'，说明是路径，必须用 Transform.Find 精确查找
            if (nameOrPath.Contains("/")) return root.Find(nameOrPath);

            // 如果是简单名字，查字典 (极速)
            if (map.TryGetValue(nameOrPath, out Transform node)) return node;

            return null;
        }

        #endregion
    }
}
