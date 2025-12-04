using QFramework;
using YFan.Attributes;

public interface ICounterModel : IModel
{
    /// <summary>
    /// 计数器数值
    /// </summary>
    BindableProperty<int> Counter { get; set; }
}

[AutoRegister(typeof(ICounterModel))]
public class CounterModel : AbstractModel, ICounterModel
{
    public BindableProperty<int> Counter { get; set; } = new BindableProperty<int>(0);

    protected override void OnInit()
    {
        Counter.SetValueWithoutEvent(0);
    }
}
