using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.Movement
{
    public class MovementController : MonoBehaviour
    {
        //Movement
        [SerializeField, Range(1, 30), TitleGroup("Movement", "Walk")]
        private float moveSpeed = 5.0f;

        [SerializeField, Range(1, 80), TitleGroup("Movement", "Walk")]
        private float acceleration = 20.0f;

        [SerializeField, Range(1, 30), TitleGroup("Movement", "Sprint")]
        private float sprintSpeed = 10.0f;

        [SerializeField, Range(1, 80), TitleGroup("Movement", "Sprint")]
        private float sprintAcceleration = 25.0f;

        [SerializeField, Range(0, 30), TitleGroup("Movement", "Air")]
        private float airAcceleration = 5.0f;

        //Friction
        [SerializeField, Range(0, 5), TitleGroup("Friction")]
        private float stopFriction = 0.5f;

        //Jump
        [SerializeField, Range(0.5f, 8.0f), TitleGroup("Jump")]
        private float jumpHeight = 2.0f;

        [SerializeField, Range(0.0f, 2.0f), TitleGroup("Jump")]
        private float jumpInputBufferDuration = 0.15f;

        [SerializeField, Range(0.0f, 2.0f), TitleGroup("Jump")]
        private float coyoteTimeDuration = 0.2f;

        [SerializeField, Range(0.0f, 3.0f), TitleGroup("Jump")]
        private float fallGravityMultiplier = 2.0f;

        //Walking Angles
        [SerializeField, Range(0, 90), TitleGroup("Walking Angles")]
        private float maxGroundAngle = 45.0f;

        [SerializeField, Range(0, 90), TitleGroup("Walking Angles")]
        private float maxStairsAngle = 60.0f;

        //Layers
        [SerializeField, TitleGroup("Layers")] private LayerMask groundMask = -1;

        [SerializeField, TitleGroup("Layers"), PropertySpace(SpaceBefore = 0, SpaceAfter = 10)]
        private LayerMask stairMask = -1;

        //Ground Snap
        [SerializeField, FoldoutGroup("Ground Snap")]
        private float maxSnapSpeed = 100.0f;

        [SerializeField, FoldoutGroup("Ground Snap")]
        private float probeDistance = 1.5f;

        [SerializeField, FoldoutGroup("Ground Snap")]
        private LayerMask probeMask = -1;


        private Vector2 moveInput;
        private Vector3 velocity;

        private Vector3 rightAxis;
        private Vector3 forwardAxis;

        private float minGroundDot;
        private float minStairsDot;

        private int stepsSinceLastGrounded;
        private int stepsSinceLastJump;

        private int groundContactCount;
        private int steepContactCount;

        private Vector3 contactNormal;
        private Vector3 steepNormal;
        private Vector3 lastContactNormal;

        private bool jumpRequested;
        private float timeOfJumpRequestEnd;
        private float coyoteTimeEnd;

        private Rigidbody body;
        private Transform inputSpace;

        public bool OnGround => groundContactCount > 0;

        private void OnValidate()
        {
            minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            minStairsDot = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
        }

        private void Awake()
        {
            OnValidate();
            body = GetComponent<Rigidbody>();
            inputSpace = Camera.main.transform;

            body.useGravity = false;
        }

        private void Update()
        {
            if (!jumpRequested && InputHandler.JumpInput.Value)
            {
                jumpRequested = InputHandler.JumpInput.Consume();
                timeOfJumpRequestEnd = Time.realtimeSinceStartup + jumpInputBufferDuration;
            }
            else if (Time.realtimeSinceStartup >= timeOfJumpRequestEnd)
                jumpRequested = false;
        }

        private void FixedUpdate()
        {
            UpdateState();
            AdjustVelocity();
            ApplyFriction();
            ApplyGravity();

            if (jumpRequested)
                Jump();

            ClearState();
            body.velocity = velocity;
        }

        private void UpdateState()
        {
            moveInput = InputHandler.MoveInput.Value;
            velocity = body.velocity;

            stepsSinceLastGrounded++;
            stepsSinceLastJump++;

            rightAxis = Vector3.ProjectOnPlane(inputSpace.right, Vector3.up);
            forwardAxis = Vector3.ProjectOnPlane(inputSpace.forward, Vector3.up);

            if (OnGround || SnapToGround() || CheckForSteepContacts())
            {
                stepsSinceLastGrounded = 0;
                if (groundContactCount > 1)
                    contactNormal.Normalize();
                if (stepsSinceLastJump > 2)
                    coyoteTimeEnd = Time.realtimeSinceStartup + coyoteTimeDuration;
            }
            else
            {
                contactNormal = Vector3.up;
            }
        }

        private void AdjustVelocity()
        {
            if (moveInput.sqrMagnitude == 0 && !OnGround)
                return;

            Vector3 xAxis = ProjectOnContactPlane(rightAxis).normalized;
            Vector3 zAxis = ProjectOnContactPlane(forwardAxis).normalized;

            Vector2 adjustment = Vector2.zero;

            float maxSpeed = InputHandler.SprintInput.Value && moveInput.y > 0 ? sprintSpeed : moveSpeed;

            adjustment.x = moveInput.x * maxSpeed - Vector3.Dot(velocity, xAxis);
            adjustment.y = moveInput.y * maxSpeed - Vector3.Dot(velocity, zAxis);

            float currentAcceleration = GetAcceleration();

            adjustment = Vector2.ClampMagnitude(adjustment, currentAcceleration * Time.deltaTime);

            velocity += xAxis * adjustment.x + zAxis * adjustment.y;
        }

        private void ApplyGravity()
        {
            //If we're on the ground apply gravity perpendicular to the ground.
            if (OnGround)
                velocity += contactNormal * (Vector3.Dot(Physics.gravity, contactNormal) * Time.deltaTime);

            //If we're falling, apply gravity with a multiplier.
            else if (!OnGround && velocity.y < 0)
                velocity += Physics.gravity * (fallGravityMultiplier * Time.deltaTime);

            //If we're not on the ground and not falling, apply normal gravity.
            else
                velocity += Physics.gravity * Time.deltaTime;
        }

        private void ApplyFriction()
        {
            //If we're not moving and on ground, apply friction to stop us.
            if (moveInput != Vector2.zero || !OnGround)
                return;

            velocity -= velocity * (stopFriction * Time.deltaTime);
        }

        private void ClearState()
        {
            lastContactNormal = contactNormal;
            
            groundContactCount = 0;
            contactNormal = Vector3.zero;
            steepContactCount = 0;
            steepNormal = Vector3.zero;
        }

        private void Jump()
        {
            if (!OnGround && Time.realtimeSinceStartup >= coyoteTimeEnd)
                return;

            stepsSinceLastJump = 0;

            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            Vector3 jumpDirection = (contactNormal + Vector3.up).normalized;

            float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
            if (alignedSpeed > 0f)
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            else if (alignedSpeed < 0f)
                jumpSpeed -= alignedSpeed;

            velocity += jumpDirection * jumpSpeed;

            jumpRequested = false;
            coyoteTimeEnd = 0f;
        }

        private bool SnapToGround()
        {
            if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
                return false;

            float speed = velocity.magnitude;
            if (speed > maxSnapSpeed)
                return false;


            if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, probeDistance, probeMask,
                    QueryTriggerInteraction.Ignore))
                return false;

            if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
                return false;

            groundContactCount = 1;
            contactNormal = hit.normal;

            //Get the dot and check if we're already aligned with the ground or not.
            float dot = Vector3.Dot(velocity.normalized, hit.normal);
            if (dot > 0f && lastContactNormal == hit.normal)
            {
                //If the contact normal is the same but we're not aligned we project the velocity.
                velocity = (velocity - hit.normal * dot).normalized * speed;
            }
            else if (dot > 0f)
            {
                //Otherwise we reflect the velocity to align with the ground.
                Vector3 reflectionNormal = -(hit.normal + lastContactNormal).normalized;
                velocity = Vector3.Reflect(velocity, reflectionNormal);
            }

            return true;
        }

        private bool CheckForSteepContacts()
        {
            if (steepContactCount > 1)
            {
                steepNormal.Normalize();
                if (steepNormal.y > minGroundDot)
                {
                    groundContactCount = 1;
                    contactNormal = steepNormal;
                    return true;
                }
            }

            return false;
        }

        private void EvaluateCollisions(Collision other)
        {
            float minDot = GetMinDot(other.gameObject.layer);
            for (int i = 0; i < other.contactCount; i++)
            {
                Vector3 normal = other.GetContact(i).normal;
                if (normal.y >= minDot)
                {
                    groundContactCount++;
                    contactNormal += normal;
                }
                else if (normal.y > -0.01f)
                {
                    steepContactCount++;
                    steepNormal += normal;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollisions(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            EvaluateCollisions(collision);
        }

        private float GetAcceleration()
        {
            if (!OnGround)
                return airAcceleration;

            if (InputHandler.SprintInput.Value && moveInput.y > 0)
                return sprintAcceleration;

            return acceleration;
        }

        private float GetMinDot(int layer)
        {
            return (stairMask & (1 << layer)) == 0 ? minGroundDot : minStairsDot;
        }

        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - contactNormal * Vector3.Dot(vector, contactNormal);
        }
    }
}