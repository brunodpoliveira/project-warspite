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
        [SerializeField] private bool enableDoomPrediction = true;
        [SerializeField] private float predictionCheckInterval = 0.2f;
        
        private Rigidbody rb;
        private float spawnTime;
        private float lastPredictionCheck;
        private GameObject predictedTarget;

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
            // Try to damage whatever we hit
            Health health = collision.gameObject.GetComponent<Health>();
            if (health != null)
            {
                // Don't damage if this projectile was shot by an enemy and hits another enemy
                // (friendly fire prevention - could be made more sophisticated)
                bool isEnemyHittingEnemy = !IsCaught && collision.gameObject.GetComponent<EnemyLogic>() != null;
                
                if (!isEnemyHittingEnemy)
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
