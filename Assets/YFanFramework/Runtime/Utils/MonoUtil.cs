using System;
using System.Collections;
using QFramework;
using UnityEngine;
using YFan.Attributes;
using YFan.Base;

namespace YFan.Utils
{
    /// <summary>
    /// 提供 Unity 生命周期事件的转发器
    ///  * 转发 Update、FixedUpdate、Application 事件
    ///  * 提供 Coroutine 驱动（仅用于兼容旧插件）
    /// </summary>
    public interface IMonoUtil : IUtility
    {
        // --- Update 驱动 ---
        void AddUpdateListener(Action action);
        void RemoveUpdateListener(Action action);

        // --- FixedUpdate 驱动 ---
        void AddFixedUpdateListener(Action action);
        void RemoveFixedUpdateListener(Action action);

        // --- Coroutine 驱动（仅用于兼容旧插件） ---
        Coroutine StartCoroutine(IEnumerator routine);
        void StopCoroutine(Coroutine routine);

        // --- Application 事件驱动 ---
        event Action OnApplicationQuitEvent;
        event Action<bool> OnApplicationPauseEvent;
    }

    /// <summary>
    /// 这里的 MonoUtil 仅作为 Unity 生命周期的 "转发器"
    /// 具体的异步逻辑全部使用 UniTask
    /// </summary>
    [AutoRegister(typeof(IMonoUtil))]
    public class MonoUtil : IMonoUtil
    {
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

        private static MonoRunner _runner;

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
