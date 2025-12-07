using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QFramework;

namespace YFan.Runtime.Utils
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
                if (typeof(IUtility).IsAssignableFrom(type)) return 1; // Utility
                if (typeof(IModel).IsAssignableFrom(type)) return 2;   // Model
                if (typeof(ISystem).IsAssignableFrom(type)) return 3;  // System
                return 99;
            }
            types.Sort((a, b) => GetPriority(a).CompareTo(GetPriority(b)));

            // 已注册的模块集合
            var registeredModules = new HashSet<Type>();
            // 等待注册的模块缓冲区
            var waitingModules = new List<Type>();

            foreach (var type in types)
            {
                // 检查当前模块是否可以注册
                if (CanRegisterType(type, registeredModules))
                {
                    RegisterType(architecture, type);
                    registeredModules.Add(type);

                    // 检查缓冲区中是否有模块可以注册
                    ProcessWaitingModules(architecture, waitingModules, registeredModules);
                }
                else
                {
                    // 加入缓冲区
                    waitingModules.Add(type);
                    YLog.Info($"模块 {type.Name} 依赖未满足，加入注册缓冲区", "AutoModuleBinder");
                }
            }

            // 检查是否有未注册的模块
            if (waitingModules.Count > 0)
            {
                YLog.Warn($"以下模块因依赖未满足未能注册: {string.Join(", ", waitingModules.Select(t => t.Name))}", "AutoModuleBinder");
            }
        }

        /// <summary>
        /// 检查模块是否可以注册（依赖是否都已满足）
        /// </summary>
        /// <param name="type">要检查的模块类型</param>
        /// <param name="registeredModules">已注册的模块集合</param>
        /// <returns>是否可以注册</returns>
        private static bool CanRegisterType(Type type, HashSet<Type> registeredModules)
        {
            var attr = type.GetCustomAttribute<Attributes.AutoRegisterAttribute>();
            if (attr.Dependencies.Length == 0) return true;

            // 检查所有依赖是否都已注册
            return attr.Dependencies.All(dependency => registeredModules.Contains(dependency));
        }

        /// <summary>
        /// 处理等待注册的模块
        /// </summary>
        /// <param name="architecture">架构实例</param>
        /// <param name="waitingModules">等待注册的模块列表</param>
        /// <param name="registeredModules">已注册的模块集合</param>
        private static void ProcessWaitingModules(IArchitecture architecture, List<Type> waitingModules, HashSet<Type> registeredModules)
        {
            for (int i = waitingModules.Count - 1; i >= 0; i--)
            {
                var type = waitingModules[i];
                if (CanRegisterType(type, registeredModules))
                {
                    RegisterType(architecture, type);
                    registeredModules.Add(type);
                    waitingModules.RemoveAt(i);

                    // 递归检查是否有其他模块可以注册
                    ProcessWaitingModules(architecture, waitingModules, registeredModules);
                }
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
