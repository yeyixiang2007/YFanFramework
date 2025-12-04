using Cysharp.Threading.Tasks;
using QFramework;

namespace YFan.Runtime.Modules
{
    /// <summary>
    /// UI 管理器接口
    /// + 负责管理 UI 面板的加载、显示、隐藏、关闭等操作
    /// + 提供栈式管理面板 (Push/Pop)
    /// + 支持面板的缓存和重复利用
    /// </summary>
    public interface IUIManager : ISystem
    {
        #region 打开关闭面板

        /// <summary>
        /// 打开面板 (普通模式)
        /// 如果面板已在栈中，不会改变栈结构，仅显示
        /// </summary>
        UniTask<T> Open<T>(UIPanelData data = null) where T : BasePanel;

        /// <summary>
        /// 关闭面板
        /// </summary>
        void Close<T>() where T : BasePanel;
        void ClosePanel(string panelName); // 供 BasePanel 内部使用

        /// <summary>
        /// 获取已加载的面板
        /// </summary>
        T GetPanel<T>() where T : BasePanel;

        #endregion

        #region 栈操作

        /// <summary>
        /// 压入堆栈 (当前页面 Hide，新页面 Open)
        /// </summary>
        UniTask<T> Push<T>(UIPanelData data = null) where T : BasePanel;

        /// <summary>
        /// 弹出堆栈 (当前页面 Close，上一个页面 Show)
        /// </summary>
        void Pop();

        /// <summary>
        /// 清空堆栈 (只保留栈底或全清)
        /// </summary>
        void ClearStack();

        #endregion
    }
}
