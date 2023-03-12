using StateMachine;

namespace Player.Movement.StateMachine
{
    public abstract class BaseMovementState : BaseHierarchicalState
    {
        public MovementComponent MovementComponent { get; protected set; }
        public MovementStateMachine StateMachine { get; protected set; }
        public BaseMovementState(BaseHierarchicalState parentState) : base(parentState)
        {
            if (parentState is not BaseMovementState parentMovementState) return;
            
            StateMachine = parentMovementState.StateMachine;
            MovementComponent = parentMovementState.MovementComponent;
        }
    }
}