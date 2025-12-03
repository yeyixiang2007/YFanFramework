using System.IO;
using System.Text;
using YFan.Utils;

namespace YFan.Editor.Config
{
    /// <summary>
    /// 配置文件代码生成器
    /// </summary>
    public static class ConfigCodeGen
    {
        /// <summary>
        /// 生成配置文件的代码
        /// </summary>
        /// <param name="csvFile">配置文件的路径</param>
        public static void GenerateCode(string csvFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(csvFile);
            string[] lines = File.ReadAllLines(csvFile);

            if (lines.Length < 3) return;

            string[] names = lines[0].Split(',');
            string[] types = lines[1].Split(',');
            string[] comments = lines[2].Split(',');

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
            sb.AppendLine($"    public class {fileName}Table : ConfigBase<{fileName}>");
            sb.AppendLine("    {");
            sb.AppendLine("        public override void Init()");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dict = new Dictionary<int, {fileName}>();");
            sb.AppendLine("            foreach (var item in Items)");
            sb.AppendLine("            {");
            sb.AppendLine("                // 约定：第一列必须是 Id (int)");
            sb.AppendLine("                if (!_dict.ContainsKey(item.Id)) _dict.Add(item.Id, item);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");

            sb.AppendLine("}"); // End Namespace

            // 写入文件
            if (!Directory.Exists(ConfigKeys.CodePath)) Directory.CreateDirectory(ConfigKeys.CodePath);
            File.WriteAllText(Path.Combine(ConfigKeys.CodePath, $"{fileName}Table.cs"), sb.ToString());

            YLog.Info($"代码生成完毕: {fileName}Table.cs", "ConfigCodeGen");
        }

        private static string ParseType(string csvType)
        {
            csvType = csvType.Trim();

            // 持 List<int> 或 int[] 写法
            if (csvType.StartsWith("List<") && csvType.EndsWith(">"))
            {
                return csvType;
            }
            if (csvType.EndsWith("[]"))
            {
                string elementType = csvType.Substring(0, csvType.Length - 2);
                return $"List<{ParseType(elementType)}>";
            }

            // 其他基础类型
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
