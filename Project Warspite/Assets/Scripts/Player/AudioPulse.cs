using UnityEngine;
using Warspite.Core;

namespace Warspite.Player
{
    /// <summary>
    /// Rechargeable hyper strike that charges with movement.
    /// Faster movement = faster recharge.
    /// Enhances existing melee combat with a powerful special attack.
    /// </summary>
    public class AudioPulse : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MomentumLocomotion locomotion;

        [Header("Charge Settings")]
        [SerializeField] private float maxCharge = 100f;
        [SerializeField] private float chargePerMeterMoved = 2f; // Charge gained per meter traveled
        [SerializeField] private float minSpeedForCharge = 1f; // Minimum speed to start charging
        [SerializeField] private float passiveChargeRate = 5f; // Charge per second when stationary (optional)
        [SerializeField] private bool enablePassiveCharge = false;

        [Header("Attack Settings")]
        [SerializeField] private float pulseRange = 10f;
        [SerializeField] private float pulseDamage = 100f;
        [SerializeField] private float pulseKnockback = 50f;
        [SerializeField] private float pulseCooldown = 1f; // Cooldown after use

        [Header("Controls")]
        [SerializeField] private KeyCode pulseKey = KeyCode.Mouse2; // Middle mouse button

        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool showConeIndicator = true;
        [SerializeField] private Color chargedColor = Color.cyan;
        [SerializeField] private Color chargingColor = Color.yellow;
        [SerializeField] private int coneSegments = 16; // Number of lines to draw cone

        private float currentCharge = 0f;
        private float lastPulseTime = -999f;
        private float distanceTraveled = 0f;
        private Vector3 lastPosition;

        // Public properties for UI
        public float CurrentCharge => currentCharge;
        public float MaxCharge => maxCharge;
        public float ChargePercent => currentCharge / maxCharge;
        public bool IsFullyCharged => currentCharge >= maxCharge;
        public bool CanUsePulse => IsFullyCharged && Time.time >= lastPulseTime + pulseCooldown;

        void Start()
        {
            // Auto-find locomotion if not assigned
            if (locomotion == null)
                locomotion = GetComponent<MomentumLocomotion>();

            lastPosition = transform.position;
        }

        void Update()
        {
            UpdateCharge();
            HandleInput();
            
            if (showConeIndicator && IsFullyCharged)
            {
                DrawConeIndicator();
            }
        }

        private void UpdateCharge()
        {
            if (locomotion == null) return;

            // Don't charge if already full
            if (currentCharge >= maxCharge)
            {
                currentCharge = maxCharge;
                return;
            }

            // Calculate distance moved this frame
            Vector3 currentPosition = transform.position;
            float distanceThisFrame = Vector3.Distance(currentPosition, lastPosition);
            lastPosition = currentPosition;

            // Get horizontal speed
            Vector3 velocity = locomotion.Velocity;
            float horizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;

            // Charge based on movement
            if (horizontalSpeed >= minSpeedForCharge)
            {
                // Faster movement = more charge
                float chargeGain = distanceThisFrame * chargePerMeterMoved;
                currentCharge += chargeGain;
                distanceTraveled += distanceThisFrame;
            }
            else if (enablePassiveCharge)
            {
                // Passive charge when stationary (use unscaled time for consistent player ability)
                currentCharge += passiveChargeRate * Time.unscaledDeltaTime;
            }

            // Clamp charge
            currentCharge = Mathf.Clamp(currentCharge, 0f, maxCharge);
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(pulseKey) && CanUsePulse)
            {
                FireAudioPulse();
            }
        }

        private void FireAudioPulse()
        {
            lastPulseTime = Time.time;

            Vector3 pulseOrigin = transform.position + Vector3.up * 1f;
            Camera playerCamera = Camera.main;
            Vector3 pulseDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;

            // Find all targets in cone
            Collider[] nearbyColliders = Physics.OverlapSphere(pulseOrigin, pulseRange);

            int hitCount = 0;
            foreach (Collider col in nearbyColliders)
            {
                if (col.gameObject == gameObject) continue;

                // Check if target is in front of player (cone check)
                Vector3 toTarget = col.transform.position - pulseOrigin;
                float angle = Vector3.Angle(pulseDirection, toTarget);
                float distance = toTarget.magnitude;

                // Cone angle based on distance (wider at close range)
                float maxAngle = Mathf.Lerp(90f, 45f, distance / pulseRange);

                if (angle <= maxAngle && distance <= pulseRange)
                {
                    // Deal damage
                    Health targetHealth = col.GetComponent<Health>();
                    if (targetHealth != null && !targetHealth.IsDead)
                    {
                        targetHealth.TakeDamage(pulseDamage);
                        hitCount++;
                    }

                    // Apply knockback
                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 knockbackDirection = toTarget.normalized;
                        rb.AddForce(knockbackDirection * pulseKnockback, ForceMode.Impulse);
                    }
                }
            }

            // Consume charge
            currentCharge = 0f;
            distanceTraveled = 0f;

            // TODO: Add visual/audio effects here
            // - Shockwave particle effect
            // - Screen shake
            // - Sound effect
        }

        // Public method to manually add charge (for testing or special events)
        public void AddCharge(float amount)
        {
            currentCharge = Mathf.Clamp(currentCharge + amount, 0f, maxCharge);
        }

        // Public method to check if pulse is ready
        public bool IsPulseReady()
        {
            return CanUsePulse;
        }

        private void DrawConeIndicator()
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            Camera playerCamera = Camera.main;
            Vector3 direction = playerCamera != null ? playerCamera.transform.forward : transform.forward;

            Color indicatorColor = IsFullyCharged ? chargedColor : chargingColor;
            indicatorColor.a = 0.5f; // Semi-transparent

            // Draw cone outline using Debug lines
            for (int i = 0; i <= coneSegments; i++)
            {
                float angle = (i / (float)coneSegments) * 360f;
                
                // Calculate cone points at near and far distances
                Vector3 nearPoint = CalculateConePoint(origin, direction, 0.5f, 90f, angle);
                Vector3 farPoint = CalculateConePoint(origin, direction, pulseRange, 45f, angle);
                
                // Draw line from near to far
                Debug.DrawLine(nearPoint, farPoint, indicatorColor);
                
                // Draw radial lines from origin
                if (i % 4 == 0) // Only draw every 4th radial line to reduce clutter
                {
                    Debug.DrawLine(origin, farPoint, indicatorColor);
                }
                
                // Draw circle at far distance
                if (i < coneSegments)
                {
                    float nextAngle = ((i + 1) / (float)coneSegments) * 360f;
                    Vector3 nextFarPoint = CalculateConePoint(origin, direction, pulseRange, 45f, nextAngle);
                    Debug.DrawLine(farPoint, nextFarPoint, indicatorColor);
                }
            }
        }

        private Vector3 CalculateConePoint(Vector3 origin, Vector3 direction, float distance, float coneAngle, float rotationAngle)
        {
            // Create a point on the cone surface
            Quaternion rotation = Quaternion.LookRotation(direction);
            Quaternion angleRotation = Quaternion.AngleAxis(rotationAngle, direction);
            Quaternion coneRotation = Quaternion.AngleAxis(coneAngle, Vector3.right);
            
            Vector3 localPoint = coneRotation * Vector3.forward * distance;
            Vector3 rotatedPoint = angleRotation * localPoint;
            Vector3 worldPoint = rotation * rotatedPoint;
            
            return origin + worldPoint;
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            Vector3 origin = transform.position + Vector3.up * 1f;
            Camera playerCamera = Camera.main;
            Vector3 direction = playerCamera != null ? playerCamera.transform.forward : transform.forward;

            // Draw pulse range sphere
            Gizmos.color = IsFullyCharged ? chargedColor : chargingColor;
            Gizmos.DrawWireSphere(origin, pulseRange);

            // Draw cone visualization
            Gizmos.color = IsFullyCharged ? chargedColor : chargingColor;
            Vector3 forward = direction * pulseRange;
            
            // Draw cone at close range (90 degrees)
            Quaternion leftRotClose = Quaternion.Euler(0, -45f, 0);
            Quaternion rightRotClose = Quaternion.Euler(0, 45f, 0);
            Gizmos.DrawLine(origin, origin + leftRotClose * forward);
            Gizmos.DrawLine(origin, origin + rightRotClose * forward);

            // Draw center line
            Gizmos.DrawLine(origin, origin + forward);
        }
    }
}
