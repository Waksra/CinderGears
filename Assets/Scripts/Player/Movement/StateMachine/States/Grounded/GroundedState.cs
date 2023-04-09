using StateMachine;
using UnityEngine;

namespace Player.Movement.StateMachine.States.Grounded
{
    public class GroundedState : BaseMovementState
    {
        public GroundedState(BaseHierarchicalState parentState) : base(parentState) { }
        
        public override void Enter()
        {
            base.Enter();
            
            ChangeState(StateFactory.IdleState(this));
            MovementComponent.SetMovementValues(MovementComponent.GroundedValues);
        }

        public override void Update()
        {
            MovementComponent.GroundCheck();
            
            MovementComponent.AdjustTorque(Quaternion.Euler(0, StateMachine.DesiredRotation, 0));

            //TODO: Should stay grounded till coyote time is over.
            if (StateMachine.IsJumpRequested || !MovementComponent.IsGrounded)
                parent.ChangeState(StateFactory.AirborneState(parent));
            
            MovementComponent.ApplyRideForce();
            base.Update();
        }
    }
}