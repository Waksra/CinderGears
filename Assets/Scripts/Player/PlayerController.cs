using Player.Movement;
using Player.Movement.StateMachine;
using UnityEngine;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        private MovementComponent movementComponent;
        private MovementStateMachine movementStateMachine;

        private void Awake()
        {
            movementComponent = GetComponent<MovementComponent>();
            
            movementStateMachine = new MovementStateMachine(this);
            movementStateMachine.Enter();
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