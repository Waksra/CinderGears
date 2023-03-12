namespace Player.Movement.StateMachine
{
    public class MovementStateMachine : BaseMovementState
    {
        private PlayerController playerController;

        public MovementStateMachine(PlayerController playerController) : base(null)
        {
            this.playerController = playerController;
            MovementComponent = playerController.GetComponent<MovementComponent>();
            StateMachine = this;
        }

        public override void Enter()
        {
            base.Enter();
            SetCurrentState(StateFactory.GroundedState(this));
        }
    }
}