using System.Threading;
using TMPro;
using UnityEngine.UI;
using YFan.Attributes;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

/// <summary>
/// 计数器控制器
/// </summary>
public class CounterController : UIAbstractController
{
    [UIBind("Txt_Count")] private TMP_Text Txt_Count;
    [UIBind("Btn_Stop")] private Button Btn_Stop;

    [YReadOnly] private int CurrentValue = 0;
    private CancellationTokenSource _autoAddCts; // 用于控制自动增加任务的取消

    protected override void OnAwake()
    {
        // 第一次调用时，获取 Architecture 激活架构
        var _ = this.GetArchitecture();

        Txt_Count.text = CurrentValue.ToString();
    }

    [BindClick("Btn_Add")]
    private void OnBtnAddClick()
    {
        CurrentValue++;
        Txt_Count.text = CurrentValue.ToString();
        YLog.Info($"当前值: {CurrentValue}");
    }

    [BindClick("Btn_Sub")]
    private void OnBtnSubClick()
    {
        CurrentValue--;
        Txt_Count.text = CurrentValue.ToString();
        YLog.Info($"当前值: {CurrentValue}");
    }

    [BindClick("Btn_AutoAdd")]
    private void OnBtnAutoAddClick()
    {
        // 如果已经有任务在运行，先取消
        if (_autoAddCts != null && !_autoAddCts.IsCancellationRequested)
        {
            _autoAddCts.Cancel();
            YLog.Info("已停止当前自动增加任务");
            return;
        }

        // 创建新的取消令牌
        _autoAddCts = new CancellationTokenSource();

        // 使用 TaskUtil.Run 的正确方式
        TaskUtil.Run(async () =>
            {
                var token = _autoAddCts.Token;
                while (!token.IsCancellationRequested)
                {
                    CurrentValue++;
                    Txt_Count.text = CurrentValue.ToString();
                    await TaskUtil.Delay(0.1f, false, token);
                }
                YLog.Info("自动增加已停止");
            }, "AutoAdd");

        YLog.Info("开始自动增加");
    }

    [BindClick("Btn_Stop")]
    private void OnBtnStopClick()
    {
        // 停止自动增加任务
        if (_autoAddCts != null && !_autoAddCts.IsCancellationRequested)
        {
            _autoAddCts.Cancel();
            YLog.Info("已停止自动增加");
        }
        else
        {
            YLog.Warn("没有正在运行的自动增加任务", "CounterController");
        }
    }
}
