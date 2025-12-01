using System.Collections;
using QFramework;
using UnityEngine;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

public class MonoUtilTester : AbstractController
{
    private IMonoUtil _monoUtil;

    // 状态显示
    private int _updateFrameCount = 0;
    private int _fixedUpdateFrameCount = 0;
    private bool _isUpdateRunning = false;
    private string _coroutineStatus = "无";
    private string _appStatus = "正常运行";

    void Start()
    {
        // 获取工具
        _monoUtil = this.GetUtility<IMonoUtil>();

        // 注册全局生命周期监听
        _monoUtil.OnApplicationPauseEvent += OnAppPause;
        _monoUtil.OnApplicationQuitEvent += OnAppQuit;

        YLog.Info("MonoUtil 测试脚本已启动", "Tester");
    }

    private void OnDestroy()
    {
        // 务必养成注销的好习惯
        if (_monoUtil != null)
        {
            _monoUtil.RemoveUpdateListener(OnUpdateTick);
            _monoUtil.RemoveFixedUpdateListener(OnFixedUpdateTick);
            _monoUtil.OnApplicationPauseEvent -= OnAppPause;
            _monoUtil.OnApplicationQuitEvent -= OnAppQuit;
        }
    }

    // --- 回调逻辑 ---

    private void OnUpdateTick()
    {
        _updateFrameCount++;
        // 为了避免刷屏，每 60 帧打印一次
        if (_updateFrameCount % 60 == 0)
        {
            // YLog.Info($"Update 正在运行... Frame: {_updateFrameCount}", "MonoUtil");
        }
    }

    private void OnFixedUpdateTick()
    {
        _fixedUpdateFrameCount++;
    }

    private void OnAppPause(bool isPaused)
    {
        _appStatus = isPaused ? "后台暂停中" : "前台运行中";
        YLog.Warn($"检测到应用状态变化: {_appStatus}", "MonoUtil");
    }

    private void OnAppQuit()
    {
        YLog.Warn("检测到应用退出事件！", "MonoUtil");
    }

    // --- 协程测试逻辑 ---
    private IEnumerator LegacyCoroutine()
    {
        _coroutineStatus = "协程运行中 - 等待 2秒...";
        YLog.Info("协程开始", "MonoUtil");

        yield return new WaitForSeconds(2.0f);

        _coroutineStatus = "协程运行中 - 等待结束";
        YLog.Info("协程等待结束", "MonoUtil");

        yield return new WaitForEndOfFrame();

        _coroutineStatus = "协程已完成";
        YLog.Info("协程完成", "MonoUtil");
    }

    // --- GUI 测试面板 ---

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(350, 10, 300, 600)); // 放在 LogUtilTester 右边
        GUILayout.Box("<b>=== MonoUtil 测试 ===</b>");

        GUILayout.Space(10);
        GUILayout.Label($"<b>Update 帧数:</b> {_updateFrameCount}");
        GUILayout.Label($"<b>FixedUpdate 帧数:</b> {_fixedUpdateFrameCount}");
        GUILayout.Label($"<b>应用状态:</b> {_appStatus}");
        GUILayout.Label($"<b>协程状态:</b> {_coroutineStatus}");

        GUILayout.Space(10);

        // 测试 Update 监听
        if (!_isUpdateRunning)
        {
            if (GUILayout.Button("1. 开始监听 Update"))
            {
                _monoUtil.AddUpdateListener(OnUpdateTick);
                _monoUtil.AddFixedUpdateListener(OnFixedUpdateTick);
                _isUpdateRunning = true;
                YLog.Info("已添加 Update 监听", "Tester");
            }
        }
        else
        {
            if (GUILayout.Button("2. 停止监听 Update"))
            {
                _monoUtil.RemoveUpdateListener(OnUpdateTick);
                _monoUtil.RemoveFixedUpdateListener(OnFixedUpdateTick);
                _isUpdateRunning = false;
                YLog.Info("已移除 Update 监听", "Tester");
            }
        }

        GUILayout.Space(10);

        // 测试协程
        if (GUILayout.Button("3. 启动测试协程 (2秒)"))
        {
            _monoUtil.StartCoroutine(LegacyCoroutine());
        }

        GUILayout.Space(10);
        GUILayout.Label("<b>测试说明：</b>");
        GUILayout.Label("1. 点击开始监听，观察上方帧数变化");
        GUILayout.Label("2. 点击停止，帧数应立即停止增长");
        GUILayout.Label("3. 切换 Unity 编辑器到后台，观察应用状态");

        GUILayout.EndArea();
    }
}
