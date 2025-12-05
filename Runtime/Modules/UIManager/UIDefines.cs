using System;

namespace YFan.Runtime.Modules
{
    /// <summary>
    /// UI 层级定义 (渲染顺序从上到下)
    /// </summary>
    public enum UILayer
    {
        Bot, // 底层 (背景、主地图)
        Mid, // 中层 (普通窗口、背包、功能界面)
        Top, // 顶层 (弹窗、提示框)
        System // 系统层 (Loading、断线重连、Debug)
    }

    /// <summary>
    /// UI 缓存策略
    /// </summary>
    public enum UICachePolicy
    {
        Cache,          // 常驻内存 (主界面、背包)
        DestroyOnClose  // 关闭即销毁 (结算界面、临时弹窗)
    }

    /// <summary>
    /// UI 传递参数 (可扩展)
    /// </summary>
    public class UIPanelData
    {
    }

    /// <summary>
    /// UI 配置特性 (替代重写属性，简化配置)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class UIConfigAttribute : Attribute
    {
        public UILayer Layer { get; private set; }
        public bool UseMask { get; private set; }
        public bool CloseOnMaskClick { get; private set; }
        public UICachePolicy CachePolicy { get; private set; }
        public string AssetKey { get; private set; }

        public UIConfigAttribute(
            UILayer layer = UILayer.Mid,
            bool useMask = false,
            bool closeOnMaskClick = false,
            UICachePolicy cachePolicy = UICachePolicy.Cache,
            string assetKey = null)
        {
            Layer = layer;
            UseMask = useMask;
            CloseOnMaskClick = closeOnMaskClick;
            CachePolicy = cachePolicy;
            AssetKey = assetKey;
        }
    }
}
