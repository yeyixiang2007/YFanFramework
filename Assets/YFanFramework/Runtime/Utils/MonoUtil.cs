using System;
using System.Collections;
using QFramework;
using UnityEngine;
using YFan.Attributes;

namespace YFan.Utils
{
    /// <summary>
    /// 提供 Unity 生命周期事件的转发器
    ///  * 转发 Update、FixedUpdate、Application 事件
    ///  * 提供 Coroutine 驱动（仅用于兼容旧插件）
    /// </summary>
    public interface IMonoUtil : IUtility
    {
        /// <summary>
        /// 添加 Update 事件监听
        /// </summary>
        /// <param name="action"></param>
        void AddUpdateListener(Action action);

        /// <summary>
        /// 移除 Update 事件监听
        /// </summary>
        /// <param name="action"></param>
        void RemoveUpdateListener(Action action);

        /// <summary>
        /// 添加 FixedUpdate 事件监听
        /// </summary>
        /// <param name="action"></param>
        void AddFixedUpdateListener(Action action);

        /// <summary>
        /// 移除 FixedUpdate 事件监听
        /// </summary>
        /// <param name="action"></param>
        void RemoveFixedUpdateListener(Action action);

        /// <summary>
        /// 添加 Coroutine 事件监听
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        Coroutine StartCoroutine(IEnumerator routine);

        /// <summary>
        /// 移除 Coroutine 事件监听
        /// </summary>
        /// <param name="routine"></param>
        void StopCoroutine(Coroutine routine);

        /// <summary>
        /// 添加 ApplicationQuit 事件监听
        /// </summary>
        event Action OnApplicationQuitEvent;

        /// <summary>
        /// 添加 ApplicationPause 事件监听
        /// </summary>
        event Action<bool> OnApplicationPauseEvent;
    }

    [AutoRegister(typeof(IMonoUtil))]
    public class MonoUtil : IMonoUtil
    {
        /// <summary>
        /// 运行时 MonoBehaviour 类，用于转发 Unity 生命周期事件
        /// </summary>
        private class MonoRunner : MonoBehaviour
        {
            public event Action OnUpdateEvent;
            public event Action OnFixedUpdateEvent;
            public event Action OnAppQuitEvent;
            public event Action<bool> OnAppPauseEvent;

            private void Update()
            {
                OnUpdateEvent?.Invoke();
            }

            private void FixedUpdate()
            {
                OnFixedUpdateEvent?.Invoke();
            }

            private void OnApplicationQuit()
            {
                OnAppQuitEvent?.Invoke();
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                OnAppPauseEvent?.Invoke(pauseStatus);
            }
        }

        private static MonoRunner _runner; // 运行时 MonoBehaviour 实例

        public MonoUtil()
        {
            if (_runner == null)
            {
                GameObject go = new GameObject(ConfigKeys.MonoUtilRuntime);
                _runner = go.AddComponent<MonoRunner>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
        }

        #region 接口实现

        public void AddUpdateListener(Action action)
        {
            if (_runner != null) _runner.OnUpdateEvent += action;
        }

        public void RemoveUpdateListener(Action action)
        {
            if (_runner != null) _runner.OnUpdateEvent -= action;
        }

        public void AddFixedUpdateListener(Action action)
        {
            if (_runner != null) _runner.OnFixedUpdateEvent += action;
        }

        public void RemoveFixedUpdateListener(Action action)
        {
            if (_runner != null) _runner.OnFixedUpdateEvent -= action;
        }

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            return _runner.StartCoroutine(routine);
        }

        public void StopCoroutine(Coroutine routine)
        {
            if (_runner != null && routine != null) _runner.StopCoroutine(routine);
        }

        public event Action OnApplicationQuitEvent
        {
            add { if (_runner != null) _runner.OnAppQuitEvent += value; }
            remove { if (_runner != null) _runner.OnAppQuitEvent -= value; }
        }

        public event Action<bool> OnApplicationPauseEvent
        {
            add { if (_runner != null) _runner.OnAppPauseEvent += value; }
            remove { if (_runner != null) _runner.OnAppPauseEvent -= value; }
        }

        #endregion
    }
}
