using StateMachine;
using UnityEngine;

namespace Player.Movement.StateMachine.States.Airborne
{
    public class AirborneState : BaseMovementState
    {
        public AirborneState(BaseHierarchicalState parentState) : base(parentState) { }

        private float distanceToGroundStart;
        
        public override void Enter()
        {
            base.Enter();
            
            ChangeState(StateFactory.IdleState(this));
            MovementComponent.SetMovementValues(MovementComponent.AirborneValues);

            //TODO: The distance to ground is not set correctly when starting the game
            distanceToGroundStart = MovementComponent.DistanceToGround;
            
            if (StateMachine.IsJumpRequested)
                MovementComponent.Jump();
        }

        public override void Update()
        {
            MovementComponent.GroundCheck();
            
            MovementComponent.AdjustTorque(Quaternion.Euler(0, StateMachine.DesiredRotation, 0));

            if (MovementComponent.IsGrounded && MovementComponent.DistanceToGround < distanceToGroundStart)
                parent.ChangeState(StateFactory.GroundedState(parent));

            base.Update();
        }
    }
}