# YFanFramework - Unity 独游开发终极解决方案

![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Status](https://img.shields.io/badge/Status-Active-orange.svg)

**YFanFramework** 是一个基于 Unity 的游戏开发框架，专为独立游戏开发者设计，提供了一套完整的 MVC 架构、工具集和系统模块，旨在帮助开发者快速构建高质量的游戏项目。

它旨在解决中小型项目中的常见痛点：配置管理繁琐、存档不安全、UI 堆栈管理混乱以及资源加载异步地狱。框架提供了开箱即用的核心系统，代码清晰，易于扩展。

## ✨ 核心特性

*   **⚡ 异步优先**: 核心模块全面采用 `UniTask`，告别回调地狱，逻辑线性化。
*   **📄 强大的配置系统**: CSV 一键生成 C# 代码与 ScriptableObject 资产。支持泛型 ID（`int`/`string`），支持热重载。
*   **💾 双模式存档**: 开发环境使用明文 JSON，发布环境自动切换为 **AES加密 + GZip压缩** 的二进制格式。支持多槽位与元数据。
*   **📱 现代 UI 管理**: 基于栈（Stack）的 UI 管理，支持自动遮罩（Blocker）、层级管理、自动焦点导航（手柄支持）及 Attribute 绑定。
*   **🔊 音频系统**: BGM 双通道淡入淡出、音效对象池、语音管理、全局音量控制。
*   **🎮 输入系统**: 深度集成 Unity `InputSystem`，支持运行时改键、多套按键映射。
*   **📦 资源管理**: 封装 Addressables，提供引用计数与自动释放机制。

## 🛠️ 安装与依赖

1.  **环境要求**:
    *   Unity 2021.3 或更高版本。
    *   Package: **Addressables**, **Input System**, **Newtonsoft Json**.
2.  **第三方库(内置)**:
    *   [QFramework](https://github.com/liangxiegame/QFramework) (核心架构)
    *   [UniTask](https://github.com/Cysharp/UniTask) (异步处理)
    *   [DOTween](https://github.com/Demigiant/dotween) (动画)
3.  **安装**:
    *   将 `YFanFramework` 文件夹拖入项目的 `Assets` 目录。
    *   等待 Unity 编译完成。

## 🚀 快速开始

### 1. 启动架构
在场景中创建一个空的 GameObject，挂载初始化脚本：

```csharp
using YFan.Runtime.Base;

public class GameStart : MonoBehaviour
{
    void Awake()
    {
        // 访问一次 Interface 即可触发架构初始化
        var app = YFanApp.Interface;
        DontDestroyOnLoad(this);
    }
}
```

### 2. UI 界面开发
继承 `BasePanel` 并指定层级。

```csharp
using YFan.Runtime.Modules;
using YFan.Attributes;
using UnityEngine.UI;

public class MainMenuPanel : BasePanel
{
    // 指定层级
    public override UILayer Layer => UILayer.Mid;

    // 开启自动遮罩
    public override bool UseMask => true;

    // 自动绑定 UI 组件 (无需手动 Find)
    [UIBind("Root/StartBtn")]
    private Button _startBtn;

    protected override void OnInit()
    {
        // 绑定事件
        _startBtn.onClick.AddListener(() => {
            Debug.Log("Game Start!");
        });
    }

    // 通过 Attribute 绑定点击事件
    [BindClick("Root/QuitBtn")]
    private void OnQuitClicked()
    {
        Application.Quit();
    }
}
```

打开面板：
```csharp
// 普通打开
await YFanApp.Interface.GetSystem<IUIManager>().Open<MainMenuPanel>();

// 压入堆栈 (自动处理遮罩和焦点)
await YFanApp.Interface.GetSystem<IUIManager>().Push<ConfirmPanel>();
```

### 3. 配置表工作流
1.  打开 **YFan/Tools/Config Manager**。
2.  输入文件名创建新表（如 `ItemConfig`）。
3.  编辑 CSV 数据（第一行为字段名，第二行为类型，第三行为注释）。
    *   *支持类型: int, float, string, bool, vector3, List\<int> 等*
4.  点击 **"一键更新所有"**。
5.  在代码中调用：

```csharp
// 这里的 ID 类型根据 CSV 第一列自动推断 (int 或 string)
var item = ItemConfigTable.Get(1001);
Debug.Log($"Item Name: {item.Name}");
```

### 4. 存档系统
存档系统会自动处理 JSON 序列化、GZip 压缩和 AES 加密。

```csharp
// 定义数据结构
public class PlayerData { public int Hp; public string Name; }

// 保存
var data = new PlayerData { Hp = 100, Name = "Hero" };
SaveUtil.Save("Slot_1", data, "第一章：起航");

// 读取
var loadedData = SaveUtil.Load<PlayerData>("Slot_1");
```

## 📂 模块详解

### 🔊 AudioSystem (音频)
```csharp
var audioSys = YFanApp.Interface.GetSystem<IAudioSystem>();

// 播放 BGM (自动淡出上一首，淡入新的一首)
audioSys.PlayBGM("BGM_Battle_01", fadeDuration: 1.0f);

// 播放音效 (自动使用对象池)
audioSys.PlaySound("SFX_Sword_Hit");
```

### 🎮 InputSystem (输入)
```csharp
var inputSys = YFanApp.Interface.GetSystem<IInputSystem>();

// 运行时改键
inputSys.StartRebind("Jump", 0,
    onComplete: (newName) => Debug.Log($"改键成功: {newName}"),
    onCancel: () => Debug.Log("取消改键")
);
```

### 📦 AssetUtil (资源)
封装了 Addressables，自动管理引用计数。
```csharp
// 异步加载
var prefab = await assetUtil.LoadAsync<GameObject>("PlayerPrefab");

// 实例化 (内部自动引用计数 +1)
var go = await assetUtil.InstantiateAsync("PlayerPrefab");

// 释放 (引用计数 -1，归零时自动 Release)
assetUtil.Release("PlayerPrefab");
```

## 📝 TODO

- [ ] **UI 代码生成**: 替代目前的反射绑定，进一步提升 UI 初始化性能。
- [ ] **Config 增强**: 支持 JSON 格式的复杂嵌套数据列。
- [ ] **Audio Mixer**: 集成 AudioMixerGroup 以支持更高级的混音效果。

## 📄 License

本项目基于 MIT 协议开源。
依赖库版权归原作者所有：
*   QFramework (MIT)
*   UniTask (MIT)
*   Newtonsoft.Json (MIT)

---

**Happy Coding!** 🎉
