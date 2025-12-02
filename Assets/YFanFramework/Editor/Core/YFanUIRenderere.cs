using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using YFan.Attributes;

namespace YFan.Editor
{
    /// <summary>
    /// 通用 UI 渲染器
    /// 负责解析 YAttributes 并绘制 UI，支持 Inspector 和 EditorWindow
    /// </summary>
    public class YFanUIRenderer
    {
        private object _target; // 目标对象 (可能是 MonoBehaviour 或 EditorWindow)
        private SerializedObject _serializedObject; // 序列化对象

        // 缓存反射信息
        private List<MethodInfo> _buttonMethods = new List<MethodInfo>();
        private Dictionary<string, Func<object, bool>> _conditions = new Dictionary<string, Func<object, bool>>();

        // 分组状态
        private string _currentGroupName = null;
        private bool _isInsideGroup = false;

        #region 初始化

        public YFanUIRenderer(object target, SerializedObject serializedObject)
        {
            _target = target;
            _serializedObject = serializedObject;
            ScanMembers();
        }

        /// <summary>
        /// 扫描目标对象的成员（方法和字段）
        /// </summary>
        private void ScanMembers()
        {
            if (_target == null) return;
            var type = _target.GetType();

            // 扫描方法
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            _buttonMethods.Clear();
            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<YButtonAttribute>() != null) _buttonMethods.Add(method);
                CacheShowIf(type, method.Name, method.GetCustomAttribute<YShowIfAttribute>());
            }

            // 扫描字段
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                CacheShowIf(type, field.Name, field.GetCustomAttribute<YShowIfAttribute>());
            }
        }

        #endregion

        #region 绘制逻辑

        public void Draw()
        {
            if (_serializedObject == null || _target == null) return;

            _serializedObject.Update();
            SerializedProperty iterator = _serializedObject.GetIterator();
            bool enterChildren = true;

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

                // 反射获取字段信息
                var fieldInfo = GetFieldInfo(_target.GetType(), iterator.name);

                // 1. ShowIf
                if (!CheckShowIf(iterator.name)) continue;

                // 2. BoxGroup
                HandleBoxGroup(fieldInfo);

                // 3. 装饰 (Title, Space, HelpBox)
                if (fieldInfo != null) DrawDecorations(fieldInfo);

                // 4. 颜色与只读
                Color? originalColor = null;
                bool wasEnabled = GUI.enabled;
                if (fieldInfo != null)
                {
                    var colorAttr = fieldInfo.GetCustomAttribute<YColorAttribute>();
                    if (colorAttr != null) { originalColor = GUI.color; GUI.color = colorAttr.Color; }
                    if (fieldInfo.GetCustomAttribute<YReadOnlyAttribute>() != null) GUI.enabled = false;
                }

                // 5. 绘制属性 (Range 或 Default)
                DrawField(iterator, fieldInfo);

                // 恢复状态
                if (originalColor.HasValue) GUI.color = originalColor.Value;
                GUI.enabled = wasEnabled;
            }

            if (_isInsideGroup)
            {
                EditorGUILayout.EndVertical();
                _isInsideGroup = false;
            }
            _currentGroupName = null;

            _serializedObject.ApplyModifiedProperties();

            // 6. 绘制按钮
            DrawButtons();
        }

        /// <summary>
        /// 绘制字段的装饰（标题、间距、帮助框）
        /// </summary>
        /// <param name="field"></param>
        private void DrawDecorations(FieldInfo field)
        {
            var space = field.GetCustomAttribute<YSpaceAttribute>();
            if (space != null) GUILayout.Space(space.Height);

            var title = field.GetCustomAttribute<YTitleAttribute>();
            if (title != null)
            {
                if (space == null) GUILayout.Space(5);
                GUILayout.Label(title.Title, EditorStyles.boldLabel);
            }

            var helpBoxes = field.GetCustomAttributes<YHelpBoxAttribute>();
            foreach (var help in helpBoxes) EditorGUILayout.HelpBox(help.Message, (MessageType)help.Type);
        }

        /// <summary>
        /// 绘制字段的属性（Range 或 Default）
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="field"></param>
        private void DrawField(SerializedProperty prop, FieldInfo field)
        {
            if (field != null)
            {
                var range = field.GetCustomAttribute<YRangeAttribute>();
                if (range != null)
                {
                    if (prop.propertyType == SerializedPropertyType.Float)
                        EditorGUILayout.Slider(prop, range.Min, range.Max);
                    else if (prop.propertyType == SerializedPropertyType.Integer)
                        EditorGUILayout.IntSlider(prop, (int)range.Min, (int)range.Max);
                    else
                        EditorGUILayout.PropertyField(prop, true);
                    return;
                }
            }
            EditorGUILayout.PropertyField(prop, true);
        }

        /// <summary>
        /// 处理盒子分组（BoxGroup）
        /// </summary>
        /// <param name="member"></param>
        private void HandleBoxGroup(MemberInfo member)
        {
            string groupName = member?.GetCustomAttribute<YBoxGroupAttribute>()?.GroupName;

            if (groupName != _currentGroupName)
            {
                if (_isInsideGroup) { EditorGUILayout.EndVertical(); _isInsideGroup = false; }
                if (groupName != null)
                {
                    _isInsideGroup = true;
                    _currentGroupName = groupName;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label(groupName, EditorStyles.boldLabel);
                }
                else _currentGroupName = null;
            }
        }

        /// <summary>
        /// 绘制按钮（YButtonAttribute）
        /// </summary>
        private void DrawButtons()
        {
            if (_buttonMethods.Count == 0) return;
            if (_isInsideGroup) { EditorGUILayout.EndVertical(); _isInsideGroup = false; _currentGroupName = null; }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            foreach (var method in _buttonMethods)
            {
                if (!CheckShowIf(method.Name)) continue;

                HandleBoxGroup(method);

                var colorAttr = method.GetCustomAttribute<YColorAttribute>();
                Color oldColor = GUI.backgroundColor;
                if (colorAttr != null) GUI.backgroundColor = colorAttr.Color;

                var attr = method.GetCustomAttribute<YButtonAttribute>();
                string btnName = string.IsNullOrEmpty(attr.Name) ? ObjectNames.NicifyVariableName(method.Name) : attr.Name;

                if (GUILayout.Button(btnName, GUILayout.Height(attr.Height)))
                {
                    method.Invoke(_target, null);
                }
                GUI.backgroundColor = oldColor;
            }

            if (_isInsideGroup) { EditorGUILayout.EndVertical(); _isInsideGroup = false; }
        }

        /// <summary>
        /// 缓存 ShowIf 条件（YShowIfAttribute）
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberName"></param>
        /// <param name="attr"></param>
        private void CacheShowIf(Type type, string memberName, YShowIfAttribute attr)
        {
            if (attr == null || _conditions.ContainsKey(memberName)) return;
            var condField = type.GetField(attr.ConditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var condProp = type.GetProperty(attr.ConditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (condField != null && condField.FieldType == typeof(bool)) _conditions[memberName] = (obj) => (bool)condField.GetValue(obj);
            else if (condProp != null && condProp.PropertyType == typeof(bool)) _conditions[memberName] = (obj) => (bool)condProp.GetValue(obj, null);
        }

        /// <summary>
        /// 检查 ShowIf 条件是否满足
        /// </summary>
        /// <param name="memberName"></param>
        /// <returns></returns>
        private bool CheckShowIf(string memberName) => _conditions.TryGetValue(memberName, out var p) ? p(_target) : true;

        /// <summary>
        /// 获取字段信息（包含私有字段）
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private FieldInfo GetFieldInfo(Type type, string fieldName) => type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        #endregion
    }
}
