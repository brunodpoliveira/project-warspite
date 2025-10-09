using UnityEngine;

namespace Warspite.World
{
    /// <summary>
    /// Simple physics-based projectile.
    /// Auto-destroys after lifetime. Uses Rigidbody so it slows with Time.timeScale.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 10f;
        
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
            // Simple bounce, or destroy on impact
            if (!collision.gameObject.CompareTag("Player"))
            {
                // Could add impact effects here
            }
        }
    }
}
