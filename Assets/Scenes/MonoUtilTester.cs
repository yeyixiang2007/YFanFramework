using System.Collections;
using QFramework;
using UnityEngine;
using YFan.Attributes;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

public class MonoUtilTester : AbstractController
{
    private IMonoUtil _monoUtil;

    // --- 状态显示区 ---
    [YTitle("运行状态")]

    [SerializeField]
    [YReadOnly]
    private string _appStatus = "正常运行";

    [SerializeField]
    [YReadOnly]
    private string _coroutineStatus = "无";

    [YTitle("帧数监控")]

    [SerializeField]
    [YReadOnly]
    private int _updateFrameCount = 0;

    [SerializeField]
    [YReadOnly]
    private int _fixedUpdateFrameCount = 0;

    // 辅助字段：用于 ShowIf 判断
    [SerializeField]
    [YReadOnly] // 显示出来方便看状态
    private bool _isUpdateRunning = false;

    // 反向属性用于 ShowIf
    private bool IsNotRunning => !_isUpdateRunning;

    void Start()
    {
        _monoUtil = this.GetUtility<IMonoUtil>();
        _monoUtil.OnApplicationPauseEvent += OnAppPause;
        _monoUtil.OnApplicationQuitEvent += OnAppQuit;
        YLog.Info("MonoUtil 测试脚本已启动", "Tester");
    }

    private void OnDestroy()
    {
        if (_monoUtil != null)
        {
            StopUpdateListen(); // 复用停止逻辑
            _monoUtil.OnApplicationPauseEvent -= OnAppPause;
            _monoUtil.OnApplicationQuitEvent -= OnAppQuit;
        }
    }

    // --- Update 控制区 ---

    [YTitle("Update 驱动控制")]

    [YButton("▶ 开始监听 Update", 40)]
    [YShowIf("IsNotRunning")] // 只有没运行时才显示这个按钮
    [YColor("#88FF88")]
    private void StartUpdateListen()
    {
        _monoUtil.AddUpdateListener(OnUpdateTick);
        _monoUtil.AddFixedUpdateListener(OnFixedUpdateTick);
        _isUpdateRunning = true;
        YLog.Info("已添加 Update 监听", "Tester");
    }

    [YButton("⏹ 停止监听 Update", 40)]
    [YShowIf("_isUpdateRunning")] // 只有运行时才显示这个按钮
    [YColor("#FF8888")]
    private void StopUpdateListen()
    {
        _monoUtil.RemoveUpdateListener(OnUpdateTick);
        _monoUtil.RemoveFixedUpdateListener(OnFixedUpdateTick);
        _isUpdateRunning = false;
        YLog.Info("已移除 Update 监听", "Tester");
    }

    // --- 协程测试区 ---

    [YTitle("其他功能")]
    [YButton("测试协程 (2秒)", 35)]
    private void TestCoroutine()
    {
        _monoUtil.StartCoroutine(LegacyCoroutine());
    }

    // --- 回调逻辑 (不变) ---

    private void OnUpdateTick() => _updateFrameCount++;
    private void OnFixedUpdateTick() => _fixedUpdateFrameCount++;

    private void OnAppPause(bool isPaused)
    {
        _appStatus = isPaused ? "后台暂停中" : "前台运行中";
        YLog.Warn($"应用状态: {_appStatus}", "MonoUtil");
    }
    private void OnAppQuit() => YLog.Warn("应用退出！", "MonoUtil");

    private IEnumerator LegacyCoroutine()
    {
        _coroutineStatus = "运行中...";
        YLog.Info("协程开始", "MonoUtil");
        yield return new WaitForSeconds(2.0f);
        _coroutineStatus = "已完成";
        YLog.Info("协程结束", "MonoUtil");
    }
}
