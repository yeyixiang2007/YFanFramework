using UnityEditor;
using YFan.Runtime.Base.Abstract;

namespace YFan.Editor
{
    /// <summary>
    /// 抽象控制器编辑器
    /// * 继承自AbstractController的控制器类
    /// * 提供基础的属性绘制功能
    /// </summary>
    [CustomEditor(typeof(AbstractController), true)]
    [CanEditMultipleObjects]
    public class AbstractControllerEditor : YFanInspector { }
}
