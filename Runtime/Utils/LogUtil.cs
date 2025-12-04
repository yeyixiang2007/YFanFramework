// TODO: 多线程安全性
// TODO: 文件写入性能优化

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using QFramework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace YFan.Utils
{
    /// <summary>
    /// 日志等级枚举
    /// </summary>
    public enum LogLevel
    {
        Info, // 信息日志
        Warn, // 警告日志
        Error, // 错误日志
        Exception // 异常日志
    }

    /// <summary>
    /// 日志数据结构，用于传递给控制台或文件系统
    /// </summary>
    public struct LogData
    {
        public LogLevel Level; // 日志等级
        public string Message; // 去除富文本后的纯净消息
        public string StackTrace; // 异常栈跟踪信息
        public DateTime Time; // 日志记录时间
        public string[] Modules; // 模块标签数组
        public string ModuleColorHex; // 模块颜色的十六进制表示
    }

    /// <summary>
    /// 日志工具
    /// * 提供基础的日志记录功能
    /// * 支持模块标签、颜色自定义
    /// * 可配置是否显示时间戳、全局开关、异常栈跟踪等
    /// * 可将日志保存到文件
    /// </summary>
    public interface ILogUtil : IUtility
    {
        // --- 基础日志功能 ---
        void Info(string message, params string[] modules);
        void Warn(string message, params string[] modules);
        void Error(string message, params string[] modules);
        void Exception(Exception exception, params string[] modules);

        // --- 配置开关 ---
        void EnableTimestamp(bool enable);
        void EnableGlobal(bool enable);
        void EnableReflection(bool enable);
        void EnableSaveToFile(bool enable);

        // --- 事件系统 (用于同步到 YFanConsole) ---
        event Action<LogData> OnLogReceived;
    }

    public class LogUtil : ILogUtil, IDisposable
    {
        #region 字段与属性

        // 线程锁 (确保多线程写入文件和操作SB时的安全)
        private readonly object _lockObj = new object();

        // 缓存对象 (用于减少 GC)
        private readonly StringBuilder _sb = new StringBuilder(1024);
        // 预编译正则表达式，用于移除富文本标签，性能更高
        private static readonly Regex _richTextRegex = new Regex("<.*?>", RegexOptions.Compiled);

        // 颜色管理
        private readonly Dictionary<string, Color> _moduleColors = new Dictionary<string, Color>(); // 模块标签到颜色的映射
        private readonly Queue<Color> _colorPool = new Queue<Color>(); // 颜色池，用于复用颜色
        private readonly HashSet<string> _usedModules = new HashSet<string>(); // 已使用的模块标签，避免重复分配颜色

        // 配置项
        private bool _showTimestamp = true; // 默认开启时间戳
        private bool _isEnabled = true; // 默认开启日志记录
        private bool _enableReflection = false; // 默认关闭反射栈跟踪
        private bool _enableSaveToFile = false; // 默认关闭文件保存

        // 文件写入相关
        private string _logFilePath; // 日志文件路径
        private StreamWriter _fileWriter; // 文件写入流

        //  C# 原生随机数，用于多线程环境
        private readonly System.Random _sysRandom = new System.Random();

        // 事件（日志接收事件，用于同步到 YFanConsole）
        public event Action<LogData> OnLogReceived; // 日志接收事件，用于同步到 YFanConsole

        // 预设颜色池 (Pastel 色系，护眼且清晰)
        private static readonly Color[] _presetColors = new Color[]
        {
            new Color(0.6f, 0.9f, 1f),
            new Color(1f, 0.8f, 0.6f),
            new Color(0.8f, 1f, 0.6f),
            new Color(1f, 0.6f, 0.8f),
            new Color(0.7f, 0.7f, 1f),
            new Color(1f, 0.9f, 0.6f),
            new Color(0.6f, 1f, 0.8f),
            new Color(1f, 0.6f, 0.6f),
            new Color(0.8f, 0.6f, 1f),
            new Color(0.6f, 1f, 1f),
            new Color(1f, 0.7f, 0.5f),
            new Color(0.5f, 0.8f, 1f)
        };

        #endregion

        #region 初始化与销毁

        public LogUtil()
        {
            // 初始化颜色池
            foreach (var color in _presetColors)
            {
                _colorPool.Enqueue(color);
            }

            // 执行文件系统操作
            CleanUpOldLogs();
            SetupLogFile();
        }

        /// <summary>
        /// 初始化日志文件路径
        /// </summary>
        private void SetupLogFile()
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "Logs");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // 按启动时间命名文件，避免覆盖
                string fileName = $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                _logFilePath = Path.Combine(dir, fileName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LogUtil] 初始化日志路径失败: {e.Message}");
            }
        }

        /// <summary>
        /// 自动清理 7 天前的旧日志，防止占满玩家手机空间
        /// </summary>
        private void CleanUpOldLogs()
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "Logs");
                if (!Directory.Exists(dir)) return;

                var directoryInfo = new DirectoryInfo(dir);
                var files = directoryInfo.GetFiles("Log_*.txt");
                var expireTime = DateTime.Now.AddDays(-7);

                foreach (var file in files)
                {
                    if (file.CreationTime < expireTime)
                    {
                        file.Delete();
                    }
                }
            }
            catch
            {
                // 忽略清理错误，不影响游戏运行
            }
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                if (_fileWriter != null)
                {
                    try
                    {
                        _fileWriter.Close();
                        _fileWriter.Dispose();
                    }
                    catch { }
                    _fileWriter = null;
                }
            }
        }

        #endregion

        #region 接口实现

        public void Info(string message, params string[] modules) => LogInternal(LogLevel.Info, message, null, modules);
        public void Warn(string message, params string[] modules) => LogInternal(LogLevel.Warn, message, null, modules);
        public void Error(string message, params string[] modules) => LogInternal(LogLevel.Error, message, null, modules);
        public void Exception(Exception ex, params string[] modules) => LogInternal(LogLevel.Exception, ex.Message, ex.StackTrace, modules);

        public void EnableTimestamp(bool enable) => _showTimestamp = enable;
        public void EnableGlobal(bool enable) => _isEnabled = enable;
        public void EnableReflection(bool enable) => _enableReflection = enable;

        /// <summary>
        /// 启用/禁用将日志保存到文件
        /// </summary>
        /// <param name="enable"></param>
        public void EnableSaveToFile(bool enable)
        {
            lock (_lockObj)
            {
                _enableSaveToFile = enable;
                // 只有当真正开启且 Writer 为空时才创建流
                if (enable && _fileWriter == null && !string.IsNullOrEmpty(_logFilePath))
                {
                    try
                    {
                        // FileShare.ReadWrite 允许外部工具同时打开查看日志
                        var fs = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        _fileWriter = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[LogUtil] 无法创建日志文件流: {e.Message}");
                        _enableSaveToFile = false;
                    }
                }
            }
        }

        #endregion

        #region 内部核心逻辑


        /// <summary>
        /// 内部日志记录方法
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="stackTraceOverride"></param>
        /// <param name="modules"></param>
        private void LogInternal(LogLevel level, string message, string stackTraceOverride, string[] modules)
        {
            if (!_isEnabled) return;

            string moduleColorHex = null;
            string finalMessageForUnity;

            // 1. 核心处理 (加锁)
            lock (_lockObj)
            {
                // 颜色分配
                if (modules != null && modules.Length > 0)
                {
                    string firstModule = modules[0].Trim();
                    if (!string.IsNullOrEmpty(firstModule))
                    {
                        if (!_moduleColors.ContainsKey(firstModule)) AssignColorToModule(firstModule);
                        if (_moduleColors.TryGetValue(firstModule, out var c)) moduleColorHex = ColorToHex(c);
                    }
                }

                // 字符串构建
                _sb.Clear();
                BuildPrefix(_sb, level, modules);

                // 反射获取堆栈 (仅在编辑器下启用，真机性能开销太大)
#if UNITY_EDITOR
                if (_enableReflection && string.IsNullOrEmpty(stackTraceOverride))
                {
                    // Skip frames: 0=GetMethod, 1=LogInternal, 2=PublicMethod(Info/Warn), 3=Caller
                    var stackFrame = new StackTrace(3, true).GetFrame(0);
                    if (stackFrame != null)
                    {
                        var method = stackFrame.GetMethod();
                        _sb.Append($" [Ref: {method.DeclaringType?.Name}.{method.Name}]");
                    }
                }
#endif
                _sb.Append(" ");
                _sb.Append(message);

                finalMessageForUnity = _sb.ToString();
            }

            // 2. Unity 原生输出 (Debug.Log 内部线程安全)
            switch (level)
            {
                case LogLevel.Info: Debug.Log(finalMessageForUnity); break;
                case LogLevel.Warn: Debug.LogWarning(finalMessageForUnity); break;
                case LogLevel.Error: Debug.LogError(finalMessageForUnity); break;
                case LogLevel.Exception: Debug.LogError($"[Exception] {message}\n{stackTraceOverride}"); break;
            }

            // 3. 数据分发 (文件写入 & 控制台事件)
            // 只有当需要存档或有订阅者时才执行正则剥离，节省性能
            if (_enableSaveToFile || OnLogReceived != null)
            {
                string cleanMessage = _richTextRegex.Replace(finalMessageForUnity, string.Empty);

                // 注意：如果在非主线程调用，ExtractStackTrace 可能获取不到准确的 Unity 堆栈
                string trace = stackTraceOverride ?? StackTraceUtility.ExtractStackTrace();

                var logData = new LogData()
                {
                    Level = level,
                    Message = cleanMessage,
                    Modules = modules,
                    Time = DateTime.Now,
                    StackTrace = trace,
                    ModuleColorHex = moduleColorHex
                };

                // 事件通知
                OnLogReceived?.Invoke(logData);

                // 写入文件
                if (_enableSaveToFile)
                {
                    WriteToFile(logData);
                }
            }
        }

        /// <summary>
        /// 构建日志前缀
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="level"></param>
        /// <param name="modules"></param>
        private void BuildPrefix(StringBuilder sb, LogLevel level, string[] modules)
        {
            if (_showTimestamp)
            {
                sb.Append($"<color=#888888>[{DateTime.Now:HH:mm:ss}]</color> ");
            }

            // 模块 Tag
            if (modules != null && modules.Length > 0)
            {
                foreach (var mod in modules)
                {
                    string moduleName = mod?.Trim();
                    if (string.IsNullOrEmpty(moduleName)) continue;

                    Color col = _moduleColors.TryGetValue(moduleName, out var c) ? c : Color.white;
                    string hex = ColorUtility.ToHtmlStringRGB(col);
                    sb.Append($"<b><color=#{hex}>[{moduleName}]</color></b> ");
                }
            }
        }

        /// <summary>
        /// 写入日志数据到文件
        /// </summary>
        /// <param name="data"></param>
        private void WriteToFile(LogData data)
        {
            if (_fileWriter == null) return;

            lock (_lockObj) // 再次加锁保护文件写入
            {
                try
                {
                    // 手动拼接以获得最高写入性能
                    _fileWriter.Write("[");
                    _fileWriter.Write(data.Time.ToString("yyyy-MM-dd HH:mm:ss"));
                    _fileWriter.Write("] [");
                    _fileWriter.Write(data.Level);
                    _fileWriter.Write("] ");

                    if (data.Modules != null)
                    {
                        foreach (var m in data.Modules)
                        {
                            _fileWriter.Write("[");
                            _fileWriter.Write(m);
                            _fileWriter.Write("]");
                        }
                    }

                    _fileWriter.Write(" : ");
                    _fileWriter.WriteLine(data.Message);

                    if (data.Level == LogLevel.Error || data.Level == LogLevel.Exception)
                    {
                        _fileWriter.WriteLine("Stack Trace:");
                        _fileWriter.WriteLine(data.StackTrace);
                        _fileWriter.WriteLine("--------------------------------------------------");
                    }
                }
                catch
                {
                    // 写入失败不应崩溃游戏
                }
            }
        }

        /// <summary>
        /// 为模块分配颜色
        /// </summary>
        /// <param name="module"></param>
        private void AssignColorToModule(string module)
        {
            if (_moduleColors.ContainsKey(module)) return;
            Color color = (_colorPool.Count > 0) ? _colorPool.Dequeue() : GenerateReadableRandomColor();
            _moduleColors[module] = color;
            _usedModules.Add(module);
        }

        /// <summary>
        /// 生成可读的随机颜色（当模块颜色池为空时调用）
        /// </summary>
        /// <returns></returns>
        private Color GenerateReadableRandomColor()
        {
            // 生成高明度颜色 (0.5 ~ 1.0)，确保在深色控制台中看清
            float r = (float)(0.5 + _sysRandom.NextDouble() * 0.5);
            float g = (float)(0.5 + _sysRandom.NextDouble() * 0.5);
            float b = (float)(0.5 + _sysRandom.NextDouble() * 0.5);

            return new Color(r, g, b, 1f);
        }

        /// <summary>
        /// 将 Color 转换为 HTML 格式的 RGB 颜色字符串
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private string ColorToHex(Color c)
        {
            int r = (int)(c.r * 255);
            int g = (int)(c.g * 255);
            int b = (int)(c.b * 255);
            return $"{r:X2}{g:X2}{b:X2}";
        }

        #endregion
    }
}
