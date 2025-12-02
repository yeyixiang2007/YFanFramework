namespace YFan.Editor
{
    /// <summary>
    /// 支持 YFanFramework 属性 (Odin-like) 的编辑器窗口基类
    /// 继承此窗口后，无需写 OnGUI，直接定义变量加属性即可
    /// </summary>
    public class YFanInspector : UnityEditor.Editor
    {
        private YFanUIRenderer _renderer;

        protected virtual void OnEnable()
        {
            if (target != null)
            {
                _renderer = new YFanUIRenderer(target, serializedObject);
            }
        }

        public override void OnInspectorGUI()
        {
            if (_renderer != null)
            {
                _renderer.Draw();
            }
            else
            {
                base.OnInspectorGUI();
            }
        }
    }
}
