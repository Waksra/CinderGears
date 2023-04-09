using StateMachine;
using UnityEngine;

namespace Player.Movement.StateMachine.States.General
{
    public class MoveState : BaseMovementState
    {
        private float timeOfEnter;
        public bool hasLogged = false;
        
        public MoveState(BaseHierarchicalState parentState) : base(parentState) { }

        public override void Enter()
        {
            base.Enter();
            timeOfEnter = Time.realtimeSinceStartup;
        }

        public override void Update()
        {
            Vector2 moveInput = StateMachine.MoveInput;
            
            MovementComponent.MoveAlongGround(StateMachine.MoveInput);

            if (moveInput == Vector2.zero)
                parent.ChangeState(StateFactory.IdleState(parent));

            base.Update();
        }
    }
}