using UnityEngine;

namespace Warspite.Player
{
    /// <summary>
    /// Momentum-based character movement with inertia.
    /// WASD input gradually changes velocity instead of instant direction changes.
    /// Includes wall collision bounce/disruption.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class MomentumLocomotion : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float deceleration = 15f;
        [SerializeField] private float airControl = 0.3f;

        [Header("Physics")]
        [SerializeField] private float gravity = 20f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private float wallBounceThreshold = 5f; // Speed needed for bounce
        [SerializeField] private float wallBounceFactor = 0.3f;

        private CharacterController controller;
        private WallWalking wallWalking; // Optional wall walking component
        private Vector3 velocity;
        private Vector3 lastMoveDirection;
        private bool isGrounded;

        public Vector3 Velocity => velocity;
        public bool IsGrounded => isGrounded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            wallWalking = GetComponent<WallWalking>(); // Optional
        }

        void Update()
        {
            CheckGrounded();
            HandleMovement();
            ApplyGravity();
            
            // Move and handle collisions
            CollisionFlags collisionFlags = controller.Move(velocity * Time.deltaTime);
            HandleCollisions(collisionFlags);
        }

        private void CheckGrounded()
        {
            // Use gravity direction for ground check (works on walls/ceiling)
            Vector3 downDirection = wallWalking != null ? wallWalking.GravityDirection : Vector3.down;
            
            isGrounded = Physics.Raycast(
                transform.position,
                downDirection,
                groundCheckDistance + controller.height * 0.5f
            );
        }

        private void HandleMovement()
        {
            // Get input (using real-time delta for consistent feel)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(h, 0, v).normalized;
            
            // Debug logging
            if (wallWalking != null && wallWalking.IsWallWalking && inputDir.sqrMagnitude > 0.01f)
            {
                Debug.Log($"Wall-walking input: h={h}, v={v}, inputDir={inputDir}");
            }

            // Transform input to world space relative to camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                if (inputDir.sqrMagnitude > 0.01f)
                    Debug.LogError("MomentumLocomotion: No camera tagged as 'MainCamera' found!");
                return;
            }

            Transform camTransform = mainCam.transform;
            
            // Get the "up" direction (opposite of gravity)
            Vector3 upDirection = wallWalking != null ? -wallWalking.GravityDirection : Vector3.up;
            
            // When wall-walking, use character's orientation instead of camera
            Vector3 forward, right;
            if (wallWalking != null && wallWalking.IsWallWalking)
            {
                // Use character's forward/right which are aligned to the wall surface
                forward = transform.forward;
                right = transform.right;
            }
            else
            {
                // Normal movement: project camera directions onto the ground plane
                forward = Vector3.ProjectOnPlane(camTransform.forward, upDirection).normalized;
                right = Vector3.ProjectOnPlane(camTransform.right, upDirection).normalized;
            }
            
            Vector3 desiredMoveDir = (forward * inputDir.z + right * inputDir.x).normalized;
            
            // Debug logging for wall-walking
            if (wallWalking != null && wallWalking.IsWallWalking && inputDir.sqrMagnitude > 0.01f)
            {
                Debug.Log($"Wall-walking movement: upDir={upDirection}, forward={forward}, right={right}, desiredDir={desiredMoveDir}");
            }

            // Calculate velocity in the plane perpendicular to gravity
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, upDirection);

            if (desiredMoveDir.sqrMagnitude > 0.01f)
            {
                // Accelerate towards desired direction
                Vector3 targetVelocity = desiredMoveDir * maxSpeed;
                float accelRate = isGrounded ? acceleration : acceleration * airControl;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetVelocity,
                    accelRate * Time.deltaTime
                );
            }
            else
            {
                // Decelerate when no input
                float decelRate = isGrounded ? deceleration : deceleration * airControl;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    decelRate * Time.deltaTime
                );
            }

            // Combine horizontal movement with gravity component
            Vector3 gravityComponent = Vector3.Project(velocity, upDirection);
            velocity = horizontalVelocity + gravityComponent;
            
            // Debug final velocity
            if (wallWalking != null && wallWalking.IsWallWalking && inputDir.sqrMagnitude > 0.01f)
            {
                Debug.Log($"Wall-walking velocity: horizontal={horizontalVelocity}, gravity={gravityComponent}, final={velocity}");
            }
        }

        private void ApplyGravity()
        {
            // Use custom gravity from WallWalking if available
            if (wallWalking != null && wallWalking.IsWallWalking)
            {
                Vector3 gravityVector = wallWalking.GetGravityVector();
                Vector3 upDirection = -wallWalking.GravityDirection;
                
                // Get velocity component in gravity direction
                float verticalSpeed = Vector3.Dot(velocity, upDirection);
                
                if (isGrounded && verticalSpeed < 0)
                {
                    // Grounded - apply small stick force
                    velocity += wallWalking.GravityDirection * 2f * Time.deltaTime;
                }
                else
                {
                    // In air - apply full gravity
                    velocity += gravityVector * Time.deltaTime;
                }
            }
            else
            {
                // Default gravity behavior
                if (isGrounded && velocity.y < 0)
                {
                    velocity.y = -2f; // Small downward force to keep grounded
                }
                else
                {
                    velocity.y -= gravity * Time.deltaTime;
                }
            }
        }

        private void HandleCollisions(CollisionFlags flags)
        {
            // Wall bounce on high-speed collision
            if ((flags & CollisionFlags.Sides) != 0)
            {
                Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
                if (horizontalVel.magnitude > wallBounceThreshold)
                {
                    // Bounce off wall (simple reflection)
                    velocity.x *= -wallBounceFactor;
                    velocity.z *= -wallBounceFactor;
                }
            }
        }
    }
}
