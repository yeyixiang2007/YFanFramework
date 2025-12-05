namespace YFan.Runtime.Utils
{
    /// <summary>
    /// 状态接口
    /// T: 状态的所有者类型 (如 PlayerController, GameFlowSystem)
    /// </summary>
    public interface IState<T>
    {
        void OnInit(FSM<T> fsm, T owner);
        void OnEnter();
        void OnUpdate();
        void OnFixedUpdate();
        void OnExit();
    }

    /// <summary>
    /// 状态基类 (推荐继承此方便写逻辑)
    /// </summary>
    public abstract class AbstractState<T> : IState<T>
    {
        protected FSM<T> mFSM;
        protected T mOwner;

        public void OnInit(FSM<T> fsm, T owner)
        {
            mFSM = fsm;
            mOwner = owner;
            OnInit();
        }

        // --- 子类重写以下方法 ---

        /// <summary>
        /// 初始化状态 (可选)
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// 进入状态 (可选)
        /// </summary>
        public virtual void OnEnter() { }

        /// <summary>
        /// 轮询更新 (可选)
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// 物理更新 (可选)
        /// </summary>
        public virtual void OnFixedUpdate() { }

        /// <summary>
        /// 退出状态 (可选)
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// 切换状态 (可选)
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        protected void ChangeState<TState>() where TState : IState<T>
        {
            mFSM.ChangeState<TState>();
        }
    }
}
