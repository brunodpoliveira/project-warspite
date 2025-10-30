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
        
        [Header("Time Dilation Speed Boost")]
        [SerializeField] private bool enableTimeDilationBoost = true;
        [SerializeField] private float speedMultiplierAtMaxDilation = 3f; // 3x speed at 0.05x time
        [Tooltip("Speed formula: baseSpeed * (1 + (1 - timeScale) * multiplier)")]
        [SerializeField] private AnimationCurve speedBoostCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

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

        /// <summary>
        /// Rotates the velocity vector to maintain momentum when changing orientation (e.g., wall-walking).
        /// </summary>
        public void RotateVelocity(Quaternion rotation)
        {
            velocity = rotation * velocity;
        }

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
            CollisionFlags collisionFlags = controller.Move(velocity * Time.unscaledDeltaTime);
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

            // Calculate velocity in the plane perpendicular to gravity
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, upDirection);

            if (desiredMoveDir.sqrMagnitude > 0.01f)
            {
                // Calculate speed boost from time dilation
                float currentMaxSpeed = maxSpeed;
                if (enableTimeDilationBoost)
                {
                    // As time slows down (timeScale → 0), player gets faster
                    // timeScale 1.0 (normal) → no boost
                    // timeScale 0.05 (max dilation) → max boost
                    float dilationAmount = 1f - Time.timeScale; // 0 at normal time, 0.95 at max dilation
                    float boostMultiplier = speedBoostCurve.Evaluate(Time.timeScale);
                    currentMaxSpeed = maxSpeed * (1f + dilationAmount * speedMultiplierAtMaxDilation * boostMultiplier);
                }
                
                // Accelerate towards desired direction
                Vector3 targetVelocity = desiredMoveDir * currentMaxSpeed;
                float accelRate = isGrounded ? acceleration : acceleration * airControl;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetVelocity,
                    accelRate * Time.unscaledDeltaTime
                );
            }
            else
            {
                // Decelerate when no input
                float decelRate = isGrounded ? deceleration : deceleration * airControl;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    decelRate * Time.unscaledDeltaTime
                );
            }

            // Combine horizontal movement with gravity component
            Vector3 gravityComponent = Vector3.Project(velocity, upDirection);
            velocity = horizontalVelocity + gravityComponent;
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
                    velocity += wallWalking.GravityDirection * 2f * Time.unscaledDeltaTime;
                }
                else
                {
                    // In air - apply full gravity
                    velocity += gravityVector * Time.unscaledDeltaTime;
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
                    velocity.y -= gravity * Time.unscaledDeltaTime;
                }
            }
        }

        private void HandleCollisions(CollisionFlags flags)
        {
            // Skip wall collision handling when wall-walking (walls are walkable surfaces)
            if (wallWalking != null && wallWalking.IsWallWalking)
            {
                return;
            }

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
