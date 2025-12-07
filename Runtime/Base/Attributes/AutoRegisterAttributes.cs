using System;

namespace YFan.Runtime.Attributes
{
    /// <summary>
    /// 自动注册特性
    /// 标记在 ISystem, IModel, IUtility 的实现类上
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AutoRegisterAttribute : Attribute
    {
        /// <summary>
        /// 注册的目标接口类型 (例如 IInputSystem)
        /// 如果不填，默认注册为类本身的类型 (不推荐，QFramework 建议基于接口交互)
        /// </summary>
        public Type InterfaceType { get; private set; }

        /// <summary>
        /// 当前模块依赖的模块类型列表
        /// 只有当所有依赖模块注册完成后，当前模块才会被注册
        /// </summary>
        public Type[] Dependencies { get; private set; }

        public AutoRegisterAttribute(Type interfaceType = null, params Type[] dependencies)
        {
            InterfaceType = interfaceType;
            Dependencies = dependencies ?? Type.EmptyTypes;
        }
    }
}
