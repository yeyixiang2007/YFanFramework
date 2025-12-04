using QFramework;
using YFan.Attributes;
using YFan.Utils;

/// <summary>
/// 玩家系统
/// </summary>
public interface IPlayerSystem : ISystem
{
    void SaveGame();
    void LoadGame();
}

[AutoRegister(typeof(IPlayerSystem))]
public class PlayerSystem : AbstractSystem, IPlayerSystem
{
    private const string SaveSlot = "Demo_Player_Save";

    protected override void OnInit() { }

    /// <summary>
    /// 保存游戏
    /// </summary>
    public void SaveGame()
    {
        var model = this.GetModel<IPlayerModel>();
        var data = model.ToSaveData();

        // 使用 SaveUtil 保存
        // 参数：槽位名, 数据对象, 备注
        SaveUtil.Save(SaveSlot, data, "Demo 2 Player Data");

        YLog.Info("游戏已保存！", "PlayerSystem");
    }

    /// <summary>
    /// 从存档加载游戏
    /// </summary>
    public void LoadGame()
    {
        // 使用 SaveUtil 读取
        var data = SaveUtil.Load<PlayerSaveData>(SaveSlot);

        if (data != null)
        {
            this.GetModel<IPlayerModel>().FromSaveData(data);
            YLog.Info("游戏已读取！", "PlayerSystem");
        }
        else
        {
            YLog.Warn("未找到存档！", "PlayerSystem");
        }
    }
}
