using System;
using UnityEngine;
using YFan.Attributes;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

public class BinaryTester : AbstractController
{
    // --- 输入 ---
    [YBoxGroup("1. 数据准备")]
    public string InputString = "Hello YFanFramework! 这是一段测试数据。";

    [YBoxGroup("1. 数据准备")]
    [YReadOnly]
    [SerializeField]
    private int _originalSize = 0;

    // --- 中间状态 ---
    [YSpace(10)]
    [YBoxGroup("2. 处理过程")]
    [YReadOnly]
    [SerializeField]
    private byte[] _processedBytes;

    [YBoxGroup("2. 处理过程")]
    [YReadOnly]
    [SerializeField]
    private int _currentSize = 0;

    [YBoxGroup("2. 处理过程")]
    [YReadOnly]
    [SerializeField]
    private string _status = "Empty";

    // --- 输出 ---
    [YSpace(10)]
    [YBoxGroup("3. 还原结果")]
    [YReadOnly]
    [SerializeField]
    private string _restoredString;

    [YBoxGroup("3. 还原结果")]
    [YReadOnly]
    [SerializeField]
    private bool _isMatch;

    private void Start()
    {
        YLog.Info("BinaryUtil 测试器启动", "Tester");
        _originalSize = System.Text.Encoding.UTF8.GetByteCount(InputString);
    }

    // --- 功能测试按钮 ---

    [YButton("Step1: 转二进制 (ToBytes)", 35)]
    [YColor("#88FF88")]
    private void TestToBytes()
    {
        _processedBytes = BinaryUtil.ToBytes(InputString);
        UpdateStatus("Raw Bytes");
    }

    [YButton("Step2: 压缩 (Compress)", 35)]
    private void TestCompress()
    {
        if (_processedBytes == null) TestToBytes();

        // 只有数据量大时压缩才有效果，短字符串压缩反而可能变大(因为有头文件)
        _processedBytes = BinaryUtil.Compress(_processedBytes);
        UpdateStatus("Compressed");
    }

    [YButton("Step3: 加密 (Encrypt)", 35)]
    [YColor(1f, 0.6f, 0.6f)]
    private void TestEncrypt()
    {
        if (_processedBytes == null) TestToBytes();

        _processedBytes = BinaryUtil.Encrypt(_processedBytes, "MyPassword123");
        UpdateStatus("Encrypted");
    }

    [YButton("Step4: 写入文件", 35)]
    private void TestWriteFile()
    {
        if (_processedBytes == null) return;
        string path = System.IO.Path.Combine(Application.persistentDataPath, "test.bin");
        BinaryUtil.WriteToFile(path, _processedBytes);
        YLog.Info($"文件已写入: {path}", "Binary");
    }

    [YButton(">>> 逆向还原 (解密->解压->ToString)", 40)]
    [YColor("#8888FF")]
    private void TestRestore()
    {
        if (_processedBytes == null) return;

        byte[] temp = _processedBytes;
        string flow = _status;

        try
        {
            // 1. 如果是加密状态，先解密
            if (flow.Contains("Encrypted"))
            {
                temp = BinaryUtil.Decrypt(temp, "MyPassword123");
                YLog.Info("解密完成", "Binary");
            }

            // 2. 如果是压缩状态，解压
            if (flow.Contains("Compressed"))
            {
                temp = BinaryUtil.Decompress(temp);
                YLog.Info("解压完成", "Binary");
            }

            // 3. 转字符串
            _restoredString = BinaryUtil.ToString(temp);

            // 4. 验证
            _isMatch = _restoredString == InputString;

            if (_isMatch)
                YLog.Info("数据还原成功！一致性校验通过。", "Binary");
            else
                YLog.Error("数据不匹配！", "Binary");
        }
        catch (Exception e)
        {
            YLog.Error($"还原过程出错: {e.Message}", "Binary");
        }
    }

    private void UpdateStatus(string state)
    {
        if (_status == "Empty") _status = state;
        else _status += " -> " + state;

        _currentSize = _processedBytes != null ? _processedBytes.Length : 0;
        YLog.Info($"当前状态: {_status}, 大小: {_currentSize} bytes", "Binary");
    }
}
