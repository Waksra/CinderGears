using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Player.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementComponent : MonoBehaviour
    {
        //Movement
        [FoldoutGroup("Movement")]

        //-Walk
        [SerializeField, Range(1, 30), TitleGroup("Movement/Walk")]
        private float moveSpeed = 5.0f;

        [SerializeField, Range(1, 80), TitleGroup("Movement/Walk")]
        private float acceleration = 20.0f;

        //-Sprint
        [SerializeField, Range(1, 30), TitleGroup("Movement/Sprint")]
        private float sprintSpeed = 10.0f;

        [SerializeField, Range(1, 80), TitleGroup("Movement/Sprint")]
        private float sprintAcceleration = 25.0f;

        //-Air
        [SerializeField, Range(1, 30), TitleGroup("Movement/Air")]
        private float airMoveSpeed = 5.0f;

        [SerializeField, Range(1, 80), TitleGroup("Movement/Air")]
        private float airAcceleration = 20.0f;

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
        [SerializeField, Range(1, 50), FoldoutGroup("Rotation")]
        private float rotationSpeed = 10.0f;
        
        [SerializeField, Range(0, 20), FoldoutGroup("Rotation")]
        private float rotationSpringFrequency = 14.0f;
        
        [SerializeField, Range(-1, 2), FoldoutGroup("Rotation")]
        private float rotationSpringDamping = 1.0f;

        //Ride
        [SerializeField, Range(0, 2), FoldoutGroup("Ride")]
        private float rideHeight = 1.0f;

        [SerializeField, Range(0, 2), FoldoutGroup("Ride")]
        private float groundCheckDistance = 1.5f;

        [SerializeField, Range(0, 20), FoldoutGroup("Ride")]
        private float rideSpringFrequency = 10.0f;

        [SerializeField, Range(0, 20), FoldoutGroup("Ride")]
        private float rideSpringDamping = 1.0f;

        //Internal
        private Vector3 velocity;
        private Vector2 moveInput;
        private Vector2 lookInput;

        private Vector3 rightAxis;
        private Vector3 forwardAxis;

        private float desiredYRotation;
        private Quaternion desiredRotation;
        private float rotationProportionalGain;
        private float rotationDerivativeGain;

        private float minGroundDot;

        private Spring.DampedSpringMotionParams rideSpringParams;

        private bool isJumpRequested;
        private float timeOfJumpRequestEnd;
        private float timeOfCoyoteTimeEnd;

        private int stepsSinceLastGrounded;
        private int stepsSinceLastJump;

        private bool isGrounded;
        private Vector3 groundNormal;

        private float desiredRidePosition;

        private Rigidbody body;
        private Transform inputSpace;
        
        public bool IsGrounded => isGrounded;

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
            inputSpace = Camera.main.transform;
            body = GetComponent<Rigidbody>();

            body.maxAngularVelocity = 200;
            body.useGravity = true;

            desiredYRotation = body.rotation.eulerAngles.y;
        }

        private void Update()
        {
            if (!isJumpRequested && InputHandler.JumpInput)
            {
                isJumpRequested = InputHandler.JumpInput.Consume();
                timeOfJumpRequestEnd = Time.time + jumpInputBufferDuration;
            }
            else if (Time.realtimeSinceStartup >= timeOfJumpRequestEnd)
            {
                isJumpRequested = false;
            }
        }

        // private void FixedUpdate()
        // {
        //     GroundCheck();
        //     UpdateState();
        //
        //     if (isGrounded)
        //         ApplyRideForce();
        //
        //     AdjustVelocity();
        //     AdjustTorque();
        //
        //     if (isJumpRequested)
        //         Jump();
        //
        //     body.velocity = velocity;
        // }

        public void UpdateState()
        {
            moveInput = InputHandler.MoveInput.Value;
            lookInput = InputHandler.LookInput.Value;

            velocity = body.velocity;

            stepsSinceLastGrounded++;
            stepsSinceLastJump++;

            rightAxis = Vector3.ProjectOnPlane(inputSpace.right, Vector3.up);
            forwardAxis = Vector3.ProjectOnPlane(inputSpace.forward, Vector3.up);

            desiredYRotation += lookInput.x * Time.fixedDeltaTime * rotationSpeed;
            desiredRotation = Quaternion.Euler(0, desiredYRotation, 0);

            if (isGrounded)
            {
                stepsSinceLastGrounded = 0;
                if (stepsSinceLastJump > 2)
                    timeOfCoyoteTimeEnd = Time.realtimeSinceStartup + coyoteTimeDuration;
            }
            else
            {
                groundNormal = Vector3.up;
            }
        }

        public void GroundCheck()
        {
            Vector3 position = body.position;
            Ray ray = new Ray(position, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, groundCheckDistance) && hitInfo.normal.y >= minGroundDot)
            {
                isGrounded = true;
                groundNormal = hitInfo.normal;
                desiredRidePosition = hitInfo.point.y + rideHeight;
            }
            else
            {
                isGrounded = false;
                groundNormal = Vector3.up;
                desiredRidePosition = position.y;
            }

            Debug.DrawLine(position, position + Vector3.down * groundCheckDistance, Color.red);
        }

        public void AdjustVelocity()
        {
            if (moveInput.sqrMagnitude == 0 && !isGrounded)
                return;

            Vector3 xAxis = ProjectOnContactPlane(rightAxis).normalized;
            Vector3 zAxis = ProjectOnContactPlane(forwardAxis).normalized;

            Vector2 adjustment = Vector2.zero;

            float speed = GetSpeed();

            adjustment.x = moveInput.x * speed - Vector3.Dot(velocity, xAxis);
            adjustment.y = moveInput.y * speed - Vector3.Dot(velocity, zAxis);

            float currentAcceleration = GetAcceleration();

            adjustment = Vector2.ClampMagnitude(adjustment, currentAcceleration * Time.deltaTime);

            velocity += xAxis * adjustment.x + zAxis * adjustment.y;
            
            //TODO: Move this, perhaps to separate function or just always apply change here.
            body.velocity = velocity;
        }

        public void AdjustTorque()
        {
            Quaternion rotation = transform.rotation;
            Quaternion rotationError = Utils.Math.GetShortestRotation(rotation, desiredRotation);

            rotationError.ToAngleAxis(out float xMag, out Vector3 x);
            x.Normalize();
            x *= Mathf.Deg2Rad;

            Vector3 correctionalTorque = x * (rotationProportionalGain * xMag) - rotationDerivativeGain * body.angularVelocity;

            Quaternion rotInertia2World = body.inertiaTensorRotation * rotation;

            correctionalTorque = Quaternion.Inverse(rotInertia2World) * correctionalTorque;
            correctionalTorque.Scale(body.inertiaTensor);
            correctionalTorque = rotInertia2World * correctionalTorque;

            body.AddTorque(correctionalTorque);
        }

        public void Jump()
        {
            if (!isGrounded && Time.realtimeSinceStartup >= timeOfCoyoteTimeEnd)
                return;

            stepsSinceLastJump = 0;

            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            Vector3 jumpDirection = (groundNormal + Vector3.up).normalized;

            float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
            if (alignedSpeed > 0f)
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            else if (alignedSpeed < 0f)
                jumpSpeed -= alignedSpeed;

            velocity += jumpDirection * jumpSpeed;

            isJumpRequested = false;
            timeOfCoyoteTimeEnd = 0f;
        }

        public void ApplyRideForce()
        {
            Vector3 position = body.position;

            Spring.UpdateDampedSpringMotion(ref position.y, ref velocity.y, desiredRidePosition, rideSpringParams);

            body.MovePosition(position);
        }

        private float GetSpeed()
        {
            if (!isGrounded)
            {
                Vector2 horizontalVelocity = new Vector2(velocity.x, velocity.z);
                return Mathf.Max(horizontalVelocity.magnitude, airMoveSpeed);
            }

            return InputHandler.SprintInput.Value && moveInput.y > 0 ? sprintSpeed : moveSpeed;
        }

        private float GetAcceleration()
        {
            if (!isGrounded)
                return airAcceleration;

            if (InputHandler.SprintInput.Value && moveInput.y > 0)
                return sprintAcceleration;

            return acceleration;
        }

        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - groundNormal * Vector3.Dot(vector, groundNormal);
        }
    }
}