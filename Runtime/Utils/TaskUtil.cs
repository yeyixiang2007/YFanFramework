using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YFan.Runtime.Utils
{
    /// <summary>
    /// 任务工具类，提供异步任务的安全执行、重试机制等功能。
    /// + 支持流式调用，提高代码可读性。
    /// + 提供异常捕获、取消令牌管理等功能。
    /// + 设计为静态类，方便全局调用。
    /// </summary>
    public static class TaskUtil
    {
        #region 安全执行 (Fire & Forget with Logging)

        /// <summary>
        /// 启动一个异步任务，不等待它完成（Fire-and-Forget）。
        /// + 异常会被自动捕获并记录到 YLog。
        /// + 支持流式调用: taskFunc.RunSafe("Module")
        /// </summary>
        public static void Run(this Func<UniTask> taskFunc, string moduleName = "Async")
        {
            RunInternal(taskFunc(), moduleName).Forget();
        }

        /// <summary>
        /// 启动一个已创建的 Task
        /// + 支持流式调用: myTask.RunSafe("Module")
        /// </summary>
        public static void Run(this UniTask task, string moduleName = "Async")
        {
            RunInternal(task, moduleName).Forget();
        }

        // 为了让流式调用更直观，增加一个 RunSafe 别名指向 Run
        // 这样写代码时 myTask.RunSafe() 比 myTask.Run() 语义更清晰（表明是安全执行）
        public static void RunSafe(this UniTask task, string moduleName = "Async") => Run(task, moduleName);
        public static void RunSafe(this Func<UniTask> taskFunc, string moduleName = "Async") => Run(taskFunc, moduleName);

        /// <summary>
        /// 内部执行方法，处理异常和取消
        /// + 支持流式调用: RunInternal(...)
        /// </summary>
        private static async UniTaskVoid RunInternal(UniTask task, string moduleName)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // YLog.Info("Task Canceled", moduleName);
            }
            catch (Exception e)
            {
                YLog.Exception(e, moduleName);
            }
        }

        #endregion

        #region 重试机制 (Retry Logic)

        /// <summary>
        /// 带有重试机制的执行
        /// + 支持流式调用: myAction.Retry(...)
        /// </summary>
        public static async UniTask<T> Retry<T>(
            this Func<UniTask<T>> action, // 加了 this
            int retryCount = 3,
            float delaySeconds = 1.0f,
            Action<int, Exception> onRetry = null)
        {
            int tried = 0;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    tried++;
                    if (tried > retryCount) throw;

                    onRetry?.Invoke(tried, ex);
                    await UniTask.WaitForSeconds(delaySeconds);
                }
            }
        }

        /// <summary>
        /// 无返回值的重试
        /// + 支持流式调用: myAction.Retry(...)
        /// </summary>
        public static async UniTask Retry(
            this Func<UniTask> action, // 加了 this
            int retryCount = 3,
            float delaySeconds = 1.0f,
            Action<int, Exception> onRetry = null)
        {
            int tried = 0;
            while (true)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    tried++;
                    if (tried > retryCount) throw;

                    onRetry?.Invoke(tried, ex);
                    await UniTask.WaitForSeconds(delaySeconds);
                }
            }
        }

        #endregion

        #region 令牌管理 (Cancellation)

        // 注意：扩展方法不支持 ref 参数，所以 CancelSafe 和 Renew 必须保持静态调用风格
        // 这也是为了安全，确保外部变量能被置空

        /// <summary>
        /// 安全取消 CancellationTokenSource
        /// + 支持流式调用: cts.CancelSafe()
        /// </summary>
        public static void CancelSafe(ref CancellationTokenSource cts)
        {
            if (cts != null)
            {
                try
                {
                    if (!cts.IsCancellationRequested) cts.Cancel();
                }
                catch (ObjectDisposedException) { }
                finally
                {
                    cts.Dispose();
                    cts = null;
                }
            }
        }

        /// <summary>
        /// 安全重新创建 CancellationTokenSource
        /// + 支持流式调用: cts.Renew()
        /// </summary>
        public static CancellationTokenSource Renew(ref CancellationTokenSource cts)
        {
            CancelSafe(ref cts);
            cts = new CancellationTokenSource();
            return cts;
        }

        #endregion

        #region 条件等待

        /// <summary>
        /// 等待直到条件满足或超时
        /// + 支持流式调用: await WaitUntil(...)
        /// </summary>
        public static async UniTask<bool> WaitUntil(Func<bool> predicate, float timeoutSeconds, PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            float timer = 0f;
            while (!predicate())
            {
                timer += Time.deltaTime;
                if (timer >= timeoutSeconds) return false;
                await UniTask.Yield(timing);
            }
            return true;
        }

        #endregion

        #region 时间等待封装

        /// <summary>
        /// 延迟指定秒数 (封装 UniTask.Delay)
        /// </summary>
        /// <param name="seconds">秒数</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放(受不受暂停影响)</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static UniTask Delay(float seconds, bool ignoreTimeScale = false, CancellationToken cancellationToken = default)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale, PlayerLoopTiming.Update, cancellationToken);
        }

        /// <summary>
        /// 延迟指定帧数 (封装 UniTask.Yield)
        /// </summary>
        /// <param name="frames">帧数</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static UniTask DelayFrame(int frames, CancellationToken cancellationToken = default)
        {
            return UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        #endregion
    }
}
