using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class test
    {
        /// <summary> 1 </summary>
        public int Id;
        /// <summary> 叶以翔 </summary>
        public string Name;
        /// <summary> 1 </summary>
        public int Sex;
        /// <summary> 15359029780 </summary>
        public string Phone;
    }

    [CreateAssetMenu(menuName = "YFan/Config/testTable")]
    public class testTable : ConfigBase<test>
    {
        public override void Init()
        {
            _dict = new Dictionary<int, test>();
            foreach (var item in Items)
            {
                // 约定：第一列必须是 Id (int)
                if (!_dict.ContainsKey(item.Id)) _dict.Add(item.Id, item);
            }
        }
    }
}
