using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using YFan.Utils;

namespace YFan.Editor.Config
{
    /// <summary>
    /// 配置文件代码生成器
    /// </summary>
    public static class ConfigCodeGen
    {
        /// <summary>
        /// 生成配置文件的 C# 代码
        /// </summary>
        /// <param name="csvFile">CSV 文件路径</param>
        public static void GenerateCode(string csvFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(csvFile);
            string[] lines = File.ReadAllLines(csvFile);

            if (lines.Length < 3) return;

            string[] names = lines[0].Split(',');
            string[] types = lines[1].Split(',');
            string[] comments = lines[2].Split(',');

            // 获取 Key 的信息 (约定第一列为 Key)
            string keyName = names[0].Trim();
            string keyTypeRaw = types[0].Trim();
            string keyType = ParseType(keyTypeRaw);

            StringBuilder sb = new StringBuilder();

            // --- Header ---
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace YFan.Base");
            sb.AppendLine("{");

            // --- 生成单行数据结构 ---
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public class {fileName}");
            sb.AppendLine("    {");

            for (int i = 0; i < names.Length; i++)
            {
                string type = ParseType(types[i]);
                sb.AppendLine($"        /// <summary> {comments[i]} </summary>");
                sb.AppendLine($"        public {type} {names[i]};");
            }
            sb.AppendLine("    }");
            sb.AppendLine();

            // --- 生成容器类  ---
            sb.AppendLine($"    [CreateAssetMenu(menuName = \"YFan/Config/{fileName}Table\")]");
            sb.AppendLine($"    public class {fileName}Table : ConfigBase<{keyType}, {fileName}>");
            sb.AppendLine("    {");
            sb.AppendLine("        public override void Init()");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dict = new Dictionary<{keyType}, {fileName}>();");
            sb.AppendLine("            foreach (var item in Items)");
            sb.AppendLine("            {");
            sb.AppendLine($"                if (!_dict.ContainsKey(item.{keyName})) _dict.Add(item.{keyName}, item);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");

            sb.AppendLine("}"); // End Namespace

            if (!Directory.Exists(ConfigKeys.CodePath)) Directory.CreateDirectory(ConfigKeys.CodePath);
            File.WriteAllText(Path.Combine(ConfigKeys.CodePath, $"{fileName}Table.cs"), sb.ToString());

            MarkForImport(csvFile);

            YLog.Info($"代码生成完毕: {fileName}Table.cs (Key: {keyType})", "ConfigCodeGen");
        }

        /// <summary>
        /// 标记文件为需要导入
        /// </summary>
        /// <param name="csvFile">CSV 文件路径</param>
        private static void MarkForImport(string csvFile)
        {
            string current = EditorPrefs.GetString(ConfigKeys.PendingFilesKey, "");
            if (!current.Split(';').Contains(csvFile))
            {
                if (!string.IsNullOrEmpty(current)) current += ";";
                current += csvFile;
                EditorPrefs.SetString(ConfigKeys.PendingFilesKey, current);
            }
        }

        /// <summary>
        /// 当脚本重新加载时，执行挂起的导入任务
        /// </summary>
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!EditorPrefs.HasKey(ConfigKeys.PendingFilesKey)) return;

            string fileString = EditorPrefs.GetString(ConfigKeys.PendingFilesKey, "");
            EditorPrefs.DeleteKey(ConfigKeys.PendingFilesKey);

            if (string.IsNullOrEmpty(fileString)) return;

            EditorApplication.delayCall += () =>
            {
                var files = fileString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                bool hasImported = false;

                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        ConfigImporter.ImportData(file);
                        hasImported = true;
                    }
                }

                if (hasImported)
                {
                    AssetDatabase.Refresh();
                    YLog.Info("编译完成，已自动执行挂起的数据导入任务。", "ConfigCodeGen");
                }
            };
        }

        /// <summary>
        /// 根据 CSV 中的类型字符串解析为 C# 类型
        /// </summary>
        /// <param name="csvType">CSV 中的类型字符串</param>
        /// <returns>解析后的 C# 类型字符串</returns>
        private static string ParseType(string csvType)
        {
            csvType = csvType.Trim();

            if (csvType.StartsWith("List<") && csvType.EndsWith(">"))
            {
                return csvType;
            }
            if (csvType.EndsWith("[]"))
            {
                string elementType = csvType.Substring(0, csvType.Length - 2);
                return $"List<{ParseType(elementType)}>";
            }

            switch (csvType.ToLower())
            {
                case "int": return "int";
                case "float": return "float";
                case "bool": return "bool";
                case "string": return "string";
                case "vector3": return "Vector3";
                case "vector2": return "Vector2";
                default: return "string";
            }
        }
    }
}
