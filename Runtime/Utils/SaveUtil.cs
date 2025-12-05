using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using YFan.Runtime.Modules;

namespace YFan.Runtime.Utils
{
    /// <summary>
    /// 存档模式
    /// </summary>
    public enum SaveMode
    {
        Json, // 明文 JSON (适合开发调试)
        Binary // 加密二进制 (适合发布版本)
    }

    /// <summary>
    /// 存档元数据 (用于在读档列表显示)
    /// </summary>
    [Serializable]
    public class SaveMetadata
    {
        public string FileName; // 文件名
        public string SlotName; // 槽位名
        public string Timestamp; // 存档时间
        public long PlayTimeSeconds; // 总游玩时长
        public string CustomNote; // 自定义备注 (如 "第3章 - 城堡")
        public string ScreenshotPath; // 截图路径 (可选)
    }

    /// <summary>
    /// 强大的存档管理系统
    /// * 支持 多槽位、元数据管理
    /// * 支持 JSON/Binary 模式自动切换
    /// </summary>
    public static class SaveUtil
    {
        // --- 配置区 ---

        // 默认存档模式 (建议在发布时改为 Binary)
#if UNITY_EDITOR
        public static SaveMode Mode = SaveMode.Json;
#else
        public static SaveMode Mode = SaveMode.Binary;
#endif

        // 存档根目录
        private static string RootDir => Path.Combine(Application.persistentDataPath, ConfigKeys.SaveRootDir);

        #region 核心 API

        /// <summary>
        /// 保存游戏数据到指定槽位
        /// </summary>
        /// <param name="slotName">槽位名 (如 "AutoSave", "Slot_1")</param>
        /// <param name="data">游戏数据对象</param>
        /// <param name="note">存档备注</param>
        public static void Save<T>(string slotName, T data, string note = "")
        {
            if (string.IsNullOrEmpty(slotName) || data == null) return;
            EnsureDir();

            string baseName = GetSaveFileName(slotName);
            string dataPath = Path.Combine(RootDir, baseName + ConfigKeys.SaveDataExt);
            string metaPath = Path.Combine(RootDir, baseName + ConfigKeys.SaveMetaExt);

            try
            {
                // 保存核心数据
                if (Mode == SaveMode.Json)
                {
                    string json = JSONUtil.ToJson(data, true); // 美化格式
                    File.WriteAllText(dataPath, json);
                }
                else
                {
                    // 二进制流程: 对象 -> Bytes -> 压缩 -> 加密 -> 写入
                    byte[] raw = BinaryUtil.ToBytes(data);
                    byte[] compressed = BinaryUtil.Compress(raw);
                    byte[] encrypted = BinaryUtil.Encrypt(compressed);
                    BinaryUtil.WriteToFile(dataPath, encrypted);
                }

                // 生成并保存元数据
                var meta = new SaveMetadata
                {
                    FileName = baseName,
                    SlotName = slotName,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    CustomNote = note,
                    // ScreenshotPath = ... (截图逻辑需额外实现)
                };

                // 元数据始终存为明文 JSON，方便快速读取列表
                File.WriteAllText(metaPath, JSONUtil.ToJson(meta));

                YLog.Info($"[{slotName}] 存档成功! ({Mode})", "SaveUtil");
            }
            catch (Exception e)
            {
                YLog.Error($"[{slotName}] 存档失败: {e.Message}", "SaveUtil");
            }
        }

        /// <summary>
        /// 从指定槽位加载数据
        /// </summary>
        public static T Load<T>(string slotName)
        {
            string baseName = GetSaveFileName(slotName);
            string dataPath = Path.Combine(RootDir, baseName + ConfigKeys.SaveDataExt);

            if (!File.Exists(dataPath))
            {
                YLog.Warn($"存档不存在: {slotName}", "SaveUtil");
                return default;
            }

            try
            {
                // 尝试判断文件头来自动识别是 JSON 还是 Binary (简单容错)
                // 这里为了简单，直接根据当前 Mode 尝试读取，失败则尝试另一种

                // 尝试按当前 Mode 读取
                return ReadDataInternal<T>(dataPath, Mode);
            }
            catch
            {
                // 如果失败，尝试另一种模式 (防止改了 Mode 后读不出旧存档)
                var fallbackMode = Mode == SaveMode.Json ? SaveMode.Binary : SaveMode.Json;
                YLog.Warn($"按 {Mode} 读取失败，尝试使用 {fallbackMode}...", "SaveUtil");
                return ReadDataInternal<T>(dataPath, fallbackMode);
            }
        }

        /// <summary>
        /// 获取所有存档的元数据列表 (用于显示读档界面)
        /// </summary>
        public static List<SaveMetadata> GetAllSaves()
        {
            EnsureDir();
            var list = new List<SaveMetadata>();
            var files = Directory.GetFiles(RootDir, "*" + ConfigKeys.SaveMetaExt);

            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var meta = JSONUtil.FromJson<SaveMetadata>(json);
                    if (meta != null) list.Add(meta);
                }
                catch { }
            }

            // 按时间倒序排列 (最新的在最前)
            return list.OrderByDescending(x => x.Timestamp).ToList();
        }

        /// <summary>
        /// 删除指定存档
        /// </summary>
        public static void Delete(string slotName)
        {
            string baseName = GetSaveFileName(slotName);
            string dataPath = Path.Combine(RootDir, baseName + ConfigKeys.SaveDataExt);
            string metaPath = Path.Combine(RootDir, baseName + ConfigKeys.SaveMetaExt);

            if (File.Exists(dataPath)) File.Delete(dataPath);
            if (File.Exists(metaPath)) File.Delete(metaPath);

            YLog.Info($"存档已删除: {slotName}", "SaveUtil");
        }

        /// <summary>
        /// 删除所有存档
        /// </summary>
        public static void DeleteAll()
        {
            if (Directory.Exists(RootDir))
            {
                Directory.Delete(RootDir, true);
                YLog.Info("所有存档已清空", "SaveUtil");
            }
        }

        #endregion

        #region 内部逻辑

        private static T ReadDataInternal<T>(string path, SaveMode mode)
        {
            if (mode == SaveMode.Json)
            {
                string json = File.ReadAllText(path);
                return JSONUtil.FromJson<T>(json);
            }
            else
            {
                // 二进制流程: 读取 -> 解密 -> 解压 -> 对象
                byte[] encrypted = BinaryUtil.ReadFromFile(path);
                byte[] compressed = BinaryUtil.Decrypt(encrypted);
                byte[] raw = BinaryUtil.Decompress(compressed);
                return BinaryUtil.ToObject<T>(raw);
            }
        }

        private static void EnsureDir()
        {
            if (!Directory.Exists(RootDir)) Directory.CreateDirectory(RootDir);
        }

        private static string GetSaveFileName(string slotName)
        {
            // 简单处理文件名非法字符
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                slotName = slotName.Replace(c, '_');
            }
            return $"Save_{slotName}";
        }

        internal static void Save(object saveSlotName, InputSettingsData settingsData, string inputSettingSaveNote)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
