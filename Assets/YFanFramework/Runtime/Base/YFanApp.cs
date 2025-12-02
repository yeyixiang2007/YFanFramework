using QFramework;
using YFan.Utils;

namespace YFan.Runtime.Base
{
    public class YFanApp : Architecture<YFanApp>
    {
        override protected void Init()
        {
            if (RegisterUtils()) YLog.Info("基础工具注册成功", "YFanApp", "Init");
            if (RegisterModules()) YLog.Info("架构模块注册成功", "YFanApp", "Init");
        }

        /// <summary>
        /// 注册基础工具（仅实例工具）
        /// </summary>
        /// <returns></returns>
        private bool RegisterUtils()
        {
            // 静态工具无需实例化

            try
            {
                // 初始化日志工具（基础工具，最先注册）
                var logUtil = new LogUtil();
                RegisterUtility<ILogUtil>(logUtil);
                YLog.Init(logUtil);
            }
            catch (System.Exception e)
            {
                YLog.Error("初始化日志工具失败：" + e.Message, "YFanApp", "RegisterUtils");
                return false;
            }

            try
            {
                RegisterUtility<IMonoUtil>(new MonoUtil()); // 初始化 MonoUtil
            }
            catch (System.Exception e)
            {
                YLog.Error("初始化 MonoUtil 失败：" + e.Message, "YFanApp", "RegisterUtils");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 注册模块
        /// </summary>
        /// <returns></returns>
        private bool RegisterModules()
        {
            return true;
        }
    }
}
