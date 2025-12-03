using System;
using System.Collections.Generic;
using UnityEngine;

namespace YFan.Base
{
    [Serializable]
    public class Test
    {
        /// <summary> 编号 </summary>
        public int Id;
        /// <summary> 名字 </summary>
        public string Name;
        /// <summary> 描述 </summary>
        public string Description;
    }

    [CreateAssetMenu(menuName = "YFan/Config/TestTable")]
    public class TestTable : ConfigBase<int, Test>
    {
        public override void Init()
        {
            _dict = new Dictionary<int, Test>();
            foreach (var item in Items)
            {
                if (!_dict.ContainsKey(item.Id)) _dict.Add(item.Id, item);
            }
        }
    }
}
