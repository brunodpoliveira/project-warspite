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

        [Header("Shockwave Settings")]
        [SerializeField] private float shockwaveSpawnDelay = 0.5f; // Time at high speed before shockwave spawns
        [SerializeField] private float shockwaveDamage = 75f; // Damage dealt by shockwave on contact
        [SerializeField] private float shockwaveSpeed = 15f; // Speed shockwave travels through tunnel (m/s)
        [SerializeField] private float shockwaveRadius = 3f; // Radius of shockwave damage sphere
        [SerializeField] private float shockwaveExtension = 5f; // Distance shockwave travels beyond tunnel end

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
            public float extensionDistance; // Distance traveled beyond tunnel end
            public GameObject visualObject;
            public System.Collections.Generic.HashSet<GameObject> damagedTargets; // Track what's been hit
        }

        private bool hasBoomActive = false;
        private System.Collections.Generic.List<WakeSegment> wakeSegments = new System.Collections.Generic.List<WakeSegment>();
        private Shockwave activeShockwave = null;
        private Vector3 lastSegmentPosition;
        private float distanceSinceLastSegment;
        private float boomActivationTime;
        private float timeAtHighSpeed = 0f; // Tracks how long player has been at high speed
        private bool shockwaveSpawned = false; // Tracks if shockwave has been spawned for current boom
        private float lastShockwaveStopTime = -999f; // Time when last shockwave stopped (for chaining cooldown)
        private bool isInChainCooldown = false; // Whether we're in cooldown after shockwave stopped
        
        private const float PLAYER_GRACE_PERIOD = 0.1f; // Player immune to own wake for this duration after activation
        private const float CHAIN_COOLDOWN = 0.75f; // Cooldown after shockwave stops before new tunnel can start

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

            // Check if cooldown has expired (use unscaled time so it works during time dilation)
            if (isInChainCooldown)
            {
                float timeRemaining = (lastShockwaveStopTime + CHAIN_COOLDOWN) - Time.unscaledTime;
                if (timeRemaining <= 0)
                {
                    isInChainCooldown = false;
                    Debug.Log($"[SonicBoom] Chain cooldown expired! Ready for new tunnel. Speed: {currentSpeed:F1}");
                }
            }

            if (meetsConditions && meetsSpeedThreshold)
            {
                // Activate boom if not active (and not in cooldown)
                if (!hasBoomActive && !isInChainCooldown)
                {
                    hasBoomActive = true;
                    boomActivationTime = Time.unscaledTime;
                    timeAtHighSpeed = 0f;
                    shockwaveSpawned = false;
                    Debug.Log($"[SonicBoom] NEW TUNNEL STARTED! Speed: {currentSpeed:F1}");
                    
                    // Start segment position behind player to avoid immediate self-damage
                    Vector3 velocity = locomotion.Velocity;
                    Vector3 direction = velocity.magnitude > 0.1f ? velocity.normalized : transform.forward;
                    lastSegmentPosition = transform.position - direction * (segmentSpacing * 2f);
                    distanceSinceLastSegment = 0f;
                }
                
                // Create trail segments as player moves (only if boom is active)
                if (hasBoomActive)
                {
                    CreateTrailSegments(false);
                    
                    // Track time at high speed
                    timeAtHighSpeed += Time.unscaledDeltaTime;
                    
                    // Spawn shockwave after delay (only once per boom cycle)
                    if (!shockwaveSpawned && timeAtHighSpeed >= shockwaveSpawnDelay)
                    {
                        SpawnShockwave();
                        shockwaveSpawned = true;
                    }
                }
            }
            else
            {
                // Below speed threshold - deactivate boom
                if (hasBoomActive)
                {
                    hasBoomActive = false;
                    timeAtHighSpeed = 0f;
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

        private Material CreateURPTransparentMaterial(Color color)
        {
            // Try to find URP Lit shader, fall back to Standard
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);
            mat.color = color;

            // URP Lit shader setup
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0); // Alpha blend
            }
            // Standard shader setup
            else if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3); // Transparent mode
            }

            // Set blend mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;

            // Add emission
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.5f);
            }

            return mat;
        }

        private GameObject CreateSegmentVisual(Vector3 position)
        {
            GameObject segmentObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            segmentObj.name = "WakeSegment";
            segmentObj.transform.position = position;
            segmentObj.transform.localScale = Vector3.one * wakeRadius * 2f;

            // Apply URP-compatible material with wake color (orange)
            Renderer renderer = segmentObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateURPTransparentMaterial(wakeColor);
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

            // Determine color: red if shockwave spawned, orange otherwise
            Color color = shockwaveSpawned ? dangerColor : wakeColor;
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
                return;
            }

            // Clean up existing shockwave if one is still active (prevents orphaned visuals)
            if (activeShockwave != null && activeShockwave.visualObject != null)
            {
                Destroy(activeShockwave.visualObject);
            }

            GameObject shockwaveVisual = CreateShockwaveVisual();

            activeShockwave = new Shockwave
            {
                currentSegmentIndex = 0,
                progress = 0f,
                extensionDistance = 0f,
                damagedTargets = new System.Collections.Generic.HashSet<GameObject>(),
                visualObject = shockwaveVisual
            };
            
            // Change wake segment colors to danger (red)
            foreach (WakeSegment segment in wakeSegments)
            {
                if (segment.visualObject != null)
                {
                    Renderer renderer = segment.visualObject.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.color = dangerColor;
                    }
                }
            }
        }

        private void UpdateShockwave()
        {
            if (activeShockwave == null || wakeSegments.Count == 0) return;

            // Calculate how far to move this frame (use unscaled time for consistent player ability)
            float distanceToMove = shockwaveSpeed * Time.unscaledDeltaTime;
            
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

            // Check if shockwave reached the end of tunnel
            if (activeShockwave.currentSegmentIndex >= wakeSegments.Count - 1)
            {
                activeShockwave.progress = 1f;
                
                // Continue traveling beyond tunnel for extension distance
                activeShockwave.extensionDistance += distanceToMove;
                
                // Check if extension is complete
                if (activeShockwave.extensionDistance >= shockwaveExtension)
                {
                    DestroyShockwave();
                    return;
                }
            }

            // Update shockwave position and check for damage
            Vector3 shockwavePosition = GetShockwavePosition();
            if (activeShockwave.visualObject != null)
            {
                activeShockwave.visualObject.transform.position = shockwavePosition;
            }

            // Check for targets to damage and obstacles
            bool hitEnemy;
            bool hitObstacle = CheckShockwaveDamage(shockwavePosition, out hitEnemy);
            
            // Stop shockwave if it hit a wall or enemy
            if (hitObstacle)
            {
                DestroyShockwave(hitEnemy);
            }
        }

        private void DestroyShockwave(bool enemyHit = false)
        {
            if (activeShockwave == null) return;

            // If enemy was hit, trigger cooldown FIRST (before destroying anything)
            // This immediately stops new segment creation
            if (enemyHit)
            {
                Debug.Log($"[SonicBoom] Enemy hit! Tunnel destroyed, starting {CHAIN_COOLDOWN}s cooldown");
                hasBoomActive = false;
                timeAtHighSpeed = 0f;
                isInChainCooldown = true;
                lastShockwaveStopTime = Time.unscaledTime;
            }

            // Destroy shockwave visual
            if (activeShockwave.visualObject != null)
            {
                Destroy(activeShockwave.visualObject);
            }
            
            // Clean up all wake segments from this tunnel
            // (Since we set hasBoomActive=false above, no new segments are being created)
            for (int i = wakeSegments.Count - 1; i >= 0; i--)
            {
                if (wakeSegments[i].visualObject != null)
                {
                    Destroy(wakeSegments[i].visualObject);
                }
            }
            wakeSegments.Clear();
            
            activeShockwave = null;
        }

        private Vector3 GetShockwavePosition()
        {
            if (activeShockwave == null || wakeSegments.Count == 0) return Vector3.zero;

            int index = activeShockwave.currentSegmentIndex;
            
            // If at end of tunnel, extend beyond
            if (index >= wakeSegments.Count - 1)
            {
                WakeSegment lastSeg = wakeSegments[wakeSegments.Count - 1];
                
                // Calculate direction from second-to-last to last segment
                Vector3 direction = Vector3.forward; // Default
                if (wakeSegments.Count >= 2)
                {
                    WakeSegment secondLast = wakeSegments[wakeSegments.Count - 2];
                    direction = (lastSeg.position - secondLast.position).normalized;
                }
                
                // Extend beyond tunnel end
                return lastSeg.position + direction * activeShockwave.extensionDistance;
            }

            Vector3 currentPos = wakeSegments[index].position;
            Vector3 nextPos = wakeSegments[index + 1].position;
            
            return Vector3.Lerp(currentPos, nextPos, activeShockwave.progress);
        }

        private bool CheckShockwaveDamage(Vector3 position, out bool hitEnemy)
        {
            // Use 75% of visual radius for detection (balance between precision and reliability)
            float collisionRadius = shockwaveRadius * 0.75f;
            
            // Find all objects within shockwave radius (ignore triggers)
            Collider[] nearbyColliders = Physics.OverlapSphere(position, collisionRadius, ~0, QueryTriggerInteraction.Ignore);
            bool shouldStop = false;
            hitEnemy = false;

            foreach (Collider col in nearbyColliders)
            {
                GameObject target = col.gameObject;
                
                // Check if this is player-related for logging
                bool isPlayerRelated = target == gameObject || target.transform.IsChildOf(transform);
                
                // Skip if already damaged by this shockwave
                if (activeShockwave.damagedTargets.Contains(target))
                {
                    continue;
                }
                
                // Skip if this is a wake segment visual
                if (target.name.Contains("WakeSegment") || target.name.Contains("Shockwave"))
                {
                    continue;
                }
                
                // DON'T skip player's own collider anymore - we want to damage the player!
                // if (target == gameObject) continue;
                
                // Skip projectiles
                if (target.GetComponent<Warspite.World.Projectile>() != null)
                {
                    continue;
                }

                // Check for walls/obstacles (anything with collider but no Health)
                // Try to get Health from the collider, or from parent if it's a child object
                Health targetHealth = col.GetComponent<Health>();
                if (targetHealth == null && col.transform.parent != null)
                {
                    targetHealth = col.GetComponentInParent<Health>();
                }
                
                if (targetHealth == null)
                {
                    // Ignore floor (anything below the shockwave)
                    if (target.name.ToLower().Contains("floor") || target.name.ToLower().Contains("ground"))
                    {
                        continue;
                    }
                    
                    // Only stop for actual walls (static colliders) that are vertical obstacles
                    if (col.gameObject.isStatic)
                    {
                        // Check if this is a vertical wall (not floor/ceiling)
                        Vector3 toWall = target.transform.position - position;
                        float verticalComponent = Mathf.Abs(toWall.y);
                        float horizontalComponent = new Vector2(toWall.x, toWall.z).magnitude;
                        
                        // Only stop if it's more horizontal than vertical (i.e., a wall, not floor)
                        if (horizontalComponent > verticalComponent)
                        {
                            // Hit a wall or obstacle - stop shockwave
                            if (spawnShockwaveEffect)
                            {
                                ShockwaveEffect.SpawnShockwave(position, Color.white, shockwaveRadius);
                            }
                            shouldStop = true;
                        }
                    }
                    continue;
                }

                // Check for damageable targets
                if (!targetHealth.IsDead)
                {
                    // Check if this is the player (check root or if it's a child of player)
                    bool isPlayer = target == gameObject || target.transform.IsChildOf(transform);
                    
                    // Skip player damage during grace period (only at boom start)
                    if (isPlayer && Time.unscaledTime - boomActivationTime < PLAYER_GRACE_PERIOD)
                    {
                        continue;
                    }

                    // Deal damage once
                    targetHealth.TakeDamage(shockwaveDamage);
                    activeShockwave.damagedTargets.Add(target);

                    // Spawn visual effect
                    if (spawnShockwaveEffect)
                    {
                        Color effectColor = isPlayer ? dangerColor : shockwaveColor;
                        ShockwaveEffect.SpawnShockwave(position, effectColor, shockwaveRadius);
                    }

                    // Enemy absorbed the shockwave - stop it
                    if (!isPlayer)
                    {
                        shouldStop = true;
                        hitEnemy = true;
                    }
                }
            }

            return shouldStop;
        }

        private GameObject CreateShockwaveVisual()
        {
            GameObject shockwaveObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shockwaveObj.name = "ShockwaveVisual";
            shockwaveObj.transform.localScale = Vector3.one * shockwaveRadius * 2f;

            // Apply URP-compatible material with shockwave color (cyan/light blue)
            Renderer renderer = shockwaveObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = CreateURPTransparentMaterial(shockwaveColor);
                // Boost emission for shockwave
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", shockwaveColor * 2f);
                }
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

            // Determine color based on whether shockwave has spawned
            Color lineColor = shockwaveSpawned ? dangerColor : wakeColor;

            // Draw lines connecting segments to show the trail
            for (int i = 0; i < wakeSegments.Count - 1; i++)
            {
                WakeSegment current = wakeSegments[i];
                WakeSegment next = wakeSegments[i + 1];
                
                Debug.DrawLine(current.position, next.position, lineColor);
            }

            // Draw connection from last segment to player if active
            if (hasBoomActive && wakeSegments.Count > 0)
            {
                WakeSegment lastSegment = wakeSegments[wakeSegments.Count - 1];
                Debug.DrawLine(lastSegment.position, transform.position, lineColor);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || wakeSegments.Count == 0) return;

            // Determine color based on whether shockwave has spawned
            Gizmos.color = shockwaveSpawned ? dangerColor : wakeColor;

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

            // Draw line from last segment to player if active
            if (hasBoomActive && wakeSegments.Count > 0)
            {
                Gizmos.DrawLine(wakeSegments[wakeSegments.Count - 1].position, transform.position);
            }
        }
    }
}
