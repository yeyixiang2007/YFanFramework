using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace YFan.Utils
{
    /// <summary>
    /// 基于 Newtonsoft.Json 的高性能 JSON 工具
    /// + 自动处理 Unity 常用类型 (Vector3, Color 等) 的序列化格式
    /// + 统一异常处理，对接 YLog
    /// + 支持忽略循环引用
    /// </summary>
    public static class JSONUtil
    {
        // 公开配置，供 BinaryUtil 的 BSON 序列化复用
        public static readonly JsonSerializerSettings DefaultSettings;

        // 格式化配置 (用于 Debug 输出)
        private static readonly JsonSerializerSettings _prettySettings;

        static JSONUtil()
        {
            DefaultSettings = new JsonSerializerSettings
            {
                // 忽略循环引用 (防止序列化 GameObject/Transform 时死循环)
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                // 忽略空值 (节省流量和存储空间)
                NullValueHandling = NullValueHandling.Ignore,
                // 允许解析私有字段 (如果属性加了 [JsonProperty])
                ContractResolver = new DefaultContractResolver(),
                // 自动转换枚举为字符串 (可读性更好)
                Converters = { new StringEnumConverter() }
            };

            _prettySettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented, // 开启缩进
                Converters = { new StringEnumConverter() }
            };
        }

        #region 序列化 (Object -> JSON)

        /// <summary>
        /// 将对象转为 JSON 字符串
        /// </summary>
        public static string ToJson(object obj, bool pretty = false)
        {
            if (obj == null) return null;

            try
            {
                return JsonConvert.SerializeObject(obj, pretty ? _prettySettings : DefaultSettings);
            }
            catch (Exception e)
            {
                YLog.Error($"序列化失败: {e.Message}", "JsonUtil");
                return null;
            }
        }

        #endregion

        #region 反序列化 (JSON -> Object)

        /// <summary>
        /// 将 JSON 字符串转为对象 T
        /// </summary>
        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(json, DefaultSettings);
            }
            catch (Exception e)
            {
                YLog.Error($"反序列化失败: {e.Message}\nJSON: {json}", "JsonUtil");
                return default;
            }
        }

        /// <summary>
        /// 将 JSON 字符串转为对象 (Type)
        /// </summary>
        public static object FromJson(string json, Type type)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonConvert.DeserializeObject(json, type, DefaultSettings);
            }
            catch (Exception e)
            {
                YLog.Error($"反序列化失败: {e.Message}\nJSON: {json}", "JsonUtil");
                return null;
            }
        }

        /// <summary>
        /// 将 JSON 数据填充到一个已存在的对象中 (覆盖数据)
        /// + 常用于：热更配置覆盖默认配置
        /// </summary>
        public static void PopulateObject(string json, object target)
        {
            if (string.IsNullOrEmpty(json) || target == null) return;

            try
            {
                JsonConvert.PopulateObject(json, target, DefaultSettings);
            }
            catch (Exception e)
            {
                YLog.Error($"填充对象失败: {e.Message}\nJSON: {json}", "JsonUtil");
            }
        }

        #endregion

        #region 扩展功能

        /// <summary>
        /// 深度克隆一个对象 (通过 序列化 -> 反序列化 实现)
        /// + 性能一般，但非常方便，且能实现深拷贝
        /// </summary>
        public static T DeepCopy<T>(T source)
        {
            if (source == null) return default;
            string json = ToJson(source);
            return FromJson<T>(json);
        }

        /// <summary>
        /// 检查字符串是否为合法的 JSON 格式
        /// </summary>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                // 尝试解析，不报错即为合法
                var obj = Newtonsoft.Json.Linq.JToken.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
