namespace YFan
{
    /// <summary>
    /// 配置键名常量
    /// </summary>
    public static class ConfigKeys
    {
        // --- MonoUtil 配置 ---
        public const string MonoUtilRuntime = "[YFan.MonoUtil]"; // 全局 MonoUtil 单例，用于运行时添加 MonoBehaviour

        // --- SaveUtil 配置 ---
        public const string SaveRootDir = "Saves"; // 存档根目录
        public const string SaveMetaExt = ".meta"; // 元数据文件后缀
        public const string SaveDataExt = ".dat"; // 数据文件后缀

        // --- BinaryUtil 配置 ---
        public const string BinarySecretKey = "YFan_Binary_Secret"; // 默认密钥
        public const string BinaryIV = "YFan_Binary_IV"; // 默认 IV

        // --- InputSystem 配置 ---
        public const string InputAssetKey = "Input_GlobalActions"; // Addressables 中 InputActions 资源的 Key
        public const string DefaultInputMapName = "Gameplay"; // 默认激活的 ActionMap 名称
        public const string InputSettingSaveSlot = "System_InputSettings"; // 存档槽位名
        public const string InputSettingSaveNote = "Global Input Settings"; // 存档描述

        // --- AudioSystem 配置 ---
        public const string AudioSystemRuntime = "AudioSystem_Runtime"; // 运行时 AudioSystem 单例
        public const string AudioSettingSaveSlot = "System_AudioSettings"; // 存档槽位名
        public const string AudioSettingSaveNote = "Global Audio Settings"; // 存档描述
        public const string AudioSettingBGMRoot = "Root_BGM"; // BGM 音频设置根节点
        public const string AudioSettingVoiceRoot = "Root_Voice"; // 语音音频设置根节点
        public const string AudioSettingSFXPoolRoot = "Root_SFX_Pool"; // 音效池根节点
    }
}
