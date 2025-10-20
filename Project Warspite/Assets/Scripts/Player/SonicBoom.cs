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
        [SerializeField] private float wakeTrailDistance = 5f; // Distance behind player
        [SerializeField] private float wakeRadius = 2f; // Radius of wake damage sphere
        [SerializeField] private float wakeMoveSpeed = 8f; // How fast wake follows player
        [SerializeField] private float wakeUpdateInterval = 0.1f; // How often to check for damage
        [SerializeField] private float maxWakeLifetime = 3f; // Max time wake persists after creation
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

        private bool hasBoomActive = false;
        private GameObject wakeObject;
        private Vector3 wakePosition;
        private float lastDamageTime;
        private float wakeCreationTime;
        private float wakeDistanceTraveled;
        private float speedDroppedBelowThresholdTime;
        private bool isInBleedover = false;
        private System.Collections.Generic.Dictionary<GameObject, float> lastDamageTimes = new System.Collections.Generic.Dictionary<GameObject, float>();

        // Public properties
        public bool HasActiveBoom => hasBoomActive;
        public Vector3 WakePosition => wakePosition;

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
                // Create or update boom wake
                if (!hasBoomActive)
                {
                    CreateBoomWake();
                }
                else
                {
                    UpdateBoomWake();
                }
                
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
                        DeactivateBoom();
                    }
                    else
                    {
                        // Continue updating wake during bleedover
                        UpdateBoomWake();
                    }
                }
                
                // Check if wake has exceeded max lifetime
                float wakeAge = Time.time - wakeCreationTime;
                if (wakeAge >= maxWakeLifetime)
                {
                    Debug.Log("Sonic boom dissipated (max lifetime reached)");
                    DeactivateBoom();
                }
            }

            // Check for damage from wake
            if (hasBoomActive)
            {
                CheckWakeDamage();
                UpdateWakeVisuals();
                
                // Draw visual indicator
                if (showDebugGizmos)
                {
                    DrawWakeIndicator();
                }
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

        private void CreateBoomWake()
        {
            hasBoomActive = true;
            wakeCreationTime = Time.time;
            wakeDistanceTraveled = 0f;
            isInBleedover = false;
            
            // Calculate initial wake position behind player
            Vector3 velocity = locomotion.Velocity;
            Vector3 direction = velocity.magnitude > 0.1f ? velocity.normalized : -transform.forward;
            wakePosition = transform.position - direction * wakeTrailDistance;

            // Create wake object for visualization
            if (wakeObject == null)
            {
                wakeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                wakeObject.name = "SonicBoomWake";
                wakeObject.transform.localScale = Vector3.one * wakeRadius * 2f;
                
                // Make it semi-transparent
                Renderer renderer = wakeObject.GetComponent<Renderer>();
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
                Collider col = wakeObject.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            
            wakeObject.transform.position = wakePosition;
            wakeObject.SetActive(true);

            // Spawn visual effect
            if (spawnShockwaveEffect)
            {
                ShockwaveEffect.SpawnShockwave(wakePosition, wakeColor, wakeRadius * 1.5f);
            }

            Debug.Log("Sonic Boom wake created!");
        }

        private void UpdateBoomWake()
        {
            // Calculate target position behind player
            Vector3 velocity = locomotion.Velocity;
            Vector3 direction = velocity.magnitude > 0.1f ? velocity.normalized : -transform.forward;
            Vector3 targetPosition = transform.position - direction * wakeTrailDistance;

            // Track distance before moving
            Vector3 oldPosition = wakePosition;

            // Wake follows player smoothly
            wakePosition = Vector3.MoveTowards(wakePosition, targetPosition, wakeMoveSpeed * Time.deltaTime);
            
            // Track total distance traveled
            wakeDistanceTraveled += Vector3.Distance(oldPosition, wakePosition);
            
            if (wakeObject != null)
            {
                wakeObject.transform.position = wakePosition;
            }
        }

        private void CheckWakeDamage()
        {
            // Find all objects within wake radius
            Collider[] nearbyColliders = Physics.OverlapSphere(wakePosition, wakeRadius);

            foreach (Collider col in nearbyColliders)
            {
                GameObject target = col.gameObject;
                
                // Skip if this is the wake object itself
                if (target == wakeObject) continue;

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

                        // Spawn visual effect on damage
                        if (spawnShockwaveEffect && Time.time - lastDamageTime >= 0.3f)
                        {
                            Color effectColor = isPlayer ? dangerColor : shockwaveColor;
                            ShockwaveEffect.SpawnShockwave(wakePosition, effectColor, wakeRadius * 1.5f);
                            lastDamageTime = Time.time;
                        }
                    }
                }
            }
        }

        private void UpdateWakeVisuals()
        {
            if (wakeObject == null) return;

            Renderer renderer = wakeObject.GetComponent<Renderer>();
            if (renderer == null) return;

            // Calculate fade based on age and bleedover
            float wakeAge = Time.time - wakeCreationTime;
            float ageProgress = wakeAge / maxWakeLifetime;
            
            // Calculate bleedover fade
            float bleedoverFade = 1f;
            if (isInBleedover)
            {
                float bleedoverElapsed = Time.time - speedDroppedBelowThresholdTime;
                bleedoverFade = 1f - (bleedoverElapsed / bleedoverDuration);
            }

            // Combine fades
            float finalAlpha = Mathf.Min(1f - ageProgress, bleedoverFade);
            finalAlpha = Mathf.Clamp01(finalAlpha);

            // Update material color
            Color currentColor = isInBleedover ? dangerColor : wakeColor;
            currentColor.a = finalAlpha * 0.5f; // Base transparency
            renderer.material.color = currentColor;

            // Update scale based on fade (shrink as it dissipates)
            float scale = Mathf.Lerp(wakeRadius * 2f, wakeRadius * 1.5f, 1f - finalAlpha);
            wakeObject.transform.localScale = Vector3.one * scale;
        }

        private void DeactivateBoom()
        {
            hasBoomActive = false;
            isInBleedover = false;
            
            if (wakeObject != null)
            {
                wakeObject.SetActive(false);
            }
            
            // Clear damage tracking
            lastDamageTimes.Clear();
            
            Debug.Log("Sonic boom deactivated");
        }
        
        void OnDestroy()
        {
            // Clean up wake object when component is destroyed
            if (wakeObject != null)
            {
                Destroy(wakeObject);
            }
        }

        private void DrawWakeIndicator()
        {
            if (!hasBoomActive) return;

            // Determine color based on distance to player (closer = more danger)
            float distanceToPlayer = Vector3.Distance(wakePosition, transform.position);
            Color currentColor = distanceToPlayer < wakeTrailDistance * 0.5f ? dangerColor : wakeColor;

            // Draw wake sphere
            Debug.DrawLine(wakePosition + Vector3.up * wakeRadius, wakePosition - Vector3.up * wakeRadius, currentColor);
            Debug.DrawLine(wakePosition + Vector3.right * wakeRadius, wakePosition - Vector3.right * wakeRadius, currentColor);
            Debug.DrawLine(wakePosition + Vector3.forward * wakeRadius, wakePosition - Vector3.forward * wakeRadius, currentColor);

            // Draw connection to player
            Debug.DrawLine(transform.position, wakePosition, currentColor);

            // Draw circle around wake (simplified)
            int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * Mathf.PI * 2f;
                float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

                Vector3 point1 = wakePosition + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * wakeRadius;
                Vector3 point2 = wakePosition + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * wakeRadius;

                Debug.DrawLine(point1, point2, currentColor);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || !hasBoomActive) return;

            // Draw wake sphere in editor
            Gizmos.color = wakeColor;
            Gizmos.DrawWireSphere(wakePosition, wakeRadius);

            // Draw line to player
            Gizmos.DrawLine(transform.position, wakePosition);
        }
    }
}
