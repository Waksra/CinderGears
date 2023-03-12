using StateMachine;

namespace Player.Movement.StateMachine.States.Grounded
{
    public class IdleState : BaseMovementState
    {
        public IdleState(BaseHierarchicalState parentState) : base(parentState) { }

        public override void Update()
        {
            MovementComponent.AdjustVelocity();
            base.Update();
        }
    }
}