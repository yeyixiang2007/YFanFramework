using System;

namespace YFan.Modules
{
    /// <summary>
    /// 用于 SaveUtil 保存的输入配置数据
    /// </summary>
    [Serializable]
    public class InputSettingsData
    {
        // Unity New Input System 导出的 JSON 覆盖数据
        public string OverridesJson;

        // 鼠标灵敏度等其他输入相关设置
        public float MouseSensitivity = 1.0f;
        public bool InvertY = false;
    }
}
