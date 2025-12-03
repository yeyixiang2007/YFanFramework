using System;

namespace YFan.Attributes
{
    #region 字段绑定 (组件/物体)

    /// <summary>
    /// 自动查找并赋值 UI 组件
    /// 示例: [UIBind] private Text Txt_Title;
    /// 示例: [UIBind("Top/Btn_Close")] private Button _btnClose;
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class UIBindAttribute : Attribute
    {
        public string Path { get; private set; }
        public UIBindAttribute(string path = null) => Path = path;
    }

    #endregion
    #region 方法绑定 (事件)

    /// <summary>
    /// 自动绑定 UI 事件
    /// 示例: [BindClick("Btn_Close")] private void OnBtnCloseClick() { }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public abstract class UIBindEventAttribute : Attribute
    {
        public string ComponentName { get; private set; }
        public UIBindEventAttribute(string componentName) => ComponentName = componentName;
    }

    #endregion

    #region 绑定实现

    /// <summary>
    /// 绑定 Button 的 onClick
    /// </summary>
    public class BindClickAttribute : UIBindEventAttribute
    {
        public BindClickAttribute(string btnName) : base(btnName) { }
    }

    /// <summary>
    /// 绑定 Toggle 的 onValueChanged
    /// </summary>
    public class BindToggleAttribute : UIBindEventAttribute
    {
        public BindToggleAttribute(string toggleName) : base(toggleName) { }
    }

    /// <summary>
    /// 绑定 Slider/Scrollbar 的 onValueChanged
    /// </summary>
    public class BindFloatAttribute : UIBindEventAttribute
    {
        public BindFloatAttribute(string sliderName) : base(sliderName) { }
    }

    /// <summary>
    /// 绑定 InputField 的 onValueChanged 或 onEndEdit
    /// </summary>
    public class BindInputAttribute : UIBindEventAttribute
    {
        public enum InputEvent { OnValueChanged, OnEndEdit }
        public InputEvent EventType { get; private set; }

        public BindInputAttribute(string inputName, InputEvent eventType = InputEvent.OnValueChanged)
            : base(inputName)
        {
            EventType = eventType;
        }
    }

    #endregion
}
