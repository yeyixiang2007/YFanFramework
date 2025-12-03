namespace YFan.Utils
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

        protected virtual void OnInit() { }
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnExit() { }

        // 快捷切换状态
        protected void ChangeState<TState>() where TState : IState<T>
        {
            mFSM.ChangeState<TState>();
        }
    }
}
