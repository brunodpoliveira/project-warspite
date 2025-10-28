using UnityEngine;
using Warspite.Core;

namespace Warspite.World
{
    /// <summary>
    /// Grenade projectile with timed explosion, blast damage, and shrapnel.
    /// Can be caught and thrown back by player in L3.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Grenade : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [SerializeField] private float timer = 3f;
        [SerializeField] private float blastDamage = 80f;
        [SerializeField] private float shrapnelDamage = 40f;
        [SerializeField] private float blastRadius = 5f;
        [SerializeField] private bool debugExplosion = false;
        
        [Header("Visual Settings")]
        [SerializeField] private Color grenadeColor = Color.green;
        [SerializeField] private float blinkSpeed = 5f; // Blinks faster as timer runs out
        
        private Rigidbody rb;
        private float spawnTime;
        private Renderer grenadeRenderer;
        private Material grenadeMaterial;
        private Color emissiveColor;
        private bool hasExploded = false;
        
        public bool IsCaught { get; set; }
        
        // Public setters for EnemyLogic
        public void SetExplosionStats(float blastDmg, float shrapnelDmg, float radius, float time)
        {
            blastDamage = blastDmg;
            shrapnelDamage = shrapnelDmg;
            blastRadius = radius;
            timer = time;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            grenadeRenderer = GetComponent<Renderer>();
            
            if (grenadeRenderer != null)
            {
                grenadeMaterial = grenadeRenderer.material;
                emissiveColor = grenadeColor * 2f; // Emission intensity
            }
        }

        void Start()
        {
            spawnTime = Time.time;
        }

        void Update()
        {
            // Check if timer expired
            float timeRemaining = timer - (Time.time - spawnTime);
            
            if (timeRemaining <= 0 && !hasExploded)
            {
                Explode();
                return;
            }
            
            // Visual feedback - blink faster as timer runs out
            if (grenadeMaterial != null)
            {
                float blinkRate = blinkSpeed * (1f + (1f - timeRemaining / timer) * 3f); // Faster near end
                float emission = Mathf.PingPong(Time.time * blinkRate, 1f);
                
                // Set emission color (works for both URP and Standard)
                grenadeMaterial.SetColor("_EmissionColor", emissiveColor * emission);
                grenadeMaterial.EnableKeyword("_EMISSION");
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
            // Grenades bounce, don't explode on impact
            // They only explode when timer runs out
            
            // Optional: Add bounce sound effect here
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;
            
            if (debugExplosion)
            {
                Debug.Log($"[Grenade] Exploded at {transform.position} | Blast: {blastDamage} | Shrapnel: {shrapnelDamage} | Radius: {blastRadius}m");
            }
            
            // Find all colliders in blast radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, blastRadius);
            
            foreach (Collider hitCollider in hitColliders)
            {
                Health health = hitCollider.GetComponent<Health>();
                if (health != null && !health.IsDead)
                {
                    // Check if grenade was thrown by player (caught)
                    bool isPlayerGrenade = IsCaught;
                    
                    // Don't damage enemies if thrown by enemy (friendly fire prevention)
                    EnemyLogic enemy = hitCollider.GetComponent<EnemyLogic>();
                    if (!isPlayerGrenade && enemy != null)
                    {
                        continue; // Skip enemy damage from enemy grenades
                    }
                    
                    // Calculate distance from explosion center
                    float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                    
                    // Calculate damage falloff based on distance
                    // Full damage at center, reduced at edges
                    float distanceRatio = 1f - (distance / blastRadius);
                    distanceRatio = Mathf.Clamp01(distanceRatio);
                    
                    // Blast damage (full at center, 0 at edge)
                    float finalBlastDamage = blastDamage * distanceRatio;
                    
                    // Shrapnel damage (more consistent, 50% at edge)
                    float finalShrapnelDamage = shrapnelDamage * Mathf.Lerp(0.5f, 1f, distanceRatio);
                    
                    float totalDamage = finalBlastDamage + finalShrapnelDamage;
                    
                    if (debugExplosion)
                    {
                        Debug.Log($"[Grenade] Hit {hitCollider.name} | Distance: {distance:F1}m | Blast: {finalBlastDamage:F1} | Shrapnel: {finalShrapnelDamage:F1} | Total: {totalDamage:F1}");
                    }
                    
                    health.TakeDamage(totalDamage);
                    
                    // Optional: Apply knockback force
                    Rigidbody targetRb = hitCollider.GetComponent<Rigidbody>();
                    if (targetRb != null)
                    {
                        Vector3 explosionDir = (hitCollider.transform.position - transform.position).normalized;
                        float force = 500f * distanceRatio;
                        targetRb.AddForce(explosionDir * force, ForceMode.Impulse);
                    }
                }
            }
            
            // TODO: Spawn explosion VFX
            // TODO: Play explosion sound
            
            // Destroy grenade
            Destroy(gameObject);
        }

        // Draw blast radius in editor
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, blastRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, blastRadius);
        }
    }
}
