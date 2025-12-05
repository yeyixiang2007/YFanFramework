using System.Collections.Generic;
using UnityEngine;

namespace YFan.Runtime.Base
{
    /// <summary>
    /// 所有配置表的基类 (ScriptableObject)
    /// </summary>
    /// <typeparam name="TKey">ID 的类型</typeparam>
    /// <typeparam name="TValue">数据对象的类型</typeparam>
    public abstract class ConfigBase<TKey, TValue> : ScriptableObject where TValue : class
    {
        public List<TValue> Items = new List<TValue>();
        protected Dictionary<TKey, TValue> _dict;

        public virtual void Init()
        {
            _dict = new Dictionary<TKey, TValue>();
        }

        public TValue Get(TKey id)
        {
            if (_dict == null) Init();
            if (id == null) return null;
            if (_dict.TryGetValue(id, out var item)) return item;
            return null;
        }

        public List<TValue> GetAll() => Items;
    }
}
