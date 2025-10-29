using UnityEngine;
using Warspite.Core;

namespace Warspite.World
{
    /// <summary>
    /// Simple physics-based projectile.
    /// Auto-destroys after lifetime. Uses Rigidbody so it slows with Time.timeScale.
    /// Damages entities on impact with distance-based falloff.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private float lifetime = 10f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private bool destroyOnImpact = true;
        [SerializeField] private bool enableDoomPrediction = true;
        [SerializeField] private float predictionCheckInterval = 0.2f;
        
        [Header("Damage Falloff")]
        [SerializeField] private bool useDamageFalloff = true;
        [SerializeField] private AnimationCurve falloffCurve = AnimationCurve.Linear(0f, 1f, 50f, 0.3f);
        [Tooltip("Distance ranges: 0-10m = 100%, 10-30m = 80-100%, 30-50m = 50-80%, 50m+ = 30-50%")]
        [SerializeField] private float maxFalloffDistance = 50f;
        [SerializeField] private bool debugDamageFalloff = false;
        
        private Rigidbody rb;
        private float spawnTime;
        private float lastPredictionCheck;
        private GameObject predictedTarget;
        private Vector3 spawnPosition;

        public bool IsCaught { get; set; }
        
        // Public setters for EnemyLogic to configure
        public void SetDamage(float newDamage) => damage = newDamage;
        public void SetUseDamageFalloff(bool useFalloff) => useDamageFalloff = useFalloff;
        public void SetFalloffCurve(AnimationCurve curve) => falloffCurve = curve;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            spawnTime = Time.time;
            spawnPosition = transform.position;
        }

        void Update()
        {
            // Destroy after lifetime (using scaled time so bullets live longer in slow-mo)
            if (Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
            }

            // Predict if this projectile will hit an enemy (only for thrown projectiles)
            if (enableDoomPrediction && IsCaught && Time.time - lastPredictionCheck > predictionCheckInterval)
            {
                PredictImpact();
                lastPredictionCheck = Time.time;
            }
        }

        private void PredictImpact()
        {
            // Raycast in the direction of velocity to predict impact
            Vector3 velocity = rb.linearVelocity;
            if (velocity.magnitude < 0.1f) return;

            float checkDistance = velocity.magnitude * predictionCheckInterval * 2f;
            RaycastHit hit;

            if (Physics.Raycast(transform.position, velocity.normalized, out hit, checkDistance))
            {
                // Check if we hit an enemy
                Health health = hit.collider.GetComponent<Health>();
                DoomedTag doomedTag = hit.collider.GetComponent<DoomedTag>();

                if (health != null && !health.IsDead && doomedTag != null)
                {
                    // Check if this projectile will likely kill the enemy
                    if (damage >= health.CurrentHealth * 0.8f) // Will deal significant damage
                    {
                        if (predictedTarget != hit.collider.gameObject)
                        {
                            predictedTarget = hit.collider.gameObject;
                            doomedTag.MarkAsDoomed(gameObject);
                        }
                    }
                }
            }
        }

        public void Launch(Vector3 velocity)
        {
            rb.linearVelocity = velocity;
        }

        public Vector3 GetVelocity()
        {
            return rb.linearVelocity;
        }

        public void Freeze()
        {
            rb.isKinematic = true;
        }

        public void Unfreeze()
        {
            rb.isKinematic = false;
        }

        void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"[Projectile] Hit {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");
            
            // Try to damage whatever we hit (check object and parents)
            Health health = collision.gameObject.GetComponentInParent<Health>();
            if (health != null)
            {
                Debug.Log($"[Projectile] Found Health component on {collision.gameObject.name}");
                
                // Don't damage if this projectile was shot by an enemy and hits another enemy
                // (friendly fire prevention - could be made more sophisticated)
                bool isEnemyHittingEnemy = !IsCaught && collision.gameObject.GetComponentInParent<EnemyLogic>() != null;
                
                if (!isEnemyHittingEnemy)
                {
                    // Calculate damage with falloff
                    float finalDamage = CalculateDamageWithFalloff();
                    
                    Debug.Log($"[Projectile] Dealing {finalDamage:F1} damage to {collision.gameObject.name}");
                    
                    // Debug logging
                    if (debugDamageFalloff)
                    {
                        float distance = GetDistanceTraveled();
                        float multiplier = useDamageFalloff ? falloffCurve.Evaluate(distance) : 1f;
                        Debug.Log($"[Projectile] Hit {collision.gameObject.name} | Distance: {distance:F1}m | Base: {damage:F1} | Multiplier: {multiplier:F2}x | Final: {finalDamage:F1}");
                    }
                    
                    health.TakeDamage(finalDamage);
                }
                else
                {
                    Debug.Log($"[Projectile] Skipping damage - enemy hitting enemy");
                }
            }
            else
            {
                Debug.LogWarning($"[Projectile] No Health component found on {collision.gameObject.name}");
            }

            // Destroy projectile on impact (unless it's a caught one that should bounce)
            if (destroyOnImpact && !IsCaught)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Calculate damage based on distance traveled with falloff curve.
        /// </summary>
        private float CalculateDamageWithFalloff()
        {
            if (!useDamageFalloff)
            {
                return damage; // No falloff, return base damage
            }
            
            // Calculate distance traveled
            float distanceTraveled = Vector3.Distance(spawnPosition, transform.position);
            
            // Clamp distance to max falloff range
            float clampedDistance = Mathf.Clamp(distanceTraveled, 0f, maxFalloffDistance);
            
            // Evaluate falloff curve (returns multiplier 0-1)
            float falloffMultiplier = falloffCurve.Evaluate(clampedDistance);
            
            // Calculate final damage
            float finalDamage = damage * falloffMultiplier;
            
            return finalDamage;
        }
        
        /// <summary>
        /// Get current damage at current position (for debugging/UI)
        /// </summary>
        public float GetCurrentDamage()
        {
            return CalculateDamageWithFalloff();
        }
        
        /// <summary>
        /// Get distance traveled (for debugging/UI)
        /// </summary>
        public float GetDistanceTraveled()
        {
            return Vector3.Distance(spawnPosition, transform.position);
        }
    }
}
