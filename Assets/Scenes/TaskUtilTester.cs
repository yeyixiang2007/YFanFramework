using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YFan.Attributes; // 引入 UI 特性
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

public class TaskUtilTester : AbstractController
{
    private CancellationTokenSource _cts;

    // --- 状态显示 ---
    [YTitle("令牌状态")]
    [YReadOnly]
    [SerializeField]
    private string _ctsStatus = "Empty";

    private void Start()
    {
        YLog.Info("TaskUtil 测试器已启动", "Tester");
        UpdateStatus();
    }

    private void OnDestroy()
    {
        TaskUtil.CancelSafe(ref _cts);
    }

    private void UpdateStatus()
    {
        _ctsStatus = _cts == null ? "Empty (无任务)" : "Active (运行中)";
    }

    // --- 异常处理测试 ---

    [YTitle("功能测试")]
    [YButton("1. Run (捕获异常)", 40)]
    private void TestRun()
    {
        // 演示流式调用 RunSafe
        Func<UniTask> task = async () =>
        {
            YLog.Info("任务开始...", "AsyncTest");
            await UniTask.Delay(500);
            throw new InvalidOperationException("测试异常！请检查 YLog Error。");
        };

        task.RunSafe("AsyncTest");
    }

    // --- 重试测试 ---

    [YButton("2. Retry (模拟网络重试)", 40)]
    private void TestRetry()
    {
        RunRetryTest();
    }

    // --- 取消测试 ---

    [YButton("3. Cancel / Renew", 40)]
    [YColor(1f, 0.9f, 0.5f)]
    private void TestCancel()
    {
        RunCancelTest();
    }

    // --- 超时测试 ---

    [YButton("4. WaitUntil (测试超时)", 40)]
    private void TestTimeout()
    {
        RunTimeoutTest();
    }

    // --- 逻辑实现 (保持不变，仅添加 UpdateStatus 调用) ---

    private void RunRetryTest()
    {
        int mockFailCount = 0;
        TaskUtil.Run(async () =>
        {
            string result = await TaskUtil.Retry<string>(async () =>
            {
                mockFailCount++;
                YLog.Info($"请求第 {mockFailCount} 次...", "RetryTest");
                await UniTask.Delay(200);
                if (mockFailCount < 3) throw new Exception("连接超时");
                return "Success";
            }, retryCount: 3, delaySeconds: 0.5f);

            YLog.Info($"结果: {result}", "RetryTest");
        }, "RetryTest");
    }

    private void RunCancelTest()
    {
        // Renew 时更新 UI 状态
        var token = TaskUtil.Renew(ref _cts).Token;
        UpdateStatus();

        TaskUtil.Run(async () =>
        {
            YLog.Info("启动 5秒 任务...", "CancelTest");
            try
            {
                await UniTask.Delay(5000, cancellationToken: token);
                YLog.Info("任务完成", "CancelTest");
            }
            catch (OperationCanceledException)
            {
                YLog.Warn("任务被取消", "CancelTest");
            }
            finally
            {
                if (_cts != null && _cts.Token == token) TaskUtil.CancelSafe(ref _cts);
                UpdateStatus(); // 任务结束更新 UI
            }
        }, "CancelTest");
    }

    private void RunTimeoutTest()
    {
        TaskUtil.Run(async () =>
        {
            YLog.Info("等待条件 (限时 2s)...", "TimeoutTest");
            bool success = await TaskUtil.WaitUntil(() => false, timeoutSeconds: 2.0f);
            if (!success) YLog.Error("超时！", "TimeoutTest");
        });
    }
}
