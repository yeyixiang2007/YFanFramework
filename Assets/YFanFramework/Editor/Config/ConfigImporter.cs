using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using YFan.Utils;

namespace YFan.Editor.Config
{
    /// <summary>
    /// 配置文件导入器
    /// 1. 读取 CSV 文件
    /// 2. 基于表头建立 字段名 -> 列索引 的映射字典
    /// 3. 基于类型行建立 字段名 -> 类型 的映射字典
    /// 4. 遍历所有数据行，基于映射字典赋值
    /// 5. 赋值并保存 ScriptableObject
    /// </summary>
    public static class ConfigImporter
    {
        /// <summary>
        /// 导入配置数据
        /// </summary>
        /// <param name="csvFile"></param>
        public static void ImportData(string csvFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(csvFile);
            string className = $"Game.Data.{fileName}";
            string tableClassName = $"Game.Data.{fileName}Table";

            // 反射获取类型
            Type dataType = GetTypeByString(className);
            Type tableType = GetTypeByString(tableClassName);

            if (dataType == null || tableType == null)
            {
                YLog.Error($"反射失败！未找到类: {className}。\n请先生成代码并等待编译完成。", "ConfigImporter");
                return;
            }

            // 准备 ScriptableObject
            if (!Directory.Exists(ConfigKeys.AssetPath)) Directory.CreateDirectory(ConfigKeys.AssetPath);
            string assetFullPath = Path.Combine(ConfigKeys.AssetPath, $"{fileName}.asset");

            ScriptableObject tableInstance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetFullPath);
            if (tableInstance == null)
            {
                tableInstance = ScriptableObject.CreateInstance(tableType);
                AssetDatabase.CreateAsset(tableInstance, assetFullPath);
            }

            // 读取 CSV
            string[] lines = File.ReadAllLines(csvFile, System.Text.Encoding.UTF8);
            if (lines.Length < 4)
            {
                YLog.Warn($"表格数据为空或不足 4 行。文件: {csvFile}");
            }

            // 建立 表头 -> 列索引 的映射字典

            // 获取字段名行 (第1行)
            string[] headerNames = lines.Length > 0 ? lines[0].Split(',') : new string[0];
            // 获取类型行 (第2行)
            string[] typeNames = lines.Length > 1 ? lines[1].Split(',') : new string[0];

            // 建立 字段名 -> 列索引 的映射字典
            Dictionary<string, int> columnMap = new Dictionary<string, int>();
            for (int i = 0; i < headerNames.Length; i++)
            {
                string key = headerNames[i].Trim();
                if (!columnMap.ContainsKey(key))
                {
                    columnMap.Add(key, i);
                }
            }

            // 准备列表容器
            FieldInfo listField = tableType.GetField("Items", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (listField == null)
            {
                YLog.Error($"在 {tableClassName} 中找不到 'Items' 字段！");
                return;
            }

            IList dataList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dataType));

            // 遍历数据行 (从第4行开始)
            int successCount = 0;
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 处理 CSV 逗号分隔 (简单 Split，不支持单元格内含逗号的情况)
                string[] values = line.Split(',');

                // 创建单行数据对象
                object dataObj = Activator.CreateInstance(dataType);

                // 获取数据类的所有字段
                var fields = dataType.GetFields(BindingFlags.Instance | BindingFlags.Public);

                // 遍历所有字段，基于名称赋值
                foreach (FieldInfo fieldInfo in fields)
                {
                    // 根据 C# 字段名，去 CSV 表头里找对应的列索引
                    if (columnMap.TryGetValue(fieldInfo.Name, out int colIndex))
                    {
                        // 安全检查：防止 CSV 某一行数据缺失导致越界
                        if (colIndex >= values.Length || colIndex >= typeNames.Length)
                            continue;

                        string valueStr = values[colIndex];
                        string typeStr = typeNames[colIndex];

                        try
                        {
                            object parsedValue = ParseValue(valueStr, typeStr);

                            // 容错处理：值类型不能赋 null
                            if (parsedValue == null && fieldInfo.FieldType.IsValueType)
                            {
                                parsedValue = Activator.CreateInstance(fieldInfo.FieldType);
                            }

                            fieldInfo.SetValue(dataObj, parsedValue);
                        }
                        catch (Exception ex)
                        {
                            YLog.Error($"赋值失败！行: {i + 1}, 字段: {fieldInfo.Name} (列{colIndex}), 值: {valueStr}\n错误: {ex.Message}", "ConfigImporter");
                        }
                    }
                    else
                    {
                        // CSV 里没有这个字段（可能是新生成的代码，但 Excel 还没加列），给个警告或忽略
                        // YLog.Warn($"CSV 中缺少字段: {fieldInfo.Name}，将使用默认值。");
                    }
                }

                dataList.Add(dataObj);
                successCount++;
            }

            // 赋值并保存
            listField.SetValue(tableInstance, dataList);
            EditorUtility.SetDirty(tableInstance);
            AssetDatabase.SaveAssets();

            YLog.Info($"导入成功！共 {successCount} 条数据。\n文件: {assetFullPath}", "ConfigImporter");
        }

        /// <summary>
        /// 根据字符串解析值为指定类型
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static object ParseValue(string value, string type)
        {
            if (string.IsNullOrEmpty(value)) return null;
            type = type.Trim();
            value = value.Trim(); // 去除单元格可能存在的空格

            try
            {
                // List<T> 解析
                if (type.StartsWith("List<") && type.EndsWith(">"))
                {
                    string elementTypeStr = type.Substring(5, type.Length - 6);
                    Type elementType = GetTypeByString(elementTypeStr);

                    if (elementType == null)
                    {
                        switch (elementTypeStr.ToLower())
                        {
                            case "int": elementType = typeof(int); break;
                            case "float": elementType = typeof(float); break;
                            case "string": elementType = typeof(string); break;
                            case "bool": elementType = typeof(bool); break;
                            case "vector3": elementType = typeof(Vector3); break;
                            case "vector2": elementType = typeof(Vector2); break;
                        }
                    }

                    if (elementType == null) return null;

                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = (IList)Activator.CreateInstance(listType);

                    string[] items = value.Split('|');
                    foreach (var item in items)
                    {
                        if (string.IsNullOrWhiteSpace(item)) continue;
                        object elementValue = ParseValue(item, elementTypeStr);
                        // 如果 List 元素解析失败，这里也会得到 null，需要注意
                        if (elementValue != null) list.Add(elementValue);
                    }
                    return list;
                }

                if (type.EndsWith("[]"))
                {
                    string elementType = type.Substring(0, type.Length - 2);
                    return ParseValue(value, $"List<{elementType}>");
                }

                switch (type.ToLower())
                {
                    case "int":
                        if (int.TryParse(value, out int iRes)) return iRes;
                        return 0; // 解析失败给默认值
                    case "float":
                        if (float.TryParse(value, out float fRes)) return fRes;
                        return 0f;
                    case "bool":
                        if (value == "1" || value.ToLower() == "true") return true;
                        return false;
                    case "string": return value;
                    case "vector3":
                        string[] v3 = value.Split(':');
                        return new Vector3(float.Parse(v3[0]), float.Parse(v3[1]), float.Parse(v3[2]));
                    case "vector2":
                        string[] v2 = value.Split(':');
                        return new Vector2(float.Parse(v2[0]), float.Parse(v2[1]));
                    default: return value;
                }
            }
            catch
            {
                // 仅返回 null，让外层决定是否报错或给默认值
                return null;
            }
        }

        /// <summary>
        /// 根据字符串获取类型
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private static Type GetTypeByString(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }
    }
}
