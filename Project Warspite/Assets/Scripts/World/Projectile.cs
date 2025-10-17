using UnityEngine;
using Warspite.Core;

namespace Warspite.World
{
    /// <summary>
    /// Simple physics-based projectile.
    /// Auto-destroys after lifetime. Uses Rigidbody so it slows with Time.timeScale.
    /// Damages entities on impact.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 10f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private bool destroyOnImpact = true;
        
        private Rigidbody rb;
        private float spawnTime;

        public bool IsCaught { get; set; }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            spawnTime = Time.time;
        }

        void Update()
        {
            // Destroy after lifetime (using scaled time so bullets live longer in slow-mo)
            if (Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
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
            // Try to damage whatever we hit
            Health health = collision.gameObject.GetComponent<Health>();
            if (health != null)
            {
                // Don't damage if this projectile was shot by a turret and hits another turret
                // (friendly fire prevention - could be made more sophisticated)
                bool isTurretHittingTurret = !IsCaught && collision.gameObject.GetComponent<SimpleTurret>() != null;
                
                if (!isTurretHittingTurret)
                {
                    health.TakeDamage(damage);
                }
            }

            // Destroy projectile on impact (unless it's a caught one that should bounce)
            if (destroyOnImpact && !IsCaught)
            {
                Destroy(gameObject);
            }
        }
    }
}
