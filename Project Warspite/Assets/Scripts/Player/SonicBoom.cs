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

        [Header("Shockwave Settings")]
        [SerializeField] private float shockwaveDamage = 75f; // Damage dealt by shockwave on contact
        [SerializeField] private float shockwaveSpeed = 15f; // Speed shockwave travels through tunnel (m/s)
        [SerializeField] private float shockwaveRadius = 3f; // Radius of shockwave damage sphere

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

        // Shockwave class to track the traveling damage wave
        private class Shockwave
        {
            public int currentSegmentIndex; // Which segment the wave is at
            public float progress; // 0-1 progress between current and next segment
            public GameObject visualObject;
            public System.Collections.Generic.HashSet<GameObject> damagedTargets; // Track what's been hit
        }

        private bool hasBoomActive = false;
        private System.Collections.Generic.List<WakeSegment> wakeSegments = new System.Collections.Generic.List<WakeSegment>();
        private Shockwave activeShockwave = null;
        private Vector3 lastSegmentPosition;
        private float distanceSinceLastSegment;
        private float speedDroppedBelowThresholdTime;
        private float boomActivationTime;
        private bool isInBleedover = false;
        
        private const float PLAYER_GRACE_PERIOD = 0.5f; // Player immune to own wake for this duration after activation

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
                    boomActivationTime = Time.time;
                    // Start segment position behind player to avoid immediate self-damage
                    Vector3 velocity = locomotion.Velocity;
                    Vector3 direction = velocity.magnitude > 0.1f ? velocity.normalized : transform.forward;
                    lastSegmentPosition = transform.position - direction * (segmentSpacing * 2f);
                    distanceSinceLastSegment = 0f;
                    Debug.Log("Sonic boom activated - player has grace period!");
                }
                
                // Create trail segments as player moves
                CreateTrailSegments(false);
                
                // Reset bleedover tracking
                isInBleedover = false;
            }
            else if (hasBoomActive)
            {
                // Check if we should enter bleedover period
                if (!isInBleedover)
                {
                    // Just dropped below speed threshold - spawn shockwave!
                    isInBleedover = true;
                    speedDroppedBelowThresholdTime = Time.time;
                    SpawnShockwave();
                    Debug.Log("Sonic boom entering bleedover period - shockwave spawned!");
                }
                
                // Check if bleedover period has expired
                if (isInBleedover)
                {
                    float bleedoverElapsed = Time.time - speedDroppedBelowThresholdTime;
                    if (bleedoverElapsed >= bleedoverDuration)
                    {
                        // Bleedover ended, deactivate boom
                        hasBoomActive = false;
                        isInBleedover = false;
                        Debug.Log("Sonic boom bleedover ended");
                    }
                }
            }

            // Update and clean up segments
            UpdateWakeSegments();
            
            // Update shockwave if active
            if (activeShockwave != null)
            {
                UpdateShockwave();
            }
            
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

        private void CreateTrailSegments(bool unused)
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
                    isBleedover = false, // All segments created during active movement
                    visualObject = CreateSegmentVisual(lastSegmentPosition)
                };

                wakeSegments.Add(segment);
                distanceSinceLastSegment = 0f;
            }

            lastSegmentPosition = currentPosition;
        }

        private GameObject CreateSegmentVisual(Vector3 position)
        {
            GameObject segmentObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            segmentObj.name = "WakeSegment";
            segmentObj.transform.position = position;
            segmentObj.transform.localScale = Vector3.one * wakeRadius * 2f;

            // Make it semi-transparent
            Renderer renderer = segmentObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = wakeColor;
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

            // Determine color: red if in bleedover period, orange otherwise
            Color color = isInBleedover ? dangerColor : wakeColor;
            color.a = alpha;
            renderer.material.color = color;

            // Shrink slightly as it fades
            float scale = Mathf.Lerp(wakeRadius * 2f, wakeRadius * 1.5f, fadeProgress);
            segment.visualObject.transform.localScale = Vector3.one * scale;
        }

        private void SpawnShockwave()
        {
            // Don't spawn if no segments exist
            if (wakeSegments.Count == 0)
            {
                Debug.Log("No segments to spawn shockwave through");
                return;
            }

            activeShockwave = new Shockwave
            {
                currentSegmentIndex = 0,
                progress = 0f,
                damagedTargets = new System.Collections.Generic.HashSet<GameObject>(),
                visualObject = CreateShockwaveVisual()
            };

            Debug.Log($"Shockwave spawned at segment 0 of {wakeSegments.Count}");
        }

        private void UpdateShockwave()
        {
            if (activeShockwave == null || wakeSegments.Count == 0) return;

            // Calculate how far to move this frame
            float distanceToMove = shockwaveSpeed * Time.deltaTime;
            
            // Move through segments
            while (distanceToMove > 0 && activeShockwave.currentSegmentIndex < wakeSegments.Count - 1)
            {
                WakeSegment currentSeg = wakeSegments[activeShockwave.currentSegmentIndex];
                WakeSegment nextSeg = wakeSegments[activeShockwave.currentSegmentIndex + 1];
                
                float segmentDistance = Vector3.Distance(currentSeg.position, nextSeg.position);
                float remainingInSegment = segmentDistance * (1f - activeShockwave.progress);
                
                if (distanceToMove >= remainingInSegment)
                {
                    // Move to next segment
                    distanceToMove -= remainingInSegment;
                    activeShockwave.currentSegmentIndex++;
                    activeShockwave.progress = 0f;
                }
                else
                {
                    // Move within current segment
                    activeShockwave.progress += distanceToMove / segmentDistance;
                    distanceToMove = 0f;
                }
            }

            // Check if shockwave reached the end
            if (activeShockwave.currentSegmentIndex >= wakeSegments.Count - 1)
            {
                activeShockwave.progress = 1f;
                
                // Destroy shockwave after a brief moment at the end
                if (activeShockwave.visualObject != null)
                {
                    Destroy(activeShockwave.visualObject);
                }
                activeShockwave = null;
                Debug.Log("Shockwave reached end of tunnel");
                return;
            }

            // Update shockwave position and check for damage
            Vector3 shockwavePosition = GetShockwavePosition();
            if (activeShockwave.visualObject != null)
            {
                activeShockwave.visualObject.transform.position = shockwavePosition;
            }

            // Check for targets to damage
            CheckShockwaveDamage(shockwavePosition);
        }

        private Vector3 GetShockwavePosition()
        {
            if (activeShockwave == null || wakeSegments.Count == 0) return Vector3.zero;

            int index = activeShockwave.currentSegmentIndex;
            if (index >= wakeSegments.Count - 1) return wakeSegments[wakeSegments.Count - 1].position;

            Vector3 currentPos = wakeSegments[index].position;
            Vector3 nextPos = wakeSegments[index + 1].position;
            
            return Vector3.Lerp(currentPos, nextPos, activeShockwave.progress);
        }

        private void CheckShockwaveDamage(Vector3 position)
        {
            // Find all objects within shockwave radius
            Collider[] nearbyColliders = Physics.OverlapSphere(position, shockwaveRadius);

            foreach (Collider col in nearbyColliders)
            {
                GameObject target = col.gameObject;
                
                // Skip if already damaged by this shockwave
                if (activeShockwave.damagedTargets.Contains(target)) continue;
                
                // Skip if this is a wake segment visual
                if (target.name.Contains("WakeSegment") || target.name.Contains("Shockwave")) continue;

                Health targetHealth = col.GetComponent<Health>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    // Check if this is the player
                    bool isPlayer = target == gameObject;
                    
                    // Skip player damage during grace period
                    if (isPlayer && Time.time - boomActivationTime < PLAYER_GRACE_PERIOD)
                    {
                        continue;
                    }

                    // Deal damage once
                    targetHealth.TakeDamage(shockwaveDamage);
                    activeShockwave.damagedTargets.Add(target);

                    string targetName = isPlayer ? "Player" : target.name;
                    Debug.Log($"Shockwave hit {targetName} for {shockwaveDamage} damage!");

                    // Spawn visual effect
                    if (spawnShockwaveEffect)
                    {
                        Color effectColor = isPlayer ? dangerColor : shockwaveColor;
                        ShockwaveEffect.SpawnShockwave(position, effectColor, shockwaveRadius);
                    }
                }
            }
        }

        private GameObject CreateShockwaveVisual()
        {
            GameObject shockwaveObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shockwaveObj.name = "ShockwaveVisual";
            shockwaveObj.transform.localScale = Vector3.one * shockwaveRadius * 2f;

            // Make it semi-transparent and pulsing
            Renderer renderer = shockwaveObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = shockwaveColor;
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", shockwaveColor * 2f);
                renderer.material = mat;
            }

            // Remove collider
            Collider col = shockwaveObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            return shockwaveObj;
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
            
            // Clean up shockwave
            if (activeShockwave != null && activeShockwave.visualObject != null)
            {
                Destroy(activeShockwave.visualObject);
            }
        }

        private void DrawWakeIndicator()
        {
            if (wakeSegments.Count == 0) return;

            // Determine color based on current state
            Color lineColor = isInBleedover ? dangerColor : wakeColor;

            // Draw lines connecting segments to show the trail
            for (int i = 0; i < wakeSegments.Count - 1; i++)
            {
                WakeSegment current = wakeSegments[i];
                WakeSegment next = wakeSegments[i + 1];
                
                Debug.DrawLine(current.position, next.position, lineColor);
            }

            // Draw connection from last segment to player if active and not in bleedover
            if (hasBoomActive && !isInBleedover && wakeSegments.Count > 0)
            {
                WakeSegment lastSegment = wakeSegments[wakeSegments.Count - 1];
                Debug.DrawLine(lastSegment.position, transform.position, lineColor);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || wakeSegments.Count == 0) return;

            // Determine color based on current state
            Gizmos.color = isInBleedover ? dangerColor : wakeColor;

            // Draw all wake segments in editor
            foreach (WakeSegment segment in wakeSegments)
            {
                Gizmos.DrawWireSphere(segment.position, wakeRadius);
            }

            // Draw trail path
            for (int i = 0; i < wakeSegments.Count - 1; i++)
            {
                Gizmos.DrawLine(wakeSegments[i].position, wakeSegments[i + 1].position);
            }

            // Draw line from last segment to player if active and not in bleedover
            if (hasBoomActive && !isInBleedover && wakeSegments.Count > 0)
            {
                Gizmos.DrawLine(wakeSegments[wakeSegments.Count - 1].position, transform.position);
            }
        }
    }
}
