using System.IO;
using UnityEditor;
using UnityEngine;
using YFan.Attributes; // 引入属性

namespace YFan.Editor.CodeGen
{
    public class CodeGenWindow : YFanEditorWindow // 继承自我们的基类
    {
        [MenuItem("YFan/Code Generator")]
        public static void Open()
        {
            var win = GetWindow<CodeGenWindow>("CodeGen");
            win.minSize = new Vector2(400, 500);
            win.Show();
        }

        // --- 1. 配置区域 ---
        [YBoxGroup("设置")]
        [YHelpBox("脚本生成的根目录", YMessageType.Info)]
        public string ScriptGeneratePath = "Assets/Scripts/Game";

        [YBoxGroup("设置")]
        public string Namespace = "Game.Module";

        // --- 2. 模块生成 ---
        [YSpace(15)]
        [YBoxGroup("模块生成 (MVC)")]
        public string ModuleName = "Login";

        [YBoxGroup("模块生成 (MVC)")]
        public bool CreateModel = true;

        [YBoxGroup("模块生成 (MVC)")]
        public bool CreateController = true;

        [YBoxGroup("模块生成 (MVC)")]
        public bool CreateSystem = false;

        // --- 3. UI 绑定生成 (你的 AutoUIBinder 雏形) ---
        [YSpace(15)]
        [YBoxGroup("UI 绑定")]
        [YHelpBox("请先选择一个 Prefab 或 GameObject", YMessageType.Info)]
        public GameObject TargetPrefab;

        [YBoxGroup("UI 绑定")]
        [YShowIf("HasTarget")] // 只有选了 Prefab 才显示生成按钮
        [YButton("生成 Bind 代码", 40)]
        [YColor("#88FF88")]
        public void GenerateUIBind()
        {
            Debug.Log($"正在为 {TargetPrefab.name} 生成绑定代码...");
            // 这里以后调用 AutoUIBinder.Generate(TargetPrefab);
        }

        // --- 底部按钮 ---
        [YSpace(20)]
        [YButton("生成 MVC 模板代码", 50)]
        [YColor("#8888FF")]
        public void GenerateMVC()
        {
            if (string.IsNullOrEmpty(ModuleName))
            {
                Debug.LogError("模块名不能为空");
                return;
            }

            Debug.Log($"生成模块: {ModuleName} 到 {ScriptGeneratePath} (Model:{CreateModel}, Ctrl:{CreateController})");
            // 这里调用 CodeGenKit 的核心逻辑
        }

        // --- 辅助属性 ---
        private bool HasTarget => TargetPrefab != null;
    }
}
