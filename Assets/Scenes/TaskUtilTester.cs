using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YFan.Runtime.Base.Abstract; // 确保引用了你的 AbstractController
using YFan.Utils;

public class TaskUtilTester : AbstractController
{
    private CancellationTokenSource _cts;

    private void Start()
    {
        YLog.Info("TaskUtil 测试器已启动", "Tester");
    }

    private void OnDestroy()
    {
        // 测试组件销毁时，自动清理 CTS
        TaskUtil.CancelSafe(ref _cts);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(690, 10, 350, 600));
        GUILayout.Box("<b>=== TaskUtil 异步工具测试 ===</b>");
        GUILayout.Space(10);

        // --- 测试 1: 安全执行与异常捕获 ---
        if (GUILayout.Button("1. 测试 Run (捕获异常)"))
        {
            // 传统 async void 会导致异常丢失或导致 Unity 报错且无法定位
            // 使用 TaskUtil.Run 可以将异常接管给 YLog
            TaskUtil.Run(async () =>
            {
                YLog.Info("开始执行会失败的任务...", "AsyncTest");
                await UniTask.Delay(500);
                throw new InvalidOperationException("这是一个测试抛出的异常！应该在 YLog Error 中看到我。");
            }, "AsyncTest");
        }

        GUILayout.Space(10);

        // --- 测试 2: 重试机制 ---
        if (GUILayout.Button("2. 测试 Retry (模拟网络不稳定)"))
        {
            RunRetryTest();
        }

        GUILayout.Space(10);

        // --- 测试 3: 取消机制 ---
        if (GUILayout.Button($"3. 测试 Cancel/Renew (当前: {(_cts == null ? "空" : "运行中")})"))
        {
            RunCancelTest();
        }

        GUILayout.Space(10);

        // --- 测试 4: 超时等待 ---
        if (GUILayout.Button("4. 测试 WaitUntil 超时"))
        {
            RunTimeoutTest();
        }

        GUILayout.EndArea();
    }

    // --- 模拟逻辑实现 ---

    private void RunRetryTest()
    {
        int mockFailCount = 0;

        TaskUtil.Run(async () =>
        {
            YLog.Info("开始 Retry 测试 (设定前2次失败，第3次成功)", "RetryTest");

            // 调用重试工具
            string result = await TaskUtil.Retry<string>(async () =>
            {
                mockFailCount++;
                YLog.Info($"尝试第 {mockFailCount} 次请求...", "RetryTest");

                await UniTask.Delay(200); // 模拟耗时

                if (mockFailCount < 3)
                {
                    throw new Exception("模拟连接超时");
                }
                return "请求成功数据 Payload";
            },
            retryCount: 3,
            delaySeconds: 0.5f,
            onRetry: (count, ex) =>
            {
                YLog.Warn($"第 {count} 次失败: {ex.Message}，准备重试...", "RetryTest");
            });

            YLog.Info($"最终结果: {result}", "RetryTest");

        }, "RetryTest");
    }

    private void RunCancelTest()
    {
        // Renew 会自动取消上一个 _cts (如果有的话) 并创建新的
        var token = TaskUtil.Renew(ref _cts).Token;

        TaskUtil.Run(async () =>
        {
            YLog.Info("启动一个 5秒 的长任务...", "CancelTest");
            try
            {
                await UniTask.Delay(5000, cancellationToken: token);
                YLog.Info("任务完成 (未被取消)", "CancelTest");
            }
            catch (OperationCanceledException)
            {
                // 注意：TaskUtil.Run 内部会吞掉这个异常并打印 Info，或者你可以自己 catch 处理业务
                YLog.Warn("检测到任务被取消！执行清理逻辑...", "CancelTest");
                throw; // 抛出给 TaskUtil.Run 处理（它会忽略这个异常）
            }
            finally
            {
                // 任务结束（无论成功还是取消），如果是当前 CTS 则置空
                if (_cts != null && _cts.Token == token)
                {
                    TaskUtil.CancelSafe(ref _cts);
                }
            }
        }, "CancelTest");
    }

    private void RunTimeoutTest()
    {
        TaskUtil.Run(async () =>
        {
            YLog.Info("开始等待条件 (超时设定 2秒)...", "TimeoutTest");

            // 模拟一个永远不会满足的条件
            bool success = await TaskUtil.WaitUntil(() => false, timeoutSeconds: 2.0f);

            if (success)
                YLog.Info("条件满足！", "TimeoutTest");
            else
                YLog.Error("等待超时！逻辑中断。", "TimeoutTest");
        });
    }
}
