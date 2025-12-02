using System;
using System.Threading.Tasks;
using UnityEngine;
using QFramework;
using YFan.Attributes;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

public class LogUtilTester : AbstractController
{
    private ILogUtil _logUtil;

    // --- 状态显示 ---

    [YTitle("实时状态监控")]
    [YReadOnly]
    [SerializeField] // 必须序列化才能在 Inspector 显示
    private string _lastEventModule = "None";

    [YReadOnly]
    [SerializeField]
    private string _lastEventMessage = "等待日志...";

    void Start()
    {
        _logUtil = this.GetUtility<ILogUtil>();

        // 配置
        _logUtil.EnableSaveToFile(false);
        _logUtil.EnableTimestamp(true);
        _logUtil.EnableReflection(true);

        // 绑定事件
        _logUtil.OnLogReceived += OnConsoleReceiveLog;

        YLog.Info("LogUtil 测试环境就绪", "System");
    }

    private void OnConsoleReceiveLog(LogData data)
    {
        _lastEventMessage = $"[{data.Level}] {data.Message}";
        _lastEventModule = data.Modules != null && data.Modules.Length > 0 ? data.Modules[0] : "None";
    }

    // --- 功能测试区 ---

    [YTitle("基础日志测试")]
    [YButton("1. 普通日志", 35)]
    private void TestInfo()
    {
        YLog.Info("这是一条普通的 Info 日志");
    }

    [YButton("2. UI 模块 (自动配色)", 35)]
    [YColor("#88FF88")] // 按钮颜色微调
    private void TestUI()
    {
        YLog.Info("主界面已打开", "UI");
    }

    [YButton("3. Network 模块", 35)]
    [YColor("#8888FF")]
    private void TestNet()
    {
        YLog.Info("连接服务器成功", "Network");
    }

    [YButton("4. 多模块标签", 35)]
    private void TestMulti()
    {
        YLog.Warn("玩家数据校验异常", "Player", "Data");
    }

    [YTitle("异常与压力测试")]
    [YButton("5. 错误日志", 40)]
    [YColor(1f, 0.6f, 0.6f)] // 红色按钮
    private void TestError()
    {
        YLog.Error("加载资源失败: NotFound", "Asset");
    }

    [YButton("6. 抛出异常 (带堆栈)", 40)]
    [YColor(1f, 0.4f, 0.4f)]
    private void TestException()
    {
        try
        {
            int a = 0;
            int b = 10 / a;
        }
        catch (Exception e)
        {
            YLog.Exception(e, "System");
        }
    }

    [YButton("7. 多线程并发测试", 50)]
    [YColor(1f, 0.8f, 0.2f)] // 黄色按钮
    private async void RunThreadTest()
    {
        YLog.Info("开始多线程压力测试...", "ThreadTest");
        var tasks = new Task[5];
        for (int i = 0; i < 5; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 50; j++)
                {
                    YLog.Info($"Thread[{threadId}] Log Index: {j}", "Thread");
                }
            });
        }
        await Task.WhenAll(tasks);
        YLog.Info("多线程测试完成", "ThreadTest");
    }
}
