using StateMachine;

namespace Player.Movement.StateMachine.States.Grounded
{
    public class GroundedState : BaseMovementState
    {
        public GroundedState(BaseHierarchicalState parentState) : base(parentState) { }
        
        public override void Enter()
        {
            base.Enter();
            SetCurrentState(StateFactory.IdleState(this));
        }

        public override void Update()
        {
            MovementComponent.GroundCheck();
            MovementComponent.UpdateState();

            if (!MovementComponent.IsGrounded)
            {
                //TOOD: Change to falling state
            }
            
            MovementComponent.ApplyRideForce();
            MovementComponent.AdjustTorque();
            base.Update();
        }
    }
}