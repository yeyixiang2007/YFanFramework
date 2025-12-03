# YFanFramework 使用指南

YFanFramework 是一个基于 Unity 和 QFramework 的独立游戏开发框架，旨在提供完整的游戏开发解决方案。本文档将介绍框架的使用方法、核心功能和最佳实践。

## 目录结构

```
Assets/YFanFramework/
├── Editor/           # 编辑器工具
│   ├── CodeGen/      # 代码生成工具
│   └── Core/         # 编辑器核心功能
├── Libs/             # 第三方库
│   ├── DOTween/      # 动画库
│   ├── QFramework/   # 基础框架
│   └── UniTask/      # 异步任务库
└── Runtime/          # 运行时代码
    ├── Base/         # 基础类和接口
    ├── Modules/      # 系统模块
    └── Utils/        # 工具类
```

## 1. 快速开始

### 1.1 初始化框架

在游戏启动场景中，创建一个空 GameObject 并添加 `YFanApp` 组件，或在代码中初始化：

```csharp
using YFan.Runtime.Base;
using QFramework;

public class GameEntry : MonoBehaviour
{
    void Start()
    {
        // 初始化 YFanFramework
        YFanApp.Instance.Init();
        
        // 获取系统模块
        var audioSystem = YFanApp.Instance.GetSystem<IAudioSystem>();
        var inputSystem = YFanApp.Instance.GetSystem<IInputSystem>();
        
        // 获取工具类
        var logUtil = YFanApp.Instance.GetUtility<ILogUtil>();
        var assetUtil = YFanApp.Instance.GetUtility<IAssetUtil>();
    }
}
```

## 2. 核心功能

### 2.1 日志系统 (YLog)

日志系统提供了增强的日志功能，支持模块标签、颜色自定义和文件保存。

```csharp
using YFan.Utils;

// 基础用法
YLog.Info("游戏启动成功", "GameManager");
YLog.Warn("资源加载缓慢", "ResourceManager");
YLog.Error("无法连接服务器", "NetworkManager");
YLog.Exception(exception, "GameManager");

// 配置
YLog.Init(logUtil); // 通常由框架自动初始化
YLog.EnableSaveToFile(true); // 开启日志文件保存
```

### 2.2 音频系统 (AudioSystem)

音频系统支持 BGM、音效和语音的管理，包含音量控制、淡入淡出和自动保存设置。

```csharp
using YFan.Modules;
using Cysharp.Threading.Tasks;

// 获取音频系统
var audioSystem = YFanApp.Instance.GetSystem<IAudioSystem>();

// 播放 BGM
await audioSystem.PlayBGM("bgm_main", fade: true, fadeDuration: 1.0f);

// 停止 BGM
audioSystem.StopBGM(fade: true);

// 播放音效
audioSystem.PlaySound("sfx_button_click");

// 带参数播放音效
var param = new AudioPlayParams
{
    Key = "sfx_explosion",
    VolumeScale = 1.5f,
    Position = transform.position, // 3D 音效
    RandomPitch = true
};
audioSystem.PlaySound(param);

// 音量控制
audioSystem.SetVolume(AudioLayer.Master, 0.8f);
audioSystem.SetVolume(AudioLayer.BGM, 0.6f);
audioSystem.SetMute(AudioLayer.Sound, true);
```

### 2.3 输入系统 (InputSystem)

基于 Unity 新输入系统的封装，支持按键重绑定和设置保存。

```csharp
using YFan.Modules;
using UnityEngine.InputSystem;

// 获取输入系统
var inputSystem = YFanApp.Instance.GetSystem<IInputSystem>();

// 轮询输入
if (inputSystem.GetButtonDown("Jump"))
{
    Player.Jump();
}

Vector2 moveDir = inputSystem.GetVector2("Move");
Player.Move(moveDir);

// 事件绑定
inputSystem.BindAction("Fire", ctx => Player.Fire());

// 按键重绑定
inputSystem.StartRebind("Jump", 0, 
    newKey => Debug.Log($"Jump 键已更改为: {newKey}"),
    () => Debug.Log("改键取消")
);

// 保存设置
inputSystem.SaveInputSettings();
```

### 2.4 自动模块绑定 (AutoModuleBinder)

自动扫描带有 `[AutoRegister]` 标签的类并注册到框架中。

```csharp
using YFan.Attributes;
using YFan.Modules;
using QFramework;

[AutoRegister(typeof(IMySystem))]
public class MySystem : AbstractSystem, IMySystem
{
    protected override void OnInit()
    {
        // 系统初始化
    }
    
    public void DoSomething()
    {
        // 实现功能
    }
}

// 框架会自动注册该系统
```

### 2.5 UI 自动绑定 (AutoUIBinder)

通过属性标签自动绑定 UI 组件和事件。

```csharp
using YFan.Runtime.Base.Abstract;
using YFan.Attributes;
using UnityEngine.UI;

public class MainMenuController : UIAbstractController
{
    [UIBind] private Button btnStart;
    [UIBind] private Button btnSettings;
    [UIBind("TxtTitle")] private Text titleText;
    [UIBind] private GameObject loadingPanel;
    
    void Start()
    {
        titleText.text = "游戏主菜单";
    }
    
    [BindClick("btnStart")]
    private void OnStartClick()
    {
        loadingPanel.SetActive(true);
        LoadGameAsync().Forget();
    }
    
    [BindClick("btnSettings")]
    private void OnSettingsClick()
    {
        // 打开设置面板
    }
}
```

### 2.6 任务工具 (TaskUtil)

提供安全的异步任务执行和重试机制。

```csharp
using YFan.Utils;
using Cysharp.Threading.Tasks;

// 安全执行异步任务
async UniTask LoadGameAsync()
{
    await TaskUtil.Retry(async () =>
    {
        // 尝试加载游戏数据
        await LoadGameData();
    }, retryCount: 3, delaySeconds: 1.0f);
}

// 条件等待
await TaskUtil.WaitUntil(() => IsResourceReady(), timeoutSeconds: 10.0f);

// 安全取消任务
CancellationTokenSource cts = new CancellationTokenSource();
// ...
TaskUtil.CancelSafe(ref cts);
```

## 3. 配置管理

框架使用 `ConfigKeys` 类管理全局配置常量：

```csharp
using YFan.Base;

// 访问配置常量
string inputAssetKey = ConfigKeys.InputAssetKey;
string audioSettingSaveSlot = ConfigKeys.AudioSettingSaveSlot;
```

## 4. 系统模块

### 4.1 音频系统 (AudioSystem)

| 功能 | 状态 | 描述 |
|------|------|------|
| BGM 管理 | ✅ 已实现 | 支持淡入淡出、暂停/恢复 |
| 音效管理 | ✅ 已实现 | 支持 2D/3D 音效、音效池 |
| 语音管理 | ✅ 已实现 | 独立语音通道 |
| 音量控制 | ✅ 已实现 | 主音量、BGM、音效、语音独立控制 |
| 设置保存 | ✅ 已实现 | 自动保存音频设置 |

### 4.2 输入系统 (InputSystem)

| 功能 | 状态 | 描述 |
|------|------|------|
| 基础输入 | ✅ 已实现 | 支持键盘、鼠标、手柄 |
| 按键重绑定 | ✅ 已实现 | 支持交互式按键重绑定 |
| 设置保存 | ✅ 已实现 | 自动保存按键设置 |
| 多输入映射 | ✅ 已实现 | 支持切换不同的输入映射 |

### 4.3 资源系统 (AssetSystem)

| 功能 | 状态 | 描述 |
|------|------|------|
| 资源加载 | ✅ 已实现 | 基于 Addressables 的异步加载 |
| 资源缓存 | ✅ 已实现 | 自动资源缓存和引用计数 |
| 预加载 | ✅ 已实现 | 支持按标签预加载资源 |
| 资源释放 | ✅ 已实现 | 安全的资源释放机制 |

### 4.4 存档系统 (SaveSystem)

| 功能 | 状态 | 描述 |
|------|------|------|
| 数据保存 | ✅ 已实现 | 支持二进制和 JSON 格式 |
| 加密存储 | ✅ 已实现 | 支持 AES 加密 |
| 自动备份 | ❌ 未实现 | 定期自动备份存档 |
| 云同步 | ❌ 未实现 | 支持云端存档同步 |

### 4.5 网络系统 (NetworkSystem)

| 功能 | 状态 | 描述 |
|------|------|------|
| HTTP 请求 | ❌ 未实现 | 支持 RESTful API 调用 |
| WebSocket | ❌ 未实现 | 支持实时通信 |
| 网络状态检测 | ❌ 未实现 | 监控网络连接状态 |

## 5. 工具类

### 5.1 已实现工具

| 工具类 | 描述 |
|--------|------|
| YLog | 增强的日志系统 |
| LogUtil | 日志工具实现 |
| TaskUtil | 异步任务工具 |
| AutoModuleBinder | 自动模块绑定 |
| AutoUIBinder | UI 自动绑定 |
| AssetUtil | 资源加载工具 |

### 5.2 未实现工具

| 工具类 | 描述 |
|--------|------|
| StringUtil | 字符串处理工具 |
| MathUtil | 数学计算工具 |
| DateUtil | 日期时间工具 |
| FileUtil | 文件操作工具 |
| CryptoUtil | 加密解密工具 |

## 6. 代码生成

框架提供了代码生成工具，用于自动生成模板代码：

1. 在 Unity 菜单栏中选择 `YFanFramework > Code Generator`
2. 选择要生成的代码类型（如 UI 控制器、系统模块等）
3. 填写必要信息并点击生成

## 7. 最佳实践

1. **模块设计**：遵循单一职责原则，每个系统模块只负责一个功能领域
2. **异步编程**：使用 UniTask 进行异步操作，避免阻塞主线程
3. **错误处理**：使用 YLog 记录错误，并在关键操作中添加重试机制
4. **资源管理**：使用 AssetSystem 统一管理资源加载和释放，避免内存泄漏
5. **代码规范**：遵循框架的命名规范，使用 PascalCase 命名类和方法

## 8. 常见问题

### Q: 如何添加自定义系统模块？
A: 创建一个继承自 `AbstractSystem` 的类，并添加 `[AutoRegister]` 标签

### Q: 如何扩展自动 UI 绑定支持的组件？
A: 修改 `AutoUIBinder.cs` 文件，在 `BindMethods` 方法中添加新的组件支持

### Q: 如何配置日志保存？
A: 在初始化时调用 `YLog.EnableSaveToFile(true)`

## 9. 开发路线

### 阶段 1: 基石与规范（已完成）
- ✅ 框架基础架构搭建
- ✅ 代码规范与模板
- ✅ 核心工具类开发

### 阶段 2: 生产力工具（进行中）
- ✅ 代码生成工具
- ✅ UI 自动绑定
- ⏳ 资源管理优化

### 阶段 3: 核心系统（进行中）
- ✅ 音频系统
- ✅ 输入系统
- ⏳ 存档系统
- ⏳ 网络系统

### 阶段 4: UI 与表现层（规划中）
- ⏳ UI 框架
- ⏳ 动画系统
- ⏳ 特效系统

### 阶段 5: 游戏逻辑核心（规划中）
- ⏳ 角色系统
- ⏳ 战斗系统
- ⏳ AI 系统

### 阶段 6: 高级调试与优化（规划中）
- ⏳ 性能分析工具
- ⏳ 内存优化
- ⏳ 网络优化

### 阶段 7: 验证与演示（规划中）
- ⏳ 完整游戏演示
- ⏳ 性能测试
- ⏳ 文档完善

## 10. 贡献指南

我们欢迎社区贡献！请遵循以下步骤：

1. Fork 仓库
2. 创建功能分支
3. 提交代码
4. 运行测试
5. 提交 Pull Request

## 11. 许可证

本框架采用 MIT 许可证，详见 LICENSE 文件。

## 12. 联系方式

如有问题或建议，请联系：

- GitHub: https://github.com/yourusername/YFanFramework
- Email: your.email@example.com

---

**更新日期**: 2024-01-01  
**版本**: 1.0.0