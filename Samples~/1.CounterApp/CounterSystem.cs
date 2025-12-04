using QFramework;
using YFan.Attributes;
using YFan.Utils;

public interface ICounterSystem : ISystem
{
}

[AutoRegister(typeof(ICounterSystem))]
public class CounterSystem : AbstractSystem, ICounterSystem
{
    protected override void OnInit()
    {
        this.GetModel<ICounterModel>().Counter.RegisterWithInitValue((value) =>
        {
            if (value == 10)
            {
                YLog.Info($"计数器数值增加到：{value}", "CounterSystem");
            }
        });
    }
}
