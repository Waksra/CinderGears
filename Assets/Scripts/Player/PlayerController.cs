using Player.Movement;
using Player.Movement.StateMachine;
using UnityEngine;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        private MovementComponent movementComponent;
        private MovementStateMachine movementStateMachine;

        private float timeOfJumpRequestEnd;
        
        //Input
        public Vector2 MoveInput { get; set; }
        public float DesiredRotation { get; set; }
        public bool IsJumpRequested { get; set; }

        private void Awake()
        {
            movementComponent = GetComponent<MovementComponent>();
            
            movementStateMachine = new MovementStateMachine(this);
            movementStateMachine.Enter();

            DesiredRotation = transform.rotation.eulerAngles.y;
        }

        private void Update()
        {
            MoveInput = InputHandler.MoveInput;
            
            DesiredRotation += InputHandler.LookInput.Value.x * movementComponent.RotationSpeed * Time.deltaTime;
            
            if (InputHandler.JumpInput)
            {
                IsJumpRequested = InputHandler.JumpInput.Consume();
                timeOfJumpRequestEnd = Time.realtimeSinceStartup + movementComponent.JumpInputBufferDuration;
            }
            else if (IsJumpRequested && Time.realtimeSinceStartup >= timeOfJumpRequestEnd)
            {
                IsJumpRequested = false;
            }
        }

        private void FixedUpdate()
        {
            movementStateMachine.Update();
        }

        private void OnDestroy()
        {
            movementStateMachine.Exit();
        }
    }
}