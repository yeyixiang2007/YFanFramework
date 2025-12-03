using System.Collections.Generic;
using UnityEngine;

namespace YFan.Base
{
    /// <summary>
    /// 所有配置表的基类 (ScriptableObject)
    /// </summary>
    public abstract class ConfigBase<T> : ScriptableObject where T : class
    {
        // 存储列表 (Unity 原生序列化支持 List)
        public List<T> Items = new List<T>();

        // 运行时字典索引 (ID -> Data)，提供 O(1) 查找
        protected Dictionary<int, T> _dict;

        public virtual void Init()
        {
            _dict = new Dictionary<int, T>();
            // 假设每行数据都有个 Id 字段，利用反射或者约定构建字典
            // 为了高性能，实际生成代码时会重写这个 Init 方法，避免反射
        }

        public T Get(int id)
        {
            if (_dict == null) Init();
            if (_dict.TryGetValue(id, out var item)) return item;
            return null;
        }

        public List<T> GetAll() => Items;
    }
}
