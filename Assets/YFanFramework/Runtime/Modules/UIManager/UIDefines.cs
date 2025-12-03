using System;

namespace YFan.Runtime.Modules
{
    /// <summary>
    /// UI 层级定义 (渲染顺序从上到下)
    /// </summary>
    public enum UILayer
    {
        Bot,    // 底层 (背景、主地图)
        Mid,    // 中层 (普通窗口、背包、功能界面)
        Top,    // 顶层 (弹窗、提示框)
        System  // 系统层 (Loading、断线重连、Debug)
    }

    /// <summary>
    /// UI 传递参数 (可扩展)
    /// </summary>
    public class UIPanelData
    {
        // 可以添加 object[] Args 或具体字段
        // 例如: public object[] Args { get; set; }
        // 或: public int SomeInt { get; set; }
    }
}
