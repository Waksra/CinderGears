using UnityEngine;

namespace Player.Movement.StateMachine
{
    public class MovementStateMachine : BaseMovementState
    {
        private readonly PlayerController playerController;
        
        public Vector2 MoveInput => playerController.MoveInput;
        public float DesiredRotation => playerController.DesiredRotation;
        public bool IsJumpRequested => playerController.IsJumpRequested;

        public MovementStateMachine(PlayerController playerController) : base(null)
        {
            this.playerController = playerController;
            MovementComponent = playerController.GetComponent<MovementComponent>();
            StateMachine = this;
        }

        public override void Enter()
        {
            base.Enter();
            ChangeState(StateFactory.AirborneState(this));
        }
    }
}