using Player.Movement.StateMachine.States.Grounded;
using StateMachine;

namespace Player.Movement.StateMachine
{
    public static class StateFactory
    {
        public static GroundedState GroundedState(BaseHierarchicalState parentState)
        {
            return new GroundedState(parentState);
        }
        
        public static IdleState IdleState(BaseHierarchicalState parentState)
        {
            return new IdleState(parentState);
        }
    }
}