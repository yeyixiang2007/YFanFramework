# YFanFramework - Unity 独游开发终极解决方案

## 框架简介
YFanFramework 是一个基于 Unity 的游戏开发框架，专为独立游戏开发者设计，提供了一套完整的 MVC 架构、工具集和系统模块，旨在帮助开发者快速构建高质量的游戏项目。

## 核心特点

### 架构设计
- **基于QFramework的MVC游戏架构**，经过优化和扩展
- **统一的架构入口和业务入口**，确保架构稳定性
- **AutoModuleBinder** - 基于反射的模块自动绑定系统
- **严格的程序集划分**：Runtime和Editor代码完全分离

### 代码生成与自动化
- **CodeGenKit** - 强大的代码生成器，一键生成Model、System、Controller模板
- **AutoUIBinder** - 基于反射的UI事件自动绑定
- **AutoEventBinder** - 基于反射的QFramework事件自动绑定

### 系统模块集
- **FlowSystem** - 基于FSMUtil的多状态机游戏流程管理器
- **AudioSystem** - 基于UniTask的音频系统，包含音频池、防爆音功能
- **AnimationSystem** - 基于UniTask和FSM的动画系统
- **UIAnimationSystem** - 支持UI控件的位移、缩放、旋转、RGBA变换等动画
- **UIManager** - UI管理器，实现UI栈管理和层级管理
- **AssetSystem** - 基于Addressable的资源管理系统
- **InputSystem** - 游戏输入管理系统
- **LocalizationSystem** - 多语言支持系统
- **NetworkSystem** - 网络功能系统，支持异步网络请求

### 实用工具集
- **LogUtil/YLog** - 支持多模块自动颜色区分、日志等级筛选
- **AssetUtil** - 基于Addressable的资源管理工具
- **SaveUtil** - 支持JSON、二进制、加密二进制、多槽位存档
- **TaskUtil** - 基于UniTask的异步逻辑封装
- **JSONUtil** - 基于Newtonsoft.Json的JSON功能封装
- **MonoUtil** - 为非Mono对象提供Mono虚拟机相关功能
- **FSMUtil** - 有限状态机工具

### 开发辅助工具
- **YFanConsole** - 增强型控制台，支持命令注册和执行
- **YFanMonitor** - 运行时监视器，实时查看标记变量

## 目录结构

```
YFanFramework/
├── Assets/
│   ├── AddressableAssetsData/    # Addressable资源配置
│   ├── Scenes/                   # 示例场景和测试场景
│   └── YFanFramework/            # 框架核心代码
│       ├── Editor/               # 编辑器相关代码
│       ├── Libs/                 # 第三方库依赖
│       └── Runtime/              # 运行时核心代码
├── Docs/                         # 文档目录
└── Packages/                     # Unity包管理
```

## 快速开始

### 安装与配置
1. 将框架克隆或下载到你的Unity项目中
2. 确保正确配置Assembly Definition (Asmdef)文件
3. 安装必要的依赖包：
   - QFramework
   - UniTask
   - Addressables
   - Newtonsoft.Json
   - New Input System

### 基本使用

#### 创建第一个MVC模块
使用CodeGenKit生成器一键创建Model、System和Controller：
1. 在Unity编辑器中打开CodeGenKit窗口
2. 输入模块名称
3. 点击生成按钮自动创建相关文件

#### 使用AutoModuleBinder
框架会自动通过反射机制绑定所有模块，无需手动注册。

#### 使用日志系统
```csharp
// 使用YLog记录不同级别的日志
YLog.I("这是一条信息日志");
YLog.W("这是一条警告日志");
YLog.E("这是一条错误日志");
```

## 详细文档

框架的详细设计和使用说明请查看：
- [YFanFramework设计文档](Docs/YFanFramework.md)
- [YFanFramework设计路线](Docs/YFanFramework%20设计路线.md)

## 示例场景

在`Assets/Scenes/`目录下提供了以下示例场景：
- **BinaryTester** - 二进制数据处理示例
- **JsonTester** - JSON序列化与反序列化示例
- **LogUtilTester** - 日志系统使用示例
- **MonoUtilTester** - Mono工具使用示例
- **TaskUtilTester** - UniTask异步工具示例
- **UITester** - UI系统使用示例

## 开发路线

框架采用"核心优先，工具驱动，业务验证"的迭代逻辑，分为7个主要阶段：
1. 基石与规范 (Foundation & Standards)
2. 生产力工具 (Productivity Tools)
3. 核心系统 - 资源与输入 (Core Systems - I/O)
4. UI 与表现层 (UI & Presentation)
5. 游戏逻辑核心 (Gameplay Core)
6. 高级调试与优化 (Advanced Debug & Polish)
7. 验证与演示 (Validation)

## 贡献指南

欢迎对框架进行贡献！如果你有任何建议或改进，请通过以下方式提交：
1. Fork项目仓库
2. 创建你的功能分支
3. 提交你的更改
4. 推送到分支
5. 创建一个新的Pull Request

## 许可证

本框架采用MIT许可证。详情请参阅LICENSE文件。

## 联系方式

如有任何问题或建议，请联系框架作者。

---

**YFanFramework** - 为独立游戏开发者打造的Unity终极解决方案！
