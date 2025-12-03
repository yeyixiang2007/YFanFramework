using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using YFan.Utils;

namespace YFan.Editor.Config
{
    /// <summary>
    /// 配置文件导入器
    /// </summary>
    public static class ConfigImporter
    {
        /// <summary>
        /// 导入配置文件数据
        /// </summary>
        /// <param name="csvFile">CSV 文件路径</param>
        public static void ImportData(string csvFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(csvFile);
            string className = $"YFan.Base.{fileName}";
            string tableClassName = $"YFan.Base.{fileName}Table";

            Type dataType = GetTypeByString(className);
            Type tableType = GetTypeByString(tableClassName);

            if (dataType == null || tableType == null)
            {
                YLog.Error($"反射失败！未找到类: {className}。\n请先生成代码并等待编译完成。", "ConfigImporter");
                return;
            }

            if (!Directory.Exists(ConfigKeys.AssetPath)) Directory.CreateDirectory(ConfigKeys.AssetPath);
            string assetFullPath = Path.Combine(ConfigKeys.AssetPath, $"{fileName}.asset");

            ScriptableObject tableInstance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetFullPath);
            if (tableInstance == null)
            {
                tableInstance = ScriptableObject.CreateInstance(tableType);
                AssetDatabase.CreateAsset(tableInstance, assetFullPath);
            }

            string[] lines = File.ReadAllLines(csvFile, System.Text.Encoding.UTF8);
            if (lines.Length < 4)
            {
                YLog.Warn($"表格数据为空或不足 4 行。文件: {csvFile}");
            }

            string[] headerNames = lines.Length > 0 ? lines[0].Split(',') : new string[0];
            string[] typeNames = lines.Length > 1 ? lines[1].Split(',') : new string[0];

            Dictionary<string, int> columnMap = new Dictionary<string, int>();
            for (int i = 0; i < headerNames.Length; i++)
            {
                string key = headerNames[i].Trim();
                if (!columnMap.ContainsKey(key))
                {
                    columnMap.Add(key, i);
                }
            }

            FieldInfo listField = tableType.GetField("Items", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (listField == null)
            {
                YLog.Error($"在 {tableClassName} 中找不到 'Items' 字段！");
                return;
            }

            IList dataList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dataType));

            int successCount = 0;
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] values = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                object dataObj = Activator.CreateInstance(dataType);
                var fields = dataType.GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (FieldInfo fieldInfo in fields)
                {
                    if (columnMap.TryGetValue(fieldInfo.Name, out int colIndex))
                    {
                        if (colIndex >= values.Length || colIndex >= typeNames.Length)
                            continue;

                        string valueStr = values[colIndex];
                        string typeStr = typeNames[colIndex];

                        try
                        {
                            object parsedValue = ParseValue(valueStr, typeStr);

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
                }

                dataList.Add(dataObj);
                successCount++;
            }

            listField.SetValue(tableInstance, dataList);
            EditorUtility.SetDirty(tableInstance);
            AssetDatabase.SaveAssets();

            YLog.Info($"导入成功！共 {successCount} 条数据。\n文件: {assetFullPath}", "ConfigImporter");
        }

        /// <summary>
        /// 解析 CSV 中的值为指定类型
        /// </summary>
        /// <param name="value">CSV 中的值</param>
        /// <param name="type">目标类型</param>
        /// <returns>解析后的对象</returns>
        private static object ParseValue(string value, string type)
        {
            if (string.IsNullOrEmpty(value)) return null;
            type = type.Trim();
            value = value.Trim();

            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 1)
            {
                value = value.Substring(1, value.Length - 2);
            }

            try
            {
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
                        return 0;
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
                return null;
            }
        }

        /// <summary>
        /// 根据字符串获取类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>对应的类型</returns>
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
