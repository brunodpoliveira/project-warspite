using UnityEngine;
using Warspite.Core;
using Warspite.Systems;

namespace Warspite.Player
{
    /// <summary>
    /// Creates a dangerous sonic boom "wake" when moving at high speed in deepest time dilation.
    /// The wake damages the player on sudden stops unless:
    /// - An enemy absorbs the boom
    /// - The player decelerates gently
    /// Strategic mechanic: speed = power but also risk
    /// </summary>
    public class SonicBoom : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MomentumLocomotion locomotion;
        [SerializeField] private TimeDilationController timeController;
        [SerializeField] private Health playerHealth;

        [Header("Activation Conditions")]
        [SerializeField] private float minSpeedForBoom = 8f; // Speed needed to create boom
        [SerializeField] private bool requireDeepestSlow = true; // Only in L3 time dilation

        [Header("Wake Settings")]
        [SerializeField] private float wakeRadius = 2f; // Radius of wake damage sphere
        [SerializeField] private float segmentSpacing = 0.5f; // Distance between wake segments
        [SerializeField] private float segmentLifetime = 2f; // How long each segment persists
        [SerializeField] private float bleedoverDuration = 1.5f; // Time wake persists after dropping below speed threshold

        [Header("Damage Settings")]
        [SerializeField] private float damagePerSecond = 50f; // Damage dealt by wake trail per second
        [SerializeField] private float damageInterval = 0.5f; // Time between damage ticks

        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool spawnShockwaveEffect = true;
        [SerializeField] private Color wakeColor = new Color(1f, 0.5f, 0f, 0.5f); // Orange
        [SerializeField] private Color dangerColor = new Color(1f, 0f, 0f, 0.7f); // Red
        [SerializeField] private Color shockwaveColor = new Color(0.5f, 0.8f, 1f, 0.8f); // Cyan

        // Wake segment class to track individual trail pieces
        private class WakeSegment
        {
            public Vector3 position;
            public float creationTime;
            public GameObject visualObject;
            public bool isBleedover; // Created during bleedover period
        }

        private bool hasBoomActive = false;
        private System.Collections.Generic.List<WakeSegment> wakeSegments = new System.Collections.Generic.List<WakeSegment>();
        private Vector3 lastSegmentPosition;
        private float distanceSinceLastSegment;
        private float speedDroppedBelowThresholdTime;
        private bool isInBleedover = false;
        private System.Collections.Generic.Dictionary<GameObject, float> lastDamageTimes = new System.Collections.Generic.Dictionary<GameObject, float>();

        // Public properties
        public bool HasActiveBoom => hasBoomActive;
        public int WakeSegmentCount => wakeSegments.Count;

        void Start()
        {
            // Auto-find references
            if (locomotion == null)
                locomotion = GetComponent<MomentumLocomotion>();
            
            if (timeController == null)
                timeController = FindFirstObjectByType<TimeDilationController>();
            
            if (playerHealth == null)
                playerHealth = GetComponent<Health>();
        }

        void Update()
        {
            if (locomotion == null) return;

            float currentSpeed = GetHorizontalSpeed();
            bool meetsConditions = CheckBoomConditions(currentSpeed);
            bool meetsSpeedThreshold = currentSpeed >= minSpeedForBoom;

            if (meetsConditions && meetsSpeedThreshold)
            {
                // Activate boom if not active
                if (!hasBoomActive)
                {
                    hasBoomActive = true;
                    lastSegmentPosition = transform.position;
                    distanceSinceLastSegment = 0f;
                    Debug.Log("Sonic boom activated!");
                }
                
                // Create trail segments as player moves
                CreateTrailSegments(false);
                
                // Reset bleedover tracking
                isInBleedover = false;
            }
            else if (hasBoomActive)
            {
                // Check if we should enter bleedover period
                if (!isInBleedover && !meetsSpeedThreshold)
                {
                    // Just dropped below speed threshold
                    isInBleedover = true;
                    speedDroppedBelowThresholdTime = Time.time;
                    Debug.Log("Sonic boom entering bleedover period");
                }
                
                // Check if bleedover period has expired
                if (isInBleedover)
                {
                    float bleedoverElapsed = Time.time - speedDroppedBelowThresholdTime;
                    if (bleedoverElapsed >= bleedoverDuration)
                    {
                        // Stop creating new segments, but keep existing ones
                        hasBoomActive = false;
                        isInBleedover = false;
                        Debug.Log("Sonic boom bleedover ended");
                    }
                    else
                    {
                        // Continue creating segments during bleedover (player's last position)
                        CreateTrailSegments(true);
                    }
                }
            }

            // Update and clean up segments
            UpdateWakeSegments();
            
            // Check for damage from all wake segments
            CheckWakeDamage();
            
            // Draw visual indicators
            if (showDebugGizmos)
            {
                DrawWakeIndicator();
            }
        }

        private float GetHorizontalSpeed()
        {
            Vector3 velocity = locomotion.Velocity;
            return new Vector3(velocity.x, 0, velocity.z).magnitude;
        }

        private bool CheckBoomConditions(float speed)
        {
            // Must be moving fast enough
            if (speed < minSpeedForBoom) return false;

            // Check time dilation requirement
            if (requireDeepestSlow && timeController != null)
            {
                return timeController.IsDeepestSlow();
            }

            return true;
        }

        private void CreateTrailSegments(bool isBleedoverSegment)
        {
            // Track distance traveled since last segment
            Vector3 currentPosition = transform.position;
            float distanceMoved = Vector3.Distance(lastSegmentPosition, currentPosition);
            distanceSinceLastSegment += distanceMoved;

            // Create new segment if we've moved far enough
            if (distanceSinceLastSegment >= segmentSpacing)
            {
                WakeSegment segment = new WakeSegment
                {
                    position = lastSegmentPosition,
                    creationTime = Time.time,
                    isBleedover = isBleedoverSegment,
                    visualObject = CreateSegmentVisual(lastSegmentPosition, isBleedoverSegment)
                };

                wakeSegments.Add(segment);
                distanceSinceLastSegment = 0f;
            }

            lastSegmentPosition = currentPosition;
        }

        private GameObject CreateSegmentVisual(Vector3 position, bool isBleedover)
        {
            GameObject segmentObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            segmentObj.name = isBleedover ? "WakeSegment_Bleedover" : "WakeSegment";
            segmentObj.transform.position = position;
            segmentObj.transform.localScale = Vector3.one * wakeRadius * 2f;

            // Make it semi-transparent
            Renderer renderer = segmentObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = isBleedover ? dangerColor : wakeColor;
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                renderer.material = mat;
            }

            // Remove collider so it doesn't interfere with physics
            Collider col = segmentObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            return segmentObj;
        }

        private void UpdateWakeSegments()
        {
            // Update and remove expired segments
            for (int i = wakeSegments.Count - 1; i >= 0; i--)
            {
                WakeSegment segment = wakeSegments[i];
                float age = Time.time - segment.creationTime;

                if (age >= segmentLifetime)
                {
                    // Remove expired segment
                    if (segment.visualObject != null)
                    {
                        Destroy(segment.visualObject);
                    }
                    wakeSegments.RemoveAt(i);
                }
                else
                {
                    // Update visual fade
                    UpdateSegmentVisual(segment, age);
                }
            }
        }

        private void UpdateSegmentVisual(WakeSegment segment, float age)
        {
            if (segment.visualObject == null) return;

            Renderer renderer = segment.visualObject.GetComponent<Renderer>();
            if (renderer == null) return;

            // Fade out based on age
            float fadeProgress = age / segmentLifetime;
            float alpha = (1f - fadeProgress) * 0.5f; // Max 0.5 transparency

            Color color = segment.isBleedover ? dangerColor : wakeColor;
            color.a = alpha;
            renderer.material.color = color;

            // Shrink slightly as it fades
            float scale = Mathf.Lerp(wakeRadius * 2f, wakeRadius * 1.5f, fadeProgress);
            segment.visualObject.transform.localScale = Vector3.one * scale;
        }

        private void CheckWakeDamage()
        {
            // Check damage for each wake segment
            foreach (WakeSegment segment in wakeSegments)
            {
                // Find all objects within this segment's radius
                Collider[] nearbyColliders = Physics.OverlapSphere(segment.position, wakeRadius);

                foreach (Collider col in nearbyColliders)
                {
                    GameObject target = col.gameObject;
                    
                    // Skip if this is a wake segment visual
                    if (target.name.Contains("WakeSegment")) continue;

                    Health targetHealth = col.GetComponent<Health>();
                    if (targetHealth != null && !targetHealth.IsDead)
                    {
                        // Check if enough time has passed since last damage to this target
                        float lastDamage = 0f;
                        if (lastDamageTimes.ContainsKey(target))
                        {
                            lastDamage = lastDamageTimes[target];
                        }

                        if (Time.time - lastDamage >= damageInterval)
                        {
                            // Deal damage
                            float damage = damagePerSecond * damageInterval;
                            targetHealth.TakeDamage(damage);
                            lastDamageTimes[target] = Time.time;

                            // Check if this is the player
                            bool isPlayer = target == gameObject;
                            string targetName = isPlayer ? "Player" : target.name;
                            
                            Debug.Log($"Sonic boom wake hit {targetName} for {damage} damage!");

                            // Spawn visual effect on damage (throttled)
                            if (spawnShockwaveEffect)
                            {
                                Color effectColor = isPlayer ? dangerColor : shockwaveColor;
                                ShockwaveEffect.SpawnShockwave(segment.position, effectColor, wakeRadius * 1.5f);
                            }
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            // Clean up all wake segment visuals when component is destroyed
            foreach (WakeSegment segment in wakeSegments)
            {
                if (segment.visualObject != null)
                {
                    Destroy(segment.visualObject);
                }
            }
            wakeSegments.Clear();
        }

        private void DrawWakeIndicator()
        {
            if (wakeSegments.Count == 0) return;

            // Draw lines connecting segments to show the trail
            for (int i = 0; i < wakeSegments.Count - 1; i++)
            {
                WakeSegment current = wakeSegments[i];
                WakeSegment next = wakeSegments[i + 1];
                
                Color lineColor = current.isBleedover ? dangerColor : wakeColor;
                Debug.DrawLine(current.position, next.position, lineColor);
            }

            // Draw connection from last segment to player if active
            if (hasBoomActive && wakeSegments.Count > 0)
            {
                WakeSegment lastSegment = wakeSegments[wakeSegments.Count - 1];
                Color lineColor = isInBleedover ? dangerColor : wakeColor;
                Debug.DrawLine(lastSegment.position, transform.position, lineColor);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || wakeSegments.Count == 0) return;

            // Draw all wake segments in editor
            foreach (WakeSegment segment in wakeSegments)
            {
                Gizmos.color = segment.isBleedover ? dangerColor : wakeColor;
                Gizmos.DrawWireSphere(segment.position, wakeRadius);
            }

            // Draw trail path
            for (int i = 0; i < wakeSegments.Count - 1; i++)
            {
                Gizmos.color = wakeSegments[i].isBleedover ? dangerColor : wakeColor;
                Gizmos.DrawLine(wakeSegments[i].position, wakeSegments[i + 1].position);
            }

            // Draw line from last segment to player if active
            if (hasBoomActive && wakeSegments.Count > 0)
            {
                Gizmos.color = isInBleedover ? dangerColor : wakeColor;
                Gizmos.DrawLine(wakeSegments[wakeSegments.Count - 1].position, transform.position);
            }
        }
    }
}
