using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using YFan.Attributes;

namespace YFan.Editor.Config
{
    public class ConfigWindow : YFanEditorWindow
    {
        [MenuItem("YFan/Tools/Config Manager")]
        public static void Open()
        {
            var win = GetWindow<ConfigWindow>("Config Manager");
            win.minSize = new Vector2(900, 600);
            win.Show();
        }

        #region 字段与状态

        private List<string> _fileList = new List<string>();
        private string _selectedFile = "";
        private string _searchFilter = "";
        private Vector2 _scrollPosLeft;
        private Vector2 _scrollPosRight;

        private List<List<string>> _tableData = new List<List<string>>();
        private bool _isDirty = false;

        // 延迟操作队列
        private Action _deferredAction;

        private readonly string[] _supportedTypes = new string[]
        {
            "int", "float", "string", "bool", "vector2", "vector3",
            "List<int>", "List<float>", "List<string>", "List<vector2>", "List<vector3>"
        };

        #endregion

        #region 顶部工具栏

        [YBoxGroup("新建表格")]
        public string NewFileName;

        [YBoxGroup("新建表格")]
        [YButton("创建新表", 30)]
        [YColor("#88FF88")]
        [YShowIf("HasNewFileName")]
        public void CreateTable()
        {
            if (string.IsNullOrEmpty(NewFileName)) return;
            string path = Path.Combine(ConfigKeys.CsvPath, NewFileName + ".csv");
            if (File.Exists(path)) { EditorUtility.DisplayDialog("错误", "文件已存在！", "确定"); return; }
            if (!Directory.Exists(ConfigKeys.CsvPath)) Directory.CreateDirectory(ConfigKeys.CsvPath);

            File.WriteAllText(path, ConfigKeys.NewCsvTemplate, Encoding.UTF8);

            AssetDatabase.Refresh();
            RefreshFileList();
            SelectFile(NewFileName + ".csv");
            NewFileName = "";

            OpenExternal();
        }

        [YBoxGroup("全局操作")]
        [YButton("一键更新所有 (生成 + 导入)", 40)]
        [YColor("#8888FF")]
        public void GenAndImportAll()
        {
            if (EditorUtility.DisplayDialog("确认", "这将重新生成所有 CSV 的代码并导入数据，是否继续？", "确定", "取消"))
            {
                var files = Directory.GetFiles(ConfigKeys.CsvPath, "*.csv");
                var validFiles = files.Where(f => !IsBackupFile(Path.GetFileName(f))).ToArray();

                foreach (var file in validFiles) ConfigCodeGen.GenerateCode(file);
                AssetDatabase.Refresh();

                try
                {
                    foreach (var file in validFiles) ConfigImporter.ImportData(file);
                    AssetDatabase.Refresh();
                    ShowNotification(new GUIContent("全局更新完成"));
                }
                catch
                {
                    EditorUtility.DisplayDialog("提示", "代码生成已完成。由于结构变化，请等待编译完成后再次点击此按钮以导入数据。", "好的");
                }
            }
        }

        private bool HasNewFileName => !string.IsNullOrEmpty(NewFileName);

        #endregion

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshFileList();
        }

        protected override void OnGUI()
        {
            _deferredAction = null;

            GUILayout.BeginVertical("box");
            base.OnGUI();
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            DrawSidebar();
            DrawContent();
            GUILayout.EndHorizontal();

            if (_deferredAction != null)
            {
                _deferredAction.Invoke();
                _deferredAction = null;
                Repaint();
            }
        }

        private void DrawSidebar()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(250), GUILayout.ExpandHeight(true));

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(40))) RefreshFileList();
            GUILayout.EndHorizontal();

            _scrollPosLeft = GUILayout.BeginScrollView(_scrollPosLeft);
            foreach (var file in _fileList)
            {
                if (!string.IsNullOrEmpty(_searchFilter) && !file.ToLower().Contains(_searchFilter.ToLower())) continue;

                string displayName = Path.GetFileNameWithoutExtension(file);
                if (_selectedFile == file && _isDirty) displayName += " *";

                GUI.backgroundColor = (_selectedFile == file) ? Color.cyan : Color.white;

                if (GUILayout.Button(displayName, EditorStyles.objectFieldThumb, GUILayout.Height(25)))
                {
                    if (CheckSave()) SelectFile(file);
                    GUI.FocusControl(null);
                }

                // 右键菜单支持删除
                if (Event.current.type == EventType.ContextClick && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    var clickedFile = file;
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("删除表格"), false, () =>
                    {
                        if (CheckSave())
                        {
                            _selectedFile = clickedFile;
                            DeleteCurrent();
                        }
                    });
                    menu.ShowAsContext();
                    Event.current.Use();
                }

                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawContent()
        {
            GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

            if (string.IsNullOrEmpty(_selectedFile))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("请选择或新建表格", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            else
            {
                DrawToolbar();
                GUILayout.Space(5);
                DrawTableEditor();
            }

            GUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            string fileName = Path.GetFileNameWithoutExtension(_selectedFile);

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"编辑: {fileName}" + (_isDirty ? " <未保存>" : ""), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("打开文件", EditorStyles.toolbarButton, GUILayout.Width(80))) OpenExternal();
            if (GUILayout.Button("删除表格", EditorStyles.toolbarButton, GUILayout.Width(80))) DeleteCurrent();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = _isDirty ? new Color(1f, 0.5f, 0.5f) : Color.white;
            if (GUILayout.Button("保存并备份 (Save)", GUILayout.Height(30))) SaveCsvWithBackup();
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("更新当前表 (生成+导入)", GUILayout.Height(30))) OneClickUpdate();
            GUILayout.EndHorizontal();
        }

        private void DrawTableEditor()
        {
            if (_tableData == null || _tableData.Count == 0) return;

            _scrollPosRight = GUILayout.BeginScrollView(_scrollPosRight, "box");

            int colCount = _tableData[0].Count;
            float minColWidth = 110f;

            // 1. 绘制列头操作
            GUILayout.BeginHorizontal();
            for (int c = 0; c < colCount; c++)
            {
                GUILayout.BeginHorizontal(GUILayout.Width(minColWidth));
                GUILayout.FlexibleSpace();

                if (c > 0)
                {
                    if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        int index = c;
                        _deferredAction = () => RemoveColumn(index);
                    }
                }
                else
                {
                    GUILayout.Space(20);
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            if (GUILayout.Button("+ 列", EditorStyles.miniButton, GUILayout.Width(40)))
            {
                _deferredAction = () => AddColumn();
            }
            GUILayout.EndHorizontal();

            // 2. 绘制数据行
            int maxDrawRows = Mathf.Min(_tableData.Count, 100);

            for (int r = 0; r < maxDrawRows; r++)
            {
                GUILayout.BeginHorizontal();

                Color contentColor = Color.white;
                if (r == 1) contentColor = Color.yellow;
                else if (r == 2) contentColor = Color.gray;

                GUI.color = contentColor;

                for (int c = 0; c < colCount; c++)
                {
                    string val = _tableData[r][c];

                    // 第一列前两行只读
                    if (c == 0 && (r == 0 || r == 1))
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.TextField(val, GUILayout.Width(minColWidth));
                        }
                    }
                    else if (r == 1) // 类型选择
                    {
                        int currentIndex = Array.IndexOf(_supportedTypes, val);
                        if (currentIndex == -1) currentIndex = 0;

                        int newIndex = EditorGUILayout.Popup(currentIndex, _supportedTypes, GUILayout.Width(minColWidth));
                        if (newIndex != currentIndex)
                        {
                            _tableData[r][c] = _supportedTypes[newIndex];
                            _isDirty = true;
                        }
                    }
                    else
                    {
                        string newVal = EditorGUILayout.TextField(val, GUILayout.Width(minColWidth));
                        if (newVal != val)
                        {
                            _tableData[r][c] = newVal;
                            _isDirty = true;
                        }
                    }

                    GUILayout.Space(5);
                }
                GUI.color = Color.white;

                // 删除行
                if (r >= 3)
                {
                    if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        int index = r;
                        _deferredAction = () =>
                        {
                            _tableData.RemoveAt(index);
                            _isDirty = true;
                        };
                    }
                }
                else
                {
                    GUILayout.Space(24);
                }

                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ 添加数据行"))
            {
                _deferredAction = () => AddRow();
            }

            if (_tableData.Count > 100)
            {
                GUILayout.Label($"... 剩余 {_tableData.Count - 100} 行数据未显示 ...", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.EndScrollView();
        }

        #endregion

        #region 数据操作逻辑

        private void AddColumn()
        {
            if (_tableData.Count == 0) return;
            for (int i = 0; i < _tableData.Count; i++)
            {
                if (i == 0) _tableData[i].Add("NewField");
                else if (i == 1) _tableData[i].Add("string");
                else if (i == 2) _tableData[i].Add("Desc");
                else _tableData[i].Add("");
            }
            _isDirty = true;
        }

        private void RemoveColumn(int colIndex)
        {
            if (colIndex == 0)
            {
                ShowNotification(new GUIContent("ID 列不可删除"));
                return;
            }

            if (EditorUtility.DisplayDialog("警告", "确定删除该列吗？", "删除", "取消"))
            {
                foreach (var row in _tableData)
                {
                    if (colIndex < row.Count) row.RemoveAt(colIndex);
                }
                _isDirty = true;
            }
        }

        private void AddRow()
        {
            int colCount = _tableData.Count > 0 ? _tableData[0].Count : 0;
            var newRow = new List<string>();
            for (int i = 0; i < colCount; i++) newRow.Add("");

            if (colCount > 0 && _tableData.Count > 3)
            {
                if (int.TryParse(_tableData[_tableData.Count - 1][0], out int lastId))
                {
                    newRow[0] = (lastId + 1).ToString();
                }
            }
            _tableData.Add(newRow);
            _isDirty = true;
        }

        #endregion

        #region 文件 IO 与备份

        private void RefreshFileList()
        {
            _fileList.Clear();
            if (!Directory.Exists(ConfigKeys.CsvPath)) Directory.CreateDirectory(ConfigKeys.CsvPath);

            var info = new DirectoryInfo(ConfigKeys.CsvPath);
            var files = info.GetFiles("*.csv");
            foreach (var f in files)
            {
                if (IsBackupFile(f.Name)) continue;
                _fileList.Add(f.Name);
            }
        }

        private bool IsBackupFile(string fileName)
        {
            return Regex.IsMatch(fileName, @"_\d{8}_\d{6}\.csv$");
        }

        private void SelectFile(string fileName)
        {
            _selectedFile = fileName;
            LoadCsvData();
        }

        private bool CheckSave()
        {
            if (_isDirty)
            {
                int option = EditorUtility.DisplayDialogComplex("未保存", $"文件 {_selectedFile} 已修改，是否保存？", "保存", "取消", "不保存");
                if (option == 0) { SaveCsvWithBackup(); return true; }
                if (option == 1) return false;
                if (option == 2) { _isDirty = false; return true; }
            }
            return true;
        }

        private void LoadCsvData()
        {
            _tableData.Clear();
            _isDirty = false;
            string path = GetSelectedPath();
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (var line in lines)
                {
                    _tableData.Add(line.Split(',').ToList());
                }
            }
            catch (Exception e) { Debug.LogError("加载失败: " + e.Message); }
        }

        private void SaveCsvWithBackup()
        {
            if (_tableData == null || _tableData.Count == 0) return;

            string srcPath = GetSelectedPath();

            if (File.Exists(srcPath))
            {
                string dir = Path.GetDirectoryName(srcPath);
                string fileName = Path.GetFileNameWithoutExtension(_selectedFile);
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                string backupPath = Path.Combine(dir, $"{fileName}_{timeStamp}.csv");

                try
                {
                    File.Copy(srcPath, backupPath, true);
                    CleanUpBackups(dir, fileName);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Config] 备份失败: {e.Message}");
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach (var row in _tableData)
            {
                sb.AppendLine(string.Join(",", row));
            }

            File.WriteAllText(srcPath, sb.ToString(), Encoding.UTF8);
            _isDirty = false;
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("保存成功 (已备份)"));
        }

        private void CleanUpBackups(string dir, string baseFileName)
        {
            var info = new DirectoryInfo(dir);
            var backups = info.GetFiles($"{baseFileName}_????????_??????.csv")
                              .OrderByDescending(f => f.CreationTime)
                              .ToList();

            var metaBackups = info.GetFiles($"{baseFileName}_????????_??????.meta")
                              .OrderByDescending(f => f.CreationTime)
                              .ToList();

            if (backups.Count > 3)
            {
                for (int i = 3; i < backups.Count; i++)
                {
                    try { backups[i].Delete(); } catch { }
                }
            }

            if (metaBackups.Count > 3)
            {
                for (int i = 3; i < metaBackups.Count; i++)
                {
                    try { metaBackups[i].Delete(); } catch { }
                }
            }
        }

        private void OneClickUpdate()
        {
            SaveCsvWithBackup();
            ConfigCodeGen.GenerateCode(GetSelectedPath());

            try
            {
                ConfigImporter.ImportData(GetSelectedPath());
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent("更新成功"));
            }
            catch
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("提示", "代码结构发生变化，正在编译。\n请在编译完成后再次点击此按钮以导入数据。", "好的");
            }
        }

        private string GetSelectedPath() => Path.Combine(ConfigKeys.CsvPath, _selectedFile);

        private void OpenExternal()
        {
            string path = GetSelectedPath();
            if (File.Exists(path))
            {
                EditorUtility.OpenWithDefaultApp(Path.GetFullPath(path));
            }
        }

        private void DeleteCurrent()
        {
            if (string.IsNullOrEmpty(_selectedFile)) return;

            if (EditorUtility.DisplayDialog("警告", $"确定彻底删除表格 [{_selectedFile}] 吗？\n此操作将同时删除生成的代码、Asset 文件以及所有备份。", "彻底删除", "取消"))
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(_selectedFile);
                string csvPath = GetSelectedPath();
                string dir = Path.GetDirectoryName(csvPath);

                // 1. 删除 CSV
                File.Delete(csvPath);

                // 2. 删除生成的代码
                string codeFile = Path.Combine(ConfigKeys.CodePath, $"{fileNameNoExt}Table.cs");
                if (File.Exists(codeFile)) File.Delete(codeFile);

                // 3. 删除生成的 Asset
                string assetFile = Path.Combine(ConfigKeys.AssetPath, $"{fileNameNoExt}.asset");
                if (File.Exists(assetFile)) AssetDatabase.DeleteAsset(assetFile);

                // 4. 删除备份文件
                if (Directory.Exists(dir))
                {
                    var info = new DirectoryInfo(dir);
                    var backups = info.GetFiles($"{fileNameNoExt}_????????_??????.csv");
                    foreach (var backup in backups)
                    {
                        try { backup.Delete(); } catch { }
                    }
                    var metas = info.GetFiles($"{fileNameNoExt}_????????_??????.meta");
                    foreach (var meta in metas)
                    {
                        try { meta.Delete(); } catch { }
                    }
                }

                AssetDatabase.Refresh();

                _selectedFile = "";
                _tableData.Clear();
                _isDirty = false;
                RefreshFileList();
                ShowNotification(new GUIContent("表格及备份已删除"));
            }
        }

        #endregion
    }
}
