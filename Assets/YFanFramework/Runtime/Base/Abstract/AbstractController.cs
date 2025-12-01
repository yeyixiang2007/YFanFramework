using QFramework;
using UnityEngine;

namespace YFan.Runtime.Base.Abstract
{
    /// <summary>
    /// 抽象控制器
    /// 所有控制器都需要继承该类
    /// </summary>
    public abstract class AbstractController : MonoBehaviour, IController
    {
        /// <summary>
        /// 获取架构实例
        /// </summary>
        /// <returns>架构实例</returns>
        public IArchitecture GetArchitecture() => YFanApp.Interface;
    }

















}
