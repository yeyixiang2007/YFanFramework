using System;
using System.Collections.Generic;

namespace YFan.Runtime.Utils
{
    /// <summary>
    /// 有限状态机管理器
    /// </summary>
    /// <typeparam name="T">所有者类型</typeparam>
    public class FSM<T>
    {
        // 状态字典: Type -> StateInstance
        private readonly Dictionary<Type, IState<T>> _states = new Dictionary<Type, IState<T>>();

        public IState<T> CurrentState { get; private set; }
        public IState<T> PreviousState { get; private set; }
        public T Owner { get; private set; }

        // 构造时传入 Owner
        public FSM(T owner)
        {
            Owner = owner;
        }

        #region 状态管理

        /// <summary>
        /// 添加状态 (自动初始化)
        /// </summary>
        public void AddState(IState<T> state)
        {
            var type = state.GetType();
            if (_states.ContainsKey(type))
            {
                YLog.Warn($"FSM 已存在状态: {type.Name}", "FSM");
                return;
            }

            state.OnInit(this, Owner);
            _states.Add(type, state);
        }

        /// <summary>
        /// 启动状态机 (设置初始状态)
        /// </summary>
        public void StartState<TState>() where TState : IState<T>
        {
            if (CurrentState != null)
            {
                YLog.Warn("FSM 已经启动过了，请使用 ChangeState", "FSM");
                return;
            }

            ChangeState<TState>();
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        public void ChangeState<TState>() where TState : IState<T>
        {
            Type newType = typeof(TState);

            if (!_states.TryGetValue(newType, out var newState))
            {
                YLog.Error($"FSM 找不到状态: {newType.Name}，请先 AddState", "FSM");
                return;
            }

            // 退出旧状态
            if (CurrentState != null)
            {
                PreviousState = CurrentState;
                CurrentState.OnExit();
            }

            // 进入新状态
            CurrentState = newState;
            // YLog.Info($"切换状态: {newType.Name}", "FSM"); // Debug模式可开启
            CurrentState.OnEnter();
        }

        #endregion

        #region 状态查询

        /// <summary>
        /// 是否当前状态是指定类型
        /// </summary>
        public bool IsCurrentState<TState>() where TState : IState<T>
        {
            return CurrentState is TState;
        }

        /// <summary>
        /// 是否上一个状态是指定类型
        /// </summary>
        public bool IsPreviousState<TState>() where TState : IState<T>
        {
            return PreviousState is TState;
        }

        #endregion

        #region 状态轮询

        /// <summary>
        /// 轮询更新 (需要在 Owner 的 Update 中调用)
        /// </summary>
        public void OnUpdate() => CurrentState?.OnUpdate();

        /// <summary>
        /// 物理更新 (需要在 Owner 的 FixedUpdate 中调用)
        /// </summary>
        public void OnFixedUpdate() => CurrentState?.OnFixedUpdate();

        #endregion

        /// <summary>
        /// 清理引用
        /// </summary>
        public void Clear()
        {
            CurrentState = null;
            PreviousState = null;
            _states.Clear();
        }
    }
}
