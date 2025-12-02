using UnityEngine;
using YFan.Attributes;
using YFan.Runtime.Base.Abstract;

public class UITester : AbstractController
{
    // --- 1. 基础分组 ---
    [YBoxGroup("基础信息")]
    [YHelpBox("这是玩家的唯一标识", YMessageType.Info)]
    public string PlayerID = "P_1001";

    [YBoxGroup("基础信息")]
    public string Nickname = "YFanUser";

    // --- 2. 战斗分组 (带 Range 和 Color) ---
    [YSpace(15)] // 组与组之间加点空隙
    [YBoxGroup("战斗属性")]
    [YRange(0, 100)]
    [YColor("#88FF88")] // 绿色血条
    public float Health = 100;

    [YBoxGroup("战斗属性")]
    [YRange(0, 500)]
    [YColor("#8888FF")] // 蓝色蓝条
    public int Mana = 200;

    [YBoxGroup("战斗属性")]
    [YRange(1, 10)]
    public int Level = 1;

    // --- 3. 调试分组 (带 ShowIf) ---
    [YSpace(15)]
    [YBoxGroup("调试工具")]
    [YTitle("开关")]
    public bool IsDebugMode;

    [YBoxGroup("调试工具")]
    [YShowIf("IsDebugMode")]
    [YHelpBox("高风险操作！", YMessageType.Warning)]
    [YColor(1f, 0.5f, 0.5f)]
    public string AdminPassword;

    [YBoxGroup("调试工具")]
    [YShowIf("IsDebugMode")]
    [YRange(0, 5)]
    public float GameSpeed = 1.0f;

    // --- 4. 按钮 ---
    [YButton("一键满血", 40)]
    [YColor("#00FF00")]
    public void HealAll()
    {
        Health = 100;
        Mana = 500;
        Debug.Log("Healed!");
    }

    [YButton("删除存档", 30)]
    [YShowIf("IsDebugMode")]
    [YColor(1, 0, 0)]
    [YHelpBox("此操作不可逆", YMessageType.Error)]
    public void DeleteSave()
    {
        Debug.LogError("Save Deleted!");
    }
}
