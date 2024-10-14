using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Player.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementComponent : MonoBehaviour
    {
        //Grounded Values
        [SerializeField, TitleGroup("Movement")]
        private MovementValues groundedValues;
        
        //Airborne Values
        [SerializeField, TitleGroup("Movement")]
        private MovementValues airborneValues;
        
        //Slopes
        [SerializeField, Range(0, 90), TitleGroup("Movement/Slopes")]
        private float maxSlopeAngel = 50.0f;

        //Jump
        [SerializeField, Range(0, 8), FoldoutGroup("Jump")]
        private float jumpHeight = 2.0f;

        [SerializeField, Range(0, 1), FoldoutGroup("Jump")]
        private float jumpInputBufferDuration = 0.1f;

        [SerializeField, Range(0, 1), FoldoutGroup("Jump")]
        private float coyoteTimeDuration = 0.1f;

        //Rotation
        [SerializeField, Range(1, 720), FoldoutGroup("Rotation")]
        private float rotationSpeed = 360.0f;
        
        [SerializeField, Range(0, 20), FoldoutGroup("Rotation")]
        private float rotationSpringFrequency = 14.0f;
        
        [SerializeField, Range(-1, 2), FoldoutGroup("Rotation")]
        private float rotationSpringDamping = 1.0f;

        //Ride
        [SerializeField, Range(0, 2), FoldoutGroup("Ride")]
        private float rideHeight = 1.0f;

        [SerializeField, Range(0, 3), FoldoutGroup("Ride")]
        private float groundCheckDistance = 1.5f;

        [SerializeField, Range(0, 20), FoldoutGroup("Ride")]
        private float rideSpringFrequency = 10.0f;

        [SerializeField, Range(0, 20), FoldoutGroup("Ride")]
        private float rideSpringDamping = 1.0f;

        //Internal
        private MovementValues currentValues;
        
        private float rotationProportionalGain;
        private float rotationDerivativeGain;

        private float minGroundDot;

        private Spring.DampedSpringMotionParams rideSpringParams;

        private float timeOfJumpRequestEnd;
        private float timeOfCoyoteTimeEnd;

        private bool isGrounded;
        private bool onSteep;
        private Vector3 groundNormal;
        private float distanceToGround = 1;

        private float desiredRidePosition;

        private Rigidbody body;
        private new Transform transform;
        
        
        //General Properties
        public Vector3 Velocity => body.velocity;
        
        public MovementValues CurrentValues => currentValues;
        public MovementValues GroundedValues => groundedValues;
        public MovementValues AirborneValues => airborneValues;
        
        //Rotation Properties
        public float RotationSpeed => rotationSpeed;
        
        //Jump Properties
        public float JumpInputBufferDuration => jumpInputBufferDuration;
        
        //Grounded Properties
        public bool IsGrounded => isGrounded || Time.realtimeSinceStartup < timeOfCoyoteTimeEnd;
        public float DistanceToGround => distanceToGround;

        
        //Methods
        private void OnValidate()
        {
            minGroundDot = Mathf.Cos(maxSlopeAngel * Mathf.Deg2Rad);
            rideSpringParams =
                Spring.CalcDampedSpringMotionParams(Time.fixedDeltaTime, rideSpringFrequency, rideSpringDamping);
            
            rotationProportionalGain = 6f * rotationSpringFrequency * (6f * rotationSpringFrequency) * 0.25f;
            rotationDerivativeGain = 4.5f * rotationSpringFrequency * rotationSpringDamping;
        }

        private void Awake()
        {
            OnValidate();
            
            body = GetComponent<Rigidbody>();
            transform = GetComponent<Transform>();

            body.maxAngularVelocity = 200;
            body.useGravity = true;
        }

        public void GroundCheck()
        {
            //TODO: Refactor this so that state is held during coyote time
            
            Vector3 position = body.position;
            Ray ray = new Ray(position, Vector3.down);
            
            bool previouslyGrounded = isGrounded;
            bool previouslyOnSteep = onSteep;

            isGrounded = false;
            onSteep = false;
            
            groundNormal = Vector3.up;
            desiredRidePosition = position.y;
            
            Debug.DrawLine(position, position + Vector3.down * groundCheckDistance, Color.red);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, groundCheckDistance))
            {
                groundNormal = hitInfo.normal;
                distanceToGround = hitInfo.distance;
                desiredRidePosition = hitInfo.point.y + rideHeight;

                if (hitInfo.normal.y >= minGroundDot && (previouslyGrounded || distanceToGround < rideHeight))
                    isGrounded = true;
                
                else if (previouslyOnSteep || previouslyGrounded || distanceToGround < rideHeight)
                    onSteep = true;
            }

            if (previouslyGrounded && !isGrounded)
                timeOfCoyoteTimeEnd = Time.realtimeSinceStartup + coyoteTimeDuration;

        }

        public void MoveAlongGround(Vector2 direction)
        {
            //TODO: Reconsider how to handle velocity in a more neat fashion.
            Vector3 velocity = body.velocity;
            
            Vector3 xAxis = ProjectOnContactPlane(transform.right).normalized;
            Vector3 zAxis = ProjectOnContactPlane(transform.forward).normalized;

            Vector2 adjustment = Vector2.zero;
            
            float speed = GetSpeed();
            
            adjustment.x = direction.x * speed - Vector3.Dot(velocity, xAxis);
            adjustment.y = direction.y * speed - Vector3.Dot(velocity, zAxis);
            
            float currentAcceleration = GetAcceleration();
            
            adjustment = Vector2.ClampMagnitude(adjustment, currentAcceleration * Time.deltaTime);
            
            velocity += xAxis * adjustment.x + zAxis * adjustment.y;
            
            body.velocity = velocity;
        }
        
        public void AdjustTorque(Quaternion desiredRotation)
        {
            //TODO: Look into using a PID controller for this, also need to solve full rotations as they can rotate the wrong way.
            
            Quaternion rotation = body.rotation;
            Quaternion rotationError = Math.GetShortestRotation(rotation, desiredRotation);
            
            rotationError.ToAngleAxis(out float rotationAngle, out Vector3 rotationAxis);
            rotationAxis.Normalize();
            rotationAxis *= Mathf.Deg2Rad;
            
            Vector3 correctionalTorque = rotationAxis * (rotationProportionalGain * rotationAngle) - rotationDerivativeGain * body.angularVelocity;
            
            Quaternion rotInertia2World = body.inertiaTensorRotation * rotation;
            
            correctionalTorque = Quaternion.Inverse(rotInertia2World) * correctionalTorque;
            correctionalTorque.Scale(body.inertiaTensor);
            correctionalTorque = rotInertia2World * correctionalTorque;
            
            body.AddTorque(correctionalTorque);
        }

        public void Jump()
        {
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            Vector3 jumpDirection = (groundNormal + Vector3.up).normalized;

            float alignedSpeed = Vector3.Dot(body.velocity, jumpDirection);
            if (alignedSpeed > 0f)
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            else if (alignedSpeed < 0f)
                jumpSpeed -= alignedSpeed;

            body.velocity += jumpDirection * jumpSpeed;

            timeOfCoyoteTimeEnd = 0f;
        }

        public void ApplyRideForce()
        {
            Vector3 velocity = body.velocity;
            Vector3 position = body.position;

            Spring.UpdateDampedSpringMotion(ref position.y, ref velocity.y, desiredRidePosition, rideSpringParams);

            body.MovePosition(position);
            body.velocity = velocity;
        }
        
        public void SetMovementValues(MovementValues values)
        {
            currentValues = values;
        }

        private float GetSpeed()
        {
            return InputHandler.SprintInput.Value ? currentValues.maxSprintSpeed : currentValues.maxSpeed;
        }

        private float GetAcceleration()
        {
            return InputHandler.SprintInput.Value ? currentValues.acceleration : currentValues.sprintAcceleration;
        }

        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - groundNormal * Vector3.Dot(vector, groundNormal);
        }
    }
}
