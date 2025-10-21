using UnityEngine;
using Warspite.Systems;

namespace Warspite.Player
{
    /// <summary>
    /// Allows player to walk on walls and ceilings when in deepest time dilation (L3).
    /// Rotates player to match surface orientation and adjusts gravity direction.
    /// </summary>
    public class WallWalking : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private TimeDilationController timeController;
        [SerializeField] private Transform cameraTransform;

        [Header("Wall Detection")]
        [SerializeField] private float wallDetectionDistance = 1.5f;
        [SerializeField] private float groundCheckDistance = 0.3f;
        [SerializeField] private LayerMask walkableLayers = -1;
        [SerializeField] private float minWallAngle = 45f; // Minimum angle to be considered a wall

        [Header("Activation")]
        [SerializeField] private KeyCode wallWalkKey = KeyCode.E;
        [SerializeField] private bool requireDeepestSlow = false; // Disabled for debugging
        [SerializeField] private float promptDistance = 2f; // Distance to show prompt

        [Header("Transition Settings")]
        [SerializeField] private float rotationSpeed = 10f; // Increased for more responsive transitions
        [SerializeField] private float gravityStrength = 20f;
        [SerializeField] private float exitAngleThreshold = 30f; // Angle to auto-exit wall walking

        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color wallWalkColor = Color.cyan;

        private Vector3 currentGravityDirection = Vector3.down;
        private Vector3 targetGravityDirection = Vector3.down;
        private Quaternion targetRotation = Quaternion.identity;
        private bool isWallWalking = false;
        private Vector3 lastSurfaceNormal = Vector3.up;
        private bool hasAvailableSurface = false;
        private Vector3 availableSurfaceNormal = Vector3.up;
        private Vector3 availableSurfacePosition = Vector3.zero;

        // Public properties
        public bool IsWallWalking => isWallWalking;
        public Vector3 GravityDirection => currentGravityDirection;
        public bool CanWallWalk => hasAvailableSurface && !isWallWalking;
        public Vector3 PromptPosition => availableSurfacePosition;

        void Start()
        {
            // Auto-find references
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (timeController == null)
                timeController = FindFirstObjectByType<TimeDilationController>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            if (characterController == null) return;

            bool meetsConditions = CheckWallWalkConditions();

            if (isWallWalking)
            {
                // Already wall walking - maintain alignment or check for exit
                if (meetsConditions)
                {
                    MaintainWallWalking();
                    
                    // Check for manual exit or conditions no longer met
                    if (Input.GetKeyDown(wallWalkKey))
                    {
                        ExitWallWalking();
                    }
                }
                else
                {
                    // Conditions no longer met (e.g., exited L3)
                    ExitWallWalking();
                }
            }
            else
            {
                // Not wall walking - detect available surfaces and check for activation
                if (meetsConditions)
                {
                    DetectAvailableSurface();
                    
                    // Check for button press to activate
                    if (hasAvailableSurface && Input.GetKeyDown(wallWalkKey))
                    {
                        ActivateWallWalking();
                    }
                }
                else
                {
                    hasAvailableSurface = false;
                }
            }

            // Smoothly interpolate gravity and rotation
            UpdateGravityAndRotation();
        }

        private bool CheckWallWalkConditions()
        {
            // Check if in deepest time dilation
            if (requireDeepestSlow && timeController != null)
            {
                return timeController.IsDeepestSlow();
            }

            return true;
        }

        private void DetectAvailableSurface()
        {
            // Reset available surface
            hasAvailableSurface = false;

            // Check for nearby walls
            Vector3[] directions = new Vector3[]
            {
                transform.forward,
                -transform.forward,
                transform.right,
                -transform.right,
                transform.up,
                -transform.up
            };

            float closestDistance = float.MaxValue;
            Vector3 closestNormal = Vector3.up;
            Vector3 closestPosition = Vector3.zero;
            bool foundWall = false;

            foreach (Vector3 direction in directions)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, direction, out hit, promptDistance, walkableLayers))
                {
                    // Check if this is a wall (not floor in current orientation)
                    float angle = Vector3.Angle(hit.normal, -currentGravityDirection);
                    
                    if (angle >= minWallAngle)
                    {
                        float distance = hit.distance;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestNormal = hit.normal;
                            closestPosition = hit.point;
                            foundWall = true;
                        }
                    }
                }
            }

            if (foundWall)
            {
                hasAvailableSurface = true;
                availableSurfaceNormal = closestNormal;
                availableSurfacePosition = closestPosition;
            }
        }

        private void ActivateWallWalking()
        {
            if (!hasAvailableSurface) return;

            isWallWalking = true;
            AlignToSurface(availableSurfaceNormal);
            
            // Instantly snap to wall orientation
            currentGravityDirection = targetGravityDirection;
            transform.rotation = targetRotation;
            
            Debug.Log($"Wall walking activated! Surface normal: {availableSurfaceNormal}, Gravity direction: {targetGravityDirection}");
        }

        private void MaintainWallWalking()
        {
            // Continuously detect surface under feet
            Vector3 rayOrigin = transform.position;
            Vector3 rayDirection = currentGravityDirection;
            
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, groundCheckDistance * 3f, walkableLayers))
            {
                // Update alignment to current surface
                AlignToSurface(hit.normal);
            }
            else
            {
                // Lost contact with surface - check nearby walls
                bool foundNearby = false;
                Vector3[] directions = new Vector3[]
                {
                    transform.forward,
                    -transform.forward,
                    transform.right,
                    -transform.right
                };

                foreach (Vector3 direction in directions)
                {
                    if (Physics.Raycast(transform.position, direction, out hit, wallDetectionDistance, walkableLayers))
                    {
                        float angle = Vector3.Angle(hit.normal, -currentGravityDirection);
                        if (angle >= minWallAngle)
                        {
                            AlignToSurface(hit.normal);
                            foundNearby = true;
                            break;
                        }
                    }
                }

                // If no surface found, exit wall walking
                if (!foundNearby)
                {
                    Debug.Log("Lost surface contact - exiting wall walking");
                    ExitWallWalking();
                }
            }
        }

        private void ExitWallWalking()
        {
            isWallWalking = false;
            targetGravityDirection = Vector3.down;
            targetRotation = Quaternion.identity;
            hasAvailableSurface = false;
            Debug.Log("Wall walking deactivated!");
        }

        private void CheckForNearbyWalls()
        {
            // Cast rays in multiple directions to find nearby walls
            Vector3[] directions = new Vector3[]
            {
                transform.forward,
                -transform.forward,
                transform.right,
                -transform.right,
                transform.up,
                -transform.up
            };

            float closestDistance = float.MaxValue;
            Vector3 closestNormal = Vector3.up;
            bool foundWall = false;

            foreach (Vector3 direction in directions)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, direction, out hit, wallDetectionDistance, walkableLayers))
                {
                    // Check if this is a wall (not floor/ceiling in current orientation)
                    float angle = Vector3.Angle(hit.normal, -currentGravityDirection);
                    
                    if (angle >= minWallAngle)
                    {
                        float distance = hit.distance;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestNormal = hit.normal;
                            foundWall = true;
                        }
                    }
                }
            }

            if (foundWall)
            {
                AlignToSurface(closestNormal);
            }
        }

        private void AlignToSurface(Vector3 surfaceNormal)
        {
            lastSurfaceNormal = surfaceNormal;

            // Set gravity to point away from surface
            targetGravityDirection = -surfaceNormal;

            // Calculate rotation to align "up" with surface normal
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform != null ? cameraTransform.forward : transform.forward, surfaceNormal);
            if (forward.magnitude < 0.1f)
            {
                forward = Vector3.ProjectOnPlane(transform.forward, surfaceNormal);
            }

            if (forward.magnitude > 0.1f)
            {
                targetRotation = Quaternion.LookRotation(forward, surfaceNormal);
            }
        }

        private void UpdateGravityAndRotation()
        {
            // When wall-walking, snap instantly. When exiting, smooth transition back to normal
            if (isWallWalking)
            {
                // Instant snap while wall-walking for responsive feel
                currentGravityDirection = targetGravityDirection;
                transform.rotation = targetRotation;
            }
            else
            {
                // Smooth transition when exiting wall-walking
                currentGravityDirection = Vector3.Lerp(
                    currentGravityDirection,
                    targetGravityDirection,
                    rotationSpeed * Time.deltaTime
                ).normalized;

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// Apply gravity in the current gravity direction.
        /// Call this from your movement controller instead of using default gravity.
        /// </summary>
        public Vector3 GetGravityVector()
        {
            return currentGravityDirection * gravityStrength;
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            // Draw gravity direction
            Gizmos.color = isWallWalking ? wallWalkColor : normalColor;
            Gizmos.DrawRay(transform.position, currentGravityDirection * 2f);
            
            // Draw surface normal
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, lastSurfaceNormal * 2f);

            // Draw detection sphere for available surfaces
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, promptDistance);

            // Draw available surface prompt
            if (hasAvailableSurface && !isWallWalking)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(availableSurfacePosition, 0.2f);
                Gizmos.DrawRay(availableSurfacePosition, availableSurfaceNormal * 1f);
                
                // Draw line to player
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, availableSurfacePosition);
            }
        }
    }
}
