using QFramework;
using YFan.Utils;

namespace YFan.Runtime.Base
{
    public class YFanApp : Architecture<YFanApp>
    {
        override protected void Init()
        {
            // 初始化日志工具
            var logUtil = new LogUtil();
            RegisterUtility<ILogUtil>(logUtil);
            YLog.Init(logUtil);

            // 初始化 MonoUtil
            var monoUtil = new MonoUtil();
            RegisterUtility<IMonoUtil>(monoUtil);
        }
    }
}
