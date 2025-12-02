using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using YFan.Attributes;

namespace YFan.Editor
{
    /// <summary>
    /// 基础属性绘制器，实现仿 Odin 编辑器的属性绘制功能
    /// * 提供通用的属性绘制功能
    /// * 支持自定义条件显示
    /// </summary>
    public class YFanInspector : UnityEditor.Editor
    {
        #region 字段与属性

        // --- 缓存字段 ---
        private List<MethodInfo> _buttonMethods = new List<MethodInfo>(); // 缓存按钮方法
        private Dictionary<string, Func<object, bool>> _conditions = new Dictionary<string, Func<object, bool>>(); // 缓存条件方法

        // --- 分组状态记录 ---
        private string _currentGroupName = null; // 当前分组名称
        private bool _isInsideGroup = false; // 是否在分组内

        #endregion

        #region 初始化与缓存

        protected virtual void OnEnable()
        {
            if (target == null) return;
            var type = target.GetType();

            // 1. 扫描 Method
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            _buttonMethods.Clear();
            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<YButtonAttribute>() != null) _buttonMethods.Add(method);
                CacheShowIf(type, method.Name, method.GetCustomAttribute<YShowIfAttribute>());
            }

            // 2. 扫描 Field
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                CacheShowIf(type, field.Name, field.GetCustomAttribute<YShowIfAttribute>());
            }
        }

        /// <summary>
        /// 缓存 ShowIf 条件，避免重复反射
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberName"></param>
        /// <param name="attr"></param>
        private void CacheShowIf(Type type, string memberName, YShowIfAttribute attr)
        {
            if (attr == null) return;
            if (_conditions.ContainsKey(memberName)) return;

            // 简单的反射缓存逻辑 (同之前)
            var condField = type.GetField(attr.ConditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var condProp = type.GetProperty(attr.ConditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (condField != null && condField.FieldType == typeof(bool))
                _conditions[memberName] = (obj) => (bool)condField.GetValue(obj);
            else if (condProp != null && condProp.PropertyType == typeof(bool))
                _conditions[memberName] = (obj) => (bool)condProp.GetValue(obj, null);
        }

        #endregion

        #region 绘制逻辑

        /// <summary>
        /// 绘制属性（重要方法）
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            // 重置分组状态
            _currentGroupName = null;
            _isInsideGroup = false;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true)) { EditorGUILayout.PropertyField(iterator); }
                    continue;
                }

                // 获取字段反射信息
                var fieldInfo = GetFieldInfo(target.GetType(), iterator.name);

                // --- 1. ShowIf 检查 ---
                if (!CheckShowIf(iterator.name)) continue;

                // --- 2. 处理 BoxGroup 分组逻辑 ---
                HandleBoxGroup(fieldInfo);

                // --- 3. 绘制装饰 (Space, Title, HelpBox) ---
                if (fieldInfo != null)
                {
                    // Space
                    var space = fieldInfo.GetCustomAttribute<YSpaceAttribute>();
                    if (space != null) GUILayout.Space(space.Height);

                    // Title
                    var title = fieldInfo.GetCustomAttribute<YTitleAttribute>();
                    if (title != null)
                    {
                        if (space == null) GUILayout.Space(5);
                        GUILayout.Label(title.Title, EditorStyles.boldLabel);
                    }

                    // HelpBox
                    var helpBoxes = fieldInfo.GetCustomAttributes<YHelpBoxAttribute>();
                    foreach (var help in helpBoxes)
                    {
                        EditorGUILayout.HelpBox(help.Message, (MessageType)help.Type);
                    }
                }

                // --- 4. 颜色与只读处理 ---
                Color? originalColor = null;
                bool wasEnabled = GUI.enabled;
                if (fieldInfo != null)
                {
                    var colorAttr = fieldInfo.GetCustomAttribute<YColorAttribute>();
                    if (colorAttr != null)
                    {
                        originalColor = GUI.color;
                        GUI.color = colorAttr.Color;
                    }

                    if (fieldInfo.GetCustomAttribute<YReadOnlyAttribute>() != null)
                    {
                        GUI.enabled = false;
                    }
                }

                // --- 5. 绘制属性本身 (支持 YRange) ---
                if (fieldInfo != null && fieldInfo.GetCustomAttribute<YRangeAttribute>() != null)
                {
                    var range = fieldInfo.GetCustomAttribute<YRangeAttribute>();
                    if (iterator.propertyType == SerializedPropertyType.Float)
                        EditorGUILayout.Slider(iterator, range.Min, range.Max);
                    else if (iterator.propertyType == SerializedPropertyType.Integer)
                        EditorGUILayout.IntSlider(iterator, (int)range.Min, (int)range.Max);
                    else
                        EditorGUILayout.PropertyField(iterator, true); // 不支持的类型回退
                }
                else
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }

                // --- 恢复状态 ---
                if (originalColor.HasValue) GUI.color = originalColor.Value;
                GUI.enabled = wasEnabled;
            }

            // 循环结束，如果有未关闭的 Group，关闭它
            if (_isInsideGroup)
            {
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();

            // --- 6. 绘制按钮 ---
            DrawButtons();
        }

        /// <summary>
        /// 处理 BoxGroup 分组逻辑
        /// * 支持自定义分组名称
        /// * 自动处理分组开启与关闭
        /// </summary>
        /// <param name="field"></param>
        private void HandleBoxGroup(FieldInfo field)
        {
            string groupName = null;
            if (field != null)
            {
                var attr = field.GetCustomAttribute<YBoxGroupAttribute>();
                if (attr != null) groupName = attr.GroupName;
            }

            // 状态机：
            // 1. 如果当前没在组里，但发现了新组 -> 开启新组
            // 2. 如果当前在组里，但新字段没有组 -> 关闭旧组
            // 3. 如果当前在组里，新字段是另一个组 -> 关闭旧组，开启新组
            // 4. 如果当前在组里，新字段是同一个组 -> 保持

            if (groupName != _currentGroupName)
            {
                // 如果之前有打开的组，先关闭
                if (_isInsideGroup)
                {
                    EditorGUILayout.EndVertical();
                    _isInsideGroup = false;
                }

                // 如果现在有新组，打开
                if (groupName != null)
                {
                    _isInsideGroup = true;
                    _currentGroupName = groupName;

                    // 绘制盒子样式
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    // 绘制组标题
                    GUILayout.Label(groupName, EditorStyles.boldLabel);
                }
                else
                {
                    _currentGroupName = null;
                }
            }
        }

        /// <summary>
        /// 绘制按钮
        /// * 支持自定义条件显示
        /// * 支持自定义颜色
        /// * 支持自定义名称
        /// </summary>
        private void DrawButtons()
        {
            if (_buttonMethods.Count > 0)
            {
                GUILayout.Space(10);
                DrawLine(Color.gray);
                GUILayout.Label("Actions", EditorStyles.boldLabel);

                foreach (var method in _buttonMethods)
                {
                    if (!CheckShowIf(method.Name)) continue;

                    // 支持按钮上的 HelpBox
                    var helpBoxes = method.GetCustomAttributes<YHelpBoxAttribute>();
                    foreach (var hb in helpBoxes) EditorGUILayout.HelpBox(hb.Message, (MessageType)hb.Type);

                    var attr = method.GetCustomAttribute<YButtonAttribute>();
                    var colorAttr = method.GetCustomAttribute<YColorAttribute>();

                    Color oldColor = GUI.backgroundColor;
                    if (colorAttr != null) GUI.backgroundColor = colorAttr.Color;

                    string btnName = string.IsNullOrEmpty(attr.Name) ? ObjectNames.NicifyVariableName(method.Name) : attr.Name;
                    if (GUILayout.Button(btnName, GUILayout.Height(attr.Height)))
                    {
                        method.Invoke(target, null);
                    }
                    GUI.backgroundColor = oldColor;
                }
            }
        }

        /// <summary>
        /// 检查 ShowIf 条件是否满足
        /// </summary>
        /// <param name="memberName"></param>
        /// <returns></returns>
        private bool CheckShowIf(string memberName)
        {
            if (_conditions.TryGetValue(memberName, out var predicate)) return predicate(target);
            return true;
        }

        /// <summary>
        /// 获取字段信息
        /// * 支持实例字段和公共/非公共字段
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// 绘制分隔线
        /// * 支持自定义颜色
        /// </summary>
        /// <param name="color"></param>
        private void DrawLine(Color color)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(2));
            r.height = 1;
            EditorGUI.DrawRect(r, color);
        }

        #endregion
    }
}
