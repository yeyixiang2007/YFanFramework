using System;
using UnityEngine;

namespace YFan.Attributes
{
    // --- UI 装饰类 ---

    /// <summary>
    /// 标题：在字段上方显示一个粗体标题
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public class YTitleAttribute : PropertyAttribute
    {
        public string Title;
        public YTitleAttribute(string title) { Title = title; }
    }

    /// <summary>
    /// 颜色：改变 GUI 颜色
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class YColorAttribute : PropertyAttribute
    {
        public Color Color;
        public YColorAttribute(float r, float g, float b) { Color = new Color(r, g, b); }
        public YColorAttribute(string htmlColor) { ColorUtility.TryParseHtmlString(htmlColor, out Color); }
    }

    /// <summary>
    /// 只读：在 Inspector 中显示但不可修改
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class YReadOnlyAttribute : PropertyAttribute { }

    // --- 逻辑控制类 ---

    /// <summary>
    /// 条件显示：只有当 conditionMemberName 为 true 时才显示
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class YShowIfAttribute : PropertyAttribute
    {
        public string ConditionName;
        public YShowIfAttribute(string conditionName) { ConditionName = conditionName; }
    }

    // --- 功能交互类 ---

    /// <summary>
    /// 按钮：在 Inspector 中显示一个按钮，点击执行函数
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class YButtonAttribute : Attribute
    {
        public string Name;
        public float Height;

        public YButtonAttribute(string name = null, float height = 30)
        {
            Name = name;
            Height = height;
        }
    }

    // --- 数值限制类 ---

    /// <summary>
    /// 范围限制：显示为滑动条 (支持 int 和 float)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class YRangeAttribute : PropertyAttribute
    {
        public float Min;
        public float Max;
        public YRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    // --- 布局与装饰类 ---

    /// <summary>
    /// 盒子分组：将连续的字段包裹在一个方框内
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class YBoxGroupAttribute : PropertyAttribute
    {
        public string GroupName;
        public YBoxGroupAttribute(string groupName) { GroupName = groupName; }
    }

    /// <summary>
    /// 提示框：显示 Info, Warning 或 Error 信息
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public class YHelpBoxAttribute : PropertyAttribute
    {
        public string Message;
        public YMessageType Type;

        public YHelpBoxAttribute(string message, YMessageType type = YMessageType.Info)
        {
            Message = message;
            Type = type;
        }
    }

    /// <summary>
    /// 间距：增加垂直像素间距
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class YSpaceAttribute : PropertyAttribute
    {
        public float Height;
        public YSpaceAttribute(float height = 10) { Height = height; }
    }

    // --- 为了配合 Editor 识别 MessageType ---
    public enum YMessageType
    {
        None, Info, Warning, Error
    }
}
