using System;
using System.Linq;
using System.Reflection;
using QFramework;

namespace YFan.Utils
{
    /// <summary>
    /// 自动模块绑定器
    /// + 自动扫描带有 [AutoRegister] 的类，并注册到 Architecture 中
    /// </summary>
    public static class AutoModuleBinder
    {
        /// <summary>
        /// 执行自动注册
        /// </summary>
        /// <param name="architecture">架构实例 (this)</param>
        public static void ScanAndRegister(IArchitecture architecture)
        {
            // 1. 获取当前程序集 (通常是 Assembly-CSharp)
            // 优化：如果项目分了多个程序集，这里可能需要传入 Assembly 数组
            var assembly = typeof(AutoModuleBinder).Assembly;

            // 2. 筛选所有带有 [AutoRegister] 的具体类
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsDefined(typeof(Attributes.AutoRegisterAttribute), false));

            foreach (var type in types)
            {
                RegisterType(architecture, type);
            }
        }

        private static void RegisterType(IArchitecture architecture, Type concreteType)
        {
            // 获取 Attribute 信息
            var attr = concreteType.GetCustomAttribute<Attributes.AutoRegisterAttribute>();

            // 确定注册的 Key (接口类型)
            // 如果 Attribute 没填参数，默认尝试找它实现的第一个接口，或者就是它自己
            Type interfaceType = attr.InterfaceType;

            if (interfaceType == null)
            {
                // 自动推断策略：找第一个并非 QFramework 基础接口的接口
                // 这是一个简单的推断，建议显式指定接口类型
                interfaceType = concreteType.GetInterfaces()
                    .FirstOrDefault(i => i != typeof(ISystem) && i != typeof(IModel) && i != typeof(IUtility));

                if (interfaceType == null) interfaceType = concreteType;
            }

            try
            {
                // 创建实例
                object instance = Activator.CreateInstance(concreteType);

                // 根据类型分类调用不同的注册方法
                if (typeof(ISystem).IsAssignableFrom(concreteType))
                {
                    InvokeRegisterMethod(architecture, "RegisterSystem", interfaceType, instance);
                    YLog.Info($"自动注册 System: {concreteType.Name} -> {interfaceType.Name}", "AutoModuleBinder");
                }
                else if (typeof(IModel).IsAssignableFrom(concreteType))
                {
                    InvokeRegisterMethod(architecture, "RegisterModel", interfaceType, instance);
                    YLog.Info($"自动注册 Model: {concreteType.Name} -> {interfaceType.Name}", "AutoModuleBinder");
                }
                else if (typeof(IUtility).IsAssignableFrom(concreteType))
                {
                    InvokeRegisterMethod(architecture, "RegisterUtility", interfaceType, instance);
                    YLog.Info($"自动注册 Utility: {concreteType.Name} -> {interfaceType.Name}", "AutoModuleBinder");
                }
                else
                {
                    YLog.Warn($"自动注册 类型 {concreteType.Name} 未实现 ISystem/IModel/IUtility，跳过注册。", "AutoModuleBinder");
                }
            }
            catch (Exception e)
            {
                YLog.Error($"自动注册 类型 {concreteType.Name} 注册失败: {e.Message}", "AutoModuleBinder");
            }
        }

        /// <summary>
        /// 反射调用架构的泛型注册方法
        /// e.g. architecture.RegisterSystem<IInputSystem>(new InputSystem());
        /// </summary>
        private static void InvokeRegisterMethod(IArchitecture architecture, string methodName, Type interfaceType, object instance)
        {
            // 1. 获取方法元数据 (RegisterSystem<T>)
            MethodInfo methodInfo = architecture.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

            if (methodInfo != null)
            {
                // 2. 构造泛型方法
                MethodInfo genericMethod = methodInfo.MakeGenericMethod(interfaceType);

                // 3. 执行
                genericMethod.Invoke(architecture, new object[] { instance });
            }
            else
            {
                throw new Exception($"未在架构中找到方法 {methodName}");
            }
        }
    }
}
