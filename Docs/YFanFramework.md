YFanFramework Unity框架设计

+ **基于QFramework的MVC游戏架构**
	+ 对QFramework的一些设定做了修改
		+ 新增AbstractController继承MonoBehaviour和IController
		+ 新增AbstractEditor继承EditorWindow（非原生，基于我的框架修改）和IController
            + 严格划分YFanFramework.Runtime和YFanFramework.Editor两个程序集。确保Runtime代码绝对不引用UnityEditor命名空间。
+ **统一的架构入口和业务入口（用户入口），稳定性极强**
	+ **AutoModuleBinder** - 基于反射的模块绑定，无需每个模块书写绑定逻辑
	+ 架构入口对用户不开放，保证架构模块的安全性
+ **CodeGenKit** - 代码生成器，一键生成Model、System、Controller模板代码，支持自定义模板
	+ 另外支持在编译前批量生成UI绑定代码、模块绑定代码等，大大提高打包后的游戏程序运行效率
+ **YFanConsole** - 更强大的控制台(***Editor***)
	+ **CommandRegistry** - 基于反射的命令注册器，使用属性标记静态函数或实例成员函数，可以在控制台内运行
	+ 连接到LogUtil同步原生控制台命令
	+ 支持堆栈显示
	+ 支持特定规则的提醒触发
+ **YFanMonitor** - 运行时监视器，使用属性标记静态或实例成员变量，游戏运行时可实时查看，提高开发效率(***Editor***)
+ **AutoUIBinder** - 基于反射的UI事件绑定，大量减少胶水代码，提高开发效率(***Editor***)
+ **AutoEventBinder** - 基于反射的QFramework事件绑定，高效处理事件，可选UnRegister时机(***Editor***)
+ 基于云的配置数据同步系统，实现可靠方便的配置数据管理
+ ==架构模块集==：
	+ **FlowSystem** - 基于FSMUtil的多状态机游戏流程管理器
		+ 支持状态机注册、状态机内部和状态机之间的状态切换
		+ 支持低代码状态机设计，策划和美术也可轻松上手(***考虑***)
	+ **AudioSystem** - 基于UniTask的音频系统，可高效管理、播放各种类型的音效，包含音频池、防爆音等等功能
	+ **AnimationSystem** - 基于UniTask和FSM的动画系统
		+ 帧动画、骨骼动画
    + **UIAnimationSystem** - UI动画系统，基于UniTask和FSM的动画系统
        + 支持UI控件的位移、缩放、旋转、RGBA变换等动画
        + 支持UI控件的序列动画，如淡入淡出、滑动等
        + 支持UI控件的自定义动画，如自定义插值函数等
	+ **UIManager** - UI管理器，用于管理场景中的UI控件的状态、层级等
        + UI栈管理
        + UI层级管理
	+ **AssetSystem** - 资源系统，用于管理游戏资源，是架构最开始就要注册的核心系统
        + 支持Addressable资源加载、资源缓存、资源释放等功能
        + 支持资源的异步加载、同步加载
        + 支持资源计数等安全措施
    + **InputSystem** - 输入系统，用于管理游戏输入，是架构最开始就要注册的核心系统
        + 支持键盘、鼠标、游戏手柄等输入设备
        + 支持输入事件的注册、注销、触发等功能
    + **LocalizationSystem** - 本地化系统，用于管理游戏的多语言支持
        + 支持基于Addressable的资源加载，实现图片、文本等资源的多语言切换
        + 支持基于JSON的多语言配置文件
        + 支持在运行时切换语言
        + 格式与排版本化
    + **NetworkSystem** - 网络系统，用于提供游戏网络功能
        + 支持基于UniTask的异步网络请求
        + 支持基于JSON的网络数据序列化与反序列化
        + 支持基于TCP的可靠传输协议
+ ==实用的工具集==
	+ **LogUtil** - 支持多模块自动颜色区分、日志等级筛选等
		+ **YLog** - LogUtil的静态绑定，使用方便
	+ **AssetUtil** - 基于Addressable的资源管理工具，与AssetSystem绑定，可方便地加载、获取游戏资源
	+ **SaveUtil** - 支持JSON、二进制、加密二进制、多槽位存档的存档工具
	+ **TaskUtil** - 提供基于UniTask的一系列异步逻辑的封装，便于维护
	+ **JSONUtil** - 提供给予Newtonsoft.Json的JSON相关功能绑定，如序列化与反序列化、格式化等
	+ **MonoUtil** - 为非Mono对象提供Mono虚拟机的相关功能
	+ **FSMUtil** - 有限状态机（非单例工具）
+ ==预设功能类Controller==
	+ **DraggableController** - 可鼠标拖拽的控件
	+ **UIAnimationController** - 位移、缩放、旋转、RGBA变换
	+ **LightAnimationController** - 强度、方向、深度、RGBA变换
	+ *...跟随我的项目经历逐渐补充*

+ 其他
    + SerializeReference - 序列化引用，用于在Inspector中引用其他脚本组件

