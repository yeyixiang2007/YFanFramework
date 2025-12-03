using QFramework;
using UnityEngine;
using YFan.Attributes;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;
using Cysharp.Threading.Tasks;

public class AssetUtilTester : AbstractController
{
    private IAssetUtil _assetUtil;

    // --- 配置区域 ---
    [YTitle("测试配置")]
    [YBoxGroup("设置")]
    [YHelpBox("请确保 Addressables 中存在此 Key 的资源", YMessageType.Info)]
    public string TargetKey = "Cube"; // 默认测试 Key

    // --- 状态监控 ---
    [YSpace(10)]
    [YTitle("资源状态监控")]
    [YBoxGroup("当前状态")]
    [YReadOnly][SerializeField] private string _loadStatus = "Idle";

    [YBoxGroup("当前状态")]
    [YReadOnly][SerializeField] private int _refCount = 0;

    [YBoxGroup("当前状态")]
    [YReadOnly][SerializeField] private string _loadedAssetName = "Null";

    // 缓存引用以便测试释放
    private GameObject _instantiatedObj;
    private Object _loadedAsset;

    private void Start()
    {
        _assetUtil = this.GetUtility<IAssetUtil>();
        YLog.Info("AssetUtilTester 已启动，等待测试...", "Tester");
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_assetUtil == null) return;
        _refCount = _assetUtil.GetRefCount(TargetKey);
        _loadedAssetName = _loadedAsset != null ? _loadedAsset.name : "Null";
    }

    // --- 功能测试按钮 ---

    [YButton("1. 纯加载资源 (LoadAsync)", 35)]
    [YColor("#88FF88")]
    private async void TestLoad()
    {
        if (string.IsNullOrEmpty(TargetKey)) return;

        _loadStatus = "Loading...";
        YLog.Info($"开始加载资源: {TargetKey}", "Tester");

        // 测试泛型加载 (这里假设是 GameObject，也可以是 Texture 等)
        _loadedAsset = await _assetUtil.LoadAsync<Object>(TargetKey);

        if (_loadedAsset != null)
        {
            _loadStatus = "Success";
            YLog.Info($"加载成功: {_loadedAsset.name}", "Tester");
        }
        else
        {
            _loadStatus = "Failed";
            YLog.Error($"加载失败: {TargetKey}", "Tester");
        }

        UpdateStatus();
    }

    [YButton("2. 实例化 (InstantiateAsync)", 35)]
    [YColor("#88FFFF")]
    private async void TestInstantiate()
    {
        if (string.IsNullOrEmpty(TargetKey)) return;

        _loadStatus = "Instantiating...";

        // 实例化会增加引用计数
        var go = await _assetUtil.InstantiateAsync(TargetKey);

        if (go != null)
        {
            _instantiatedObj = go;
            // 为了不让生成的物体乱跑，设置一点随机位置
            go.transform.position = Random.insideUnitSphere * 2f;
            _loadStatus = "Instantiated";
            YLog.Info($"实例化成功: {go.name}", "Tester");
        }
        else
        {
            _loadStatus = "Instantiate Failed";
        }

        UpdateStatus();
    }

    [YButton("3. 释放资源 (Release)", 35)]
    [YColor("#FF8888")]
    private void TestRelease()
    {
        if (string.IsNullOrEmpty(TargetKey)) return;

        YLog.Info($"释放资源 Key: {TargetKey}", "Tester");

        // 调用核心释放逻辑
        _assetUtil.Release(TargetKey);

        // 如果引用计数归零，清理本地引用
        if (_assetUtil.GetRefCount(TargetKey) <= 0)
        {
            _loadedAsset = null;
            _loadStatus = "Unloaded";
        }

        // 注意：AssetUtil 的 Release 只是减少引用计数和卸载内存中的 AssetBundle
        // 它不会自动 Destroy 场景中已经实例化出来的 GameObject
        // 这里为了演示效果，手动销毁场景物体（如果存在）
        if (_instantiatedObj != null)
        {
            YLog.Info("销毁场景实例对象", "Tester");
            Destroy(_instantiatedObj);
            _instantiatedObj = null;
        }

        UpdateStatus();
    }

    [YSpace(10)]
    [YButton("打印缓存信息 (Dump Info)", 30)]
    [YColor("#FFFF88")]
    private void TestDump()
    {
        _assetUtil.DumpCacheInfo();
    }
}
