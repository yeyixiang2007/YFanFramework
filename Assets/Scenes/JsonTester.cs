using System;
using System.Collections.Generic;
using UnityEngine;
using YFan.Attributes;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

public class JSONTester : AbstractController
{
    // --- 输入区域 ---
    [YBoxGroup("数据准备")]
    [YHelpBox("点击生成按钮创建一个测试对象", YMessageType.Info)]
    [YReadOnly]
    [SerializeField]
    private TestUserData _sourceData;

    // --- 输出区域 ---
    [YSpace(10)]
    [YBoxGroup("JSON 结果")]
    [YReadOnly]
    [SerializeField]
    private string _jsonResult;

    [YBoxGroup("JSON 结果")]
    [YReadOnly]
    [SerializeField]
    private TestUserData _deserializedData;

    private void Start()
    {
        YLog.Info("JSONUtil 测试器已启动", "Tester");
    }

    // --- 功能测试 ---

    [YButton("1. 生成测试数据", 35)]
    [YColor("#88FF88")]
    private void GenerateData()
    {
        _sourceData = new TestUserData
        {
            ID = 10086,
            Name = "YFan_Dev",
            Scores = new float[] { 99.5f, 88.0f, 100f },
            Position = new Vector3(1.5f, 2.5f, 3.5f),
            Type = UserType.Admin,
            MetaData = new Dictionary<string, string>
            {
                { "LoginTime", DateTime.Now.ToString() },
                { "Version", "1.0.0" }
            }
        };
        YLog.Info("测试数据已生成", "JSON");
    }

    [YButton("2. 序列化 (ToJson)", 35)]
    private void TestSerialize()
    {
        if (_sourceData == null) GenerateData();

        // 默认压缩格式
        string compact = JSONUtil.ToJson(_sourceData);
        YLog.Info($"Compact: {compact}", "JSON");

        // 美化格式
        _jsonResult = JSONUtil.ToJson(_sourceData, true);
        YLog.Info("已序列化为美化 JSON", "JSON");
    }

    [YButton("3. 反序列化 (FromJson)", 35)]
    private void TestDeserialize()
    {
        if (string.IsNullOrEmpty(_jsonResult))
        {
            YLog.Error("请先执行序列化！", "JSON");
            return;
        }

        _deserializedData = JSONUtil.FromJson<TestUserData>(_jsonResult);

        // 验证数据
        if (_deserializedData.Position == _sourceData.Position && _deserializedData.Name == _sourceData.Name)
        {
            YLog.Info("反序列化验证成功！数据一致。", "JSON");
        }
        else
        {
            YLog.Error("反序列化数据不匹配！", "JSON");
        }
    }

    [YButton("4. 深度拷贝测试 (DeepCopy)", 35)]
    [YColor("#8888FF")]
    private void TestDeepCopy()
    {
        if (_sourceData == null) GenerateData();

        var copy = JSONUtil.DeepCopy(_sourceData);

        // 修改原数据，看副本是否受影响
        _sourceData.ID = 999;

        if (copy.ID == 10086)
        {
            YLog.Info($"深度拷贝成功！原ID变为:{_sourceData.ID}, 副本ID仍为:{copy.ID}", "JSON");
        }
        else
        {
            YLog.Error("深度拷贝失败！副本受影响。", "JSON");
        }
    }

    [YButton("5. 错误 JSON 测试", 35)]
    [YColor(1f, 0.6f, 0.6f)]
    private void TestError()
    {
        string badJson = "{ \"ID\": 123, \"Name\": ... "; // 缺少右括号
        var result = JSONUtil.FromJson<TestUserData>(badJson);

        if (result == null)
        {
            YLog.Info("成功捕获了错误的 JSON，未导致崩溃。", "JSON");
        }
    }

    // 定义一些用于测试的数据结构
    [Serializable]
    public class TestUserData
    {
        public int ID;
        public string Name;
        public float[] Scores;
        public Vector3 Position; // Unity 原生类型
        public UserType Type;
        public Dictionary<string, string> MetaData;
    }

    public enum UserType
    {
        Guest,
        Admin,
        Player
    }
}
