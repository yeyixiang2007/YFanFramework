using QFramework;
using System;
using System.Threading.Tasks;
using UnityEngine;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

public class LogUtilTester : AbstractController
{
    private ILogUtil _logUtil;

    // 用于在屏幕上显示从事件系统收到的最后一条纯净日志
    private string _lastEventMessage = "等待日志...";
    private string _lastEventModule = "";

    void Start()
    {
        // 1. 初始化
        _logUtil = this.GetUtility<ILogUtil>();

        // 2. 基础配置 (按你的要求，不开启文件保存)
        _logUtil.EnableSaveToFile(false);
        _logUtil.EnableTimestamp(true);
        _logUtil.EnableReflection(true); // 在编辑器下测试反射堆栈显示

        // 3. 绑定事件 (模拟 YFanConsole 接收数据的逻辑)
        _logUtil.OnLogReceived += OnConsoleReceiveLog;

        Debug.Log("<b><color=green>=== LogUtil 测试环境已就绪 ===</color></b>");
        YLog.Info("LogUtil 初始化完成", "System");
    }

    /// <summary>
    /// 模拟控制台接收数据的回调
    /// </summary>
    private void OnConsoleReceiveLog(LogData data)
    {
        // 验证点：这里收到的 Message 应该是没有 <color> 标签的纯文本
        _lastEventMessage = $"[{data.Level}] {data.Message}";
        _lastEventModule = data.Modules != null && data.Modules.Length > 0 ? data.Modules[0] : "None";
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 600));
        GUILayout.Box("<b>=== LogUtil 测试 ===</b>");

        GUILayout.Space(10);
        GUILayout.Label("<b>--- 功能测试 ---</b>");

        if (GUILayout.Button("1. 普通日志 (无模块)"))
        {
            YLog.Info("这是一条普通的 Info 日志");
        }

        if (GUILayout.Button("2. 模块日志 (UI - 自动配色)"))
        {
            // 第一次调用 "UI" 模块，系统会自动分配一个颜色
            YLog.Info("主界面已打开", "UI");
        }

        if (GUILayout.Button("3. 模块日志 (Network - 自动配色)"))
        {
            YLog.Info("连接服务器成功", "Network");
        }

        if (GUILayout.Button("4. 多模块日志 (Player + Data)"))
        {
            YLog.Warn("玩家数据校验异常", "Player", "Data");
        }

        if (GUILayout.Button("5. 错误日志"))
        {
            YLog.Error("加载资源失败: NotFound", "Asset");
        }

        if (GUILayout.Button("6. 异常测试 (带堆栈)"))
        {
            try
            {
                int a = 0;
                int b = 10 / a; // 制造除零异常
            }
            catch (Exception e)
            {
                YLog.Exception(e, "System");
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("<b>--- 压力与线程测试 ---</b>");

        if (GUILayout.Button("7. 多线程并发测试 (验证锁)"))
        {
            RunThreadTest();
        }

        GUILayout.Space(20);
        GUILayout.Label("<b>--- 事件系统验证 ---</b>");
        GUILayout.Label($"收到模块: {_lastEventModule}");
        GUILayout.Label($"收到内容(无富文本): \n{_lastEventMessage}");

        GUILayout.EndArea();
    }

    private async void RunThreadTest()
    {
        YLog.Info("开始多线程压力测试...", "ThreadTest");

        // 模拟 5 个线程同时并发写入日志
        var tasks = new Task[5];
        for (int i = 0; i < 5; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    // 如果 LogUtil 没有加锁，这里会报错或 StringBuilder 内容错乱
                    YLog.Info($"Thread[{threadId}] Log Index: {j}", "Thread");
                }
            });
        }

        await Task.WhenAll(tasks);

        YLog.Info("多线程测试完成！如果没有报错且日志完整，说明线程安全。", "ThreadTest");
    }
}
