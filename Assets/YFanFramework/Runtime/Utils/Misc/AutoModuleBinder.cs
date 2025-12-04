using System;
using System.Linq;
using System.Reflection;
using QFramework;

namespace YFan.Utils
{
    /// <summary>
    /// 自动模块绑定器
    /// 扫描并注册所有实现了 ISystem、IModel、IUtility 接口的类
    /// </summary>
    public static class AutoModuleBinder
    {
        /// <summary>
        /// 扫描并注册所有实现了 ISystem、IModel、IUtility 接口的类
        /// </summary>
        /// <param name="architecture"></param>
        public static void ScanAndRegister(IArchitecture architecture)
        {
            Assembly gameAssembly;
            try
            {
                gameAssembly = Assembly.Load("Assembly-CSharp");
            }
            catch
            {
                // 如果是在 Editor 模式下某些特殊情况，或者项目改名了，可能找不到，回退到当前程序集
                gameAssembly = typeof(AutoModuleBinder).Assembly;
            }

            Assembly frameworkAssembly = typeof(AutoModuleBinder).Assembly;

            RegisterAssembly(architecture, frameworkAssembly);
            if (gameAssembly != frameworkAssembly && gameAssembly != null)
            {
                YLog.Info($"架构程序集扫描完成，开始扫描并注册游戏程序集 {gameAssembly.FullName}", "AutoModuleBinder");
                RegisterAssembly(architecture, gameAssembly);
            }
        }

        /// <summary>
        /// 注册程序集中的所有类型
        /// </summary>
        /// <param name="architecture"></param>
        /// <param name="assembly"></param>
        private static void RegisterAssembly(IArchitecture architecture, Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsDefined(typeof(Attributes.AutoRegisterAttribute), false))
                .ToList();

            int GetPriority(Type type)
            {
                if (typeof(IModel).IsAssignableFrom(type)) return 1;   // Model
                if (typeof(ISystem).IsAssignableFrom(type)) return 2;  // System
                if (typeof(IUtility).IsAssignableFrom(type)) return 3; // Utility
                return 99;
            }
            types.Sort((a, b) => GetPriority(a).CompareTo(GetPriority(b)));

            foreach (var type in types)
            {
                RegisterType(architecture, type);
            }
        }

        /// <summary>
        /// 注册单个类型
        /// </summary>
        /// <param name="architecture"></param>
        /// <param name="concreteType"></param>
        private static void RegisterType(IArchitecture architecture, Type concreteType)
        {
            var attr = concreteType.GetCustomAttribute<Attributes.AutoRegisterAttribute>();
            Type interfaceType = attr.InterfaceType;

            if (interfaceType == null)
            {
                interfaceType = concreteType.GetInterfaces()
                    .FirstOrDefault(i => i != typeof(ISystem) && i != typeof(IModel) && i != typeof(IUtility));
                if (interfaceType == null) interfaceType = concreteType;
            }

            try
            {
                object instance = Activator.CreateInstance(concreteType);

                if (typeof(ISystem).IsAssignableFrom(concreteType))
                {
                    InvokeRegisterMethod(architecture, "RegisterSystem", interfaceType, instance);
                    YLog.Info($"自动注册 System: {concreteType.Name}", "AutoModuleBinder");
                }
                else if (typeof(IModel).IsAssignableFrom(concreteType))
                {
                    InvokeRegisterMethod(architecture, "RegisterModel", interfaceType, instance);
                    YLog.Info($"自动注册 Model: {concreteType.Name}", "AutoModuleBinder");
                }
                else if (typeof(IUtility).IsAssignableFrom(concreteType))
                {
                    InvokeRegisterMethod(architecture, "RegisterUtility", interfaceType, instance);
                    YLog.Info($"自动注册 Utility: {concreteType.Name}", "AutoModuleBinder");
                }
            }
            catch (Exception e)
            {
                YLog.Error($"自动注册失败 {concreteType.Name}: {e.Message}", "AutoModuleBinder");
            }
        }

        /// <summary>
        /// 调用架构的注册方法
        /// </summary>
        /// <param name="architecture"></param>
        /// <param name="methodName"></param>
        /// <param name="interfaceType"></param>
        /// <param name="instance"></param>
        private static void InvokeRegisterMethod(IArchitecture architecture, string methodName, Type interfaceType, object instance)
        {
            MethodInfo methodInfo = architecture.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (methodInfo != null)
            {
                MethodInfo genericMethod = methodInfo.MakeGenericMethod(interfaceType);
                genericMethod.Invoke(architecture, new object[] { instance });
            }
        }
    }
}
