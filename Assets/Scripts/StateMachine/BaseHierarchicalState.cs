namespace StateMachine
{
    public abstract class BaseHierarchicalState
    {
        protected readonly BaseHierarchicalState parent;
        protected BaseHierarchicalState child;
        
        public BaseHierarchicalState(BaseHierarchicalState parent)
        {
            this.parent = parent;
        }
        
        public void ChangeState(BaseHierarchicalState state)
        {
            child?.Exit();
            child = state;
            child?.Enter();
        }

        public virtual void Enter() { }

        public virtual void Update()
        {
            child?.Update();
        }

        public virtual void Exit()
        {
            child?.Exit();
        }
    }
}