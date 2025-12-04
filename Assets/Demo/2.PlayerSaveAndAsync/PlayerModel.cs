using QFramework;
using UnityEngine;
using YFan.Attributes;
using System.Collections.Generic;

/// <summary>
/// 玩家存档数据
/// </summary>
public class PlayerSaveData
{
    public List<float> Position;
    public List<float> Color;
}

/// <summary>
/// 玩家模型
/// </summary>
public interface IPlayerModel : IModel
{
    BindableProperty<Vector3> Position { get; }
    BindableProperty<Color> SkinColor { get; }

    PlayerSaveData ToSaveData();
    void FromSaveData(PlayerSaveData data);
}

[AutoRegister(typeof(IPlayerModel))]
public class PlayerModel : AbstractModel, IPlayerModel
{
    public BindableProperty<Vector3> Position { get; } = new BindableProperty<Vector3>(Vector3.zero);
    public BindableProperty<Color> SkinColor { get; } = new BindableProperty<Color>(Color.white);

    protected override void OnInit()
    {
        SkinColor.Value = new Color(Random.value, Random.value, Random.value);
    }

    /// <summary>
    /// 转存为安全的数据格式
    /// </summary>
    public PlayerSaveData ToSaveData()
    {
        var pos = Position.Value;
        var col = SkinColor.Value;

        return new PlayerSaveData
        {
            // 将 Vector3 转为 List<float>
            Position = new List<float> { pos.x, pos.y, pos.z },

            // 将 Color 转为 List<float>
            Color = new List<float> { col.r, col.g, col.b, col.a }
        };
    }

    /// <summary>
    /// 从安全格式恢复
    /// </summary>
    public void FromSaveData(PlayerSaveData data)
    {
        if (data == null) return;

        // 恢复 Vector3
        if (data.Position != null && data.Position.Count >= 3)
        {
            Position.Value = new Vector3(data.Position[0], data.Position[1], data.Position[2]);
        }

        // 恢复 Color
        if (data.Color != null && data.Color.Count >= 4)
        {
            SkinColor.Value = new Color(data.Color[0], data.Color[1], data.Color[2], data.Color[3]);
        }
    }
}
