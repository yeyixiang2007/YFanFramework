using UnityEditor;
using YFan.Utils;

namespace YFan.Editor
{
    /// <summary>
    /// 支持 YFanFramework 属性 (Odin-like) 的编辑器窗口基类
    /// 继承此窗口后，无需写 OnGUI，直接定义变量加属性即可
    /// </summary>
    public class YFanEditorWindow : EditorWindow
    {
        private YFanUIRenderer _renderer;
        private SerializedObject _serializedObject;

        protected virtual void OnEnable()
        {
            // EditorWindow 本身就是 ScriptableObject，所以可以被序列化
            // 这样就能用 EditorGUILayout.PropertyField 绘制窗口里的字段了
            _serializedObject = new SerializedObject(this);
            _renderer = new YFanUIRenderer(this, _serializedObject);
        }

        protected virtual void OnGUI()
        {
            if (_serializedObject == null || _serializedObject.targetObject == null)
            {
                _serializedObject = new SerializedObject(this);
                _renderer = new YFanUIRenderer(this, _serializedObject);
            }

            if (_renderer != null)
            {
                try
                {
                    _renderer.Draw();
                }
                catch (System.Exception e)
                {
                    // 捕获布局错误，防止满屏报错
                    if (e.GetType().Name != "ArgumentException") // 忽略布局计算中的临时参数错误
                    {
                        YLog.Error($"UI布局错误: {e}", "YFanEditorWindow");
                    }
                }
            }
        }
    }
}
