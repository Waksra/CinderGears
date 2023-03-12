namespace StateMachine
{
    public abstract class BaseHierarchicalState
    {
        protected BaseHierarchicalState parentState;
        protected BaseHierarchicalState currentState;
        
        public BaseHierarchicalState(BaseHierarchicalState parentState)
        {
            this.parentState = parentState;
        }
        
        public void SetCurrentState(BaseHierarchicalState state)
        {
            currentState?.Exit();
            currentState = state;
            currentState?.Enter();
        }
        
        public void ChangeTopState(BaseHierarchicalState state)
        {
            if (parentState == null)
                SetCurrentState(state);
            else
                parentState.ChangeTopState(state);
        }

        public virtual void Enter() { }

        public virtual void Update()
        {
            currentState?.Update();
        }

        public virtual void Exit()
        {
            currentState?.Exit();
        }
    }
}