namespace YFan.Editor
{
    /// <summary>
    /// 配置键名常量
    /// </summary>
    public static class ConfigKeys
    {
        // --- 配置文件路径 ---
        public static string CsvPath = "Assets/Configs/Csv/"; // CSV 文件路径
        public static string CodePath = "Assets/Scripts/Gen/Data"; // 生成的代码路径
        public static string AssetPath = "Assets/Configs/Assets"; // 生成的资产路径
        public const string PendingFilesKey = "YFan_Config_PendingFiles"; // 待处理文件键名

        // 默认 CSV 模板内容
        public static string NewCsvTemplate =
            "Id,Name,Description\n" +
            "int,string,string\n" +
            "编号,名字,描述\n" +
            "1001,TestItem,测试配置";
    }
}

