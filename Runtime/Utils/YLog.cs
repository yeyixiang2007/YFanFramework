using System;

namespace YFan.Runtime.Utils
{
    /// <summary>
    /// LogUtil 的静态封装，提供全局访问入口
    /// 使用前建议在架构入口处调用 YLog.Init()
    /// </summary>
    public static class YLog
    {
        private static ILogUtil _logUtil;

        /// <summary>
        /// 初始化全局日志系统
        /// </summary>
        public static void Init(ILogUtil logUtil)
        {
            if (_logUtil == null)
            {
                _logUtil = logUtil;

                // 默认配置
                _logUtil.EnableTimestamp(true);
                _logUtil.EnableSaveToFile(false); // 默认不开启存档

                // 这里可以根据宏定义自动开关反射，发布版关闭以提升性能
#if UNITY_EDITOR
                _logUtil.EnableReflection(true);
#else
                _logUtil.EnableReflection(false);
#endif

                Info("YLog 系统初始化完成", "LogUtil");
            }
        }

        /// <summary>
        /// 释放资源 (通常在游戏退出时调用)
        /// </summary>
        public static void Dispose()
        {
            if (_logUtil is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _logUtil = null;
        }

        #region 静态 API 转发

        public static void Info(string msg, params string[] modules)
            => _logUtil?.Info(msg, modules);

        public static void Warn(string msg, params string[] modules)
            => _logUtil?.Warn(msg, modules);

        public static void Error(string msg, params string[] modules)
            => _logUtil?.Error(msg, modules);

        public static void Exception(Exception e, params string[] modules)
            => _logUtil?.Exception(e, modules);

        #endregion

        #region 高级功能暴露

        /// <summary>
        /// 监听日志事件 (主要用于 YFanConsole)
        /// </summary>
        public static event Action<LogData> OnLogReceived
        {
            add { if (_logUtil != null) _logUtil.OnLogReceived += value; }
            remove { if (_logUtil != null) _logUtil.OnLogReceived -= value; }
        }

        #endregion
    }
}
