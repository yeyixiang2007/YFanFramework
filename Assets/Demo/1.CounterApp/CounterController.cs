using QFramework;
using TMPro;
using UnityEngine;
using YFan.Attributes;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

/// <summary>
/// 计数器数值改变命令
/// </summary>
public class CounterChangeCommand : AbstractCommand
{
    public int CounterValue { get; set; }

    protected override void OnExecute()
    {
        this.GetModel<ICounterModel>().Counter.Value += CounterValue;
    }
}

/// <summary>
/// 计数器控制器
/// </summary>
class CounterController : UIAbstractController
{
    /// <summary>
    /// 计数器文本
    /// + [UIBind]: 绑定到场景中对应的文本控件
    /// </summary>
    [UIBind] public TMP_Text Txt_Counter;

    /// <summary>
    /// 计数器数值
    /// + [YReadOnly]: 只读属性，不能在运行时修改
    /// </summary>
    [SerializeField][YReadOnly] private int counter = 0;

    protected override void OnAwake()
    {
        IArchitecture _ = GetArchitecture();

        // 注册计数器数值改变事件
        this.GetModel<ICounterModel>().Counter.RegisterWithInitValue((value) =>
        {
            Txt_Counter.text = value.ToString();
        });
    }

    /// <summary>
    /// 增加按钮点击事件
    /// + [BindClick]: 绑定到场景中对应的按钮控件
    /// + [YButton]: 可以在 Inspector 中点击的按钮
    /// </summary>
    [BindClick("Btn_Add")]
    [YButton("Add")]
    private void OnBtnAddClick()
    {
        // 发送计数器数值改变命令
        this.SendCommand(new CounterChangeCommand()
        {
            CounterValue = 1,
        });

        // YLog 打印日志
        // + 第一个参数：日志内容
        // + 第二个参数：日志模块（一个或多个）
        YLog.Info("按钮 Btn_Add 被点击了", "CounterApp");
    }
}
