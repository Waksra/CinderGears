using StateMachine;
using UnityEngine;

namespace Player.Movement.StateMachine.States.General
{
    public class IdleState : BaseMovementState
    {
        public IdleState(BaseHierarchicalState parentState) : base(parentState) { }

        public override void Update()
        {
            Vector2 moveInput = StateMachine.MoveInput;
            
            MovementComponent.MoveAlongGround(StateMachine.MoveInput);

            if (moveInput == Vector2.zero)
                ChangeState(StateFactory.MoveState(this));
            
            base.Update();
        }
    }
}