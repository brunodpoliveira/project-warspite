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
        private Vector3 velocity;
        private Vector3 lastMoveDirection;
        private bool isGrounded;

        public Vector3 Velocity => velocity;
        public bool IsGrounded => isGrounded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
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
            isGrounded = Physics.Raycast(
                transform.position,
                Vector3.down,
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
                Debug.LogError("MomentumLocomotion: No camera tagged as 'MainCamera' found!");
                return;
            }

            Transform camTransform = mainCam.transform;
            Vector3 forward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized;
            Vector3 desiredMoveDir = (forward * inputDir.z + right * inputDir.x).normalized;

            // Calculate horizontal velocity (preserve vertical for gravity)
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);

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

            // Apply back to velocity
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }

        private void ApplyGravity()
        {
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small downward force to keep grounded
            }
            else
            {
                velocity.y -= gravity * Time.deltaTime;
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
