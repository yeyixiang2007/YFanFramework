using Cysharp.Threading.Tasks;
using QFramework;
using YFan.Runtime.Utils;

namespace YFan.Runtime.Base
{
    public class YFanApp : Architecture<YFanApp>
    {
        override protected void Init()
        {
            if (RegisterCoreUtils()) YLog.Info("基础工具注册成功", "YFanApp");
            AutoModuleBinder.ScanAndRegister(this);
            YLog.Info("架构模块注册成功", "YFanApp");
        }

        /// <summary>
        /// 注册基础工具（仅实例工具）
        /// </summary>
        /// <returns></returns>
        private bool RegisterCoreUtils()
        {
            try
            {
                // 初始化日志工具（基础工具，最先注册）
                var logUtil = new LogUtil();
                RegisterUtility<ILogUtil>(logUtil);
                YLog.Init(logUtil);
            }
            catch (System.Exception e)
            {
                YLog.Error("初始化日志工具失败：" + e.Message, "YFanApp");
                return false;
            }

            try
            {
                // 初始化 AssetUtil (资源加载)
                var assetUtil = new AssetUtil();
                RegisterUtility<IAssetUtil>(assetUtil);

                // 立即触发 Addressables 初始化，确保后续加载可用
                // 使用 Forget() 不阻塞主线程，Addressables 内部会处理并发
                assetUtil.InitializeAsync().Forget();
            }
            catch (System.Exception e)
            {
                YLog.Error("初始化 AssetUtil 失败：" + e.Message, "YFanApp");
                return false;
            }

            return true;
        }
    }
}
