using UnityEngine;

namespace Warspite.World
{
    /// <summary>
    /// Spawns projectiles at intervals to visualize time slowdown.
    /// Auto-creates simple cube projectiles if no prefab is assigned.
    /// </summary>
    public class SimpleTurret : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSize = 0.2f;

        [Header("Firing")]
        [SerializeField] private float interval = 1f;
        [SerializeField] private float muzzleSpeed = 20f;
        [SerializeField] private int burstCount = 1;
        [SerializeField] private float burstDelay = 0.1f;
        [SerializeField] private float spreadAngle = 0f;

        [Header("Targeting")]
        [SerializeField] private Transform target;
        [SerializeField] private bool trackTarget = true;

        private float lastFireTime;
        private int burstsFired;

        void Start()
        {
            lastFireTime = Time.time - interval; // Fire immediately on start
        }

        void Update()
        {
            // Fire bursts at intervals (uses scaled time so turret slows with time dilation)
            if (Time.time - lastFireTime >= interval)
            {
                StartBurst();
                lastFireTime = Time.time;
            }

            // Continue burst if in progress
            if (burstsFired < burstCount)
            {
                if (Time.time - lastFireTime >= burstsFired * burstDelay)
                {
                    FireProjectile();
                    burstsFired++;
                }
            }
        }

        private void StartBurst()
        {
            burstsFired = 0;
        }

        private void FireProjectile()
        {
            // Create projectile
            GameObject proj = CreateProjectile();
            
            // Spawn slightly in front of turret to avoid collision with turret itself
            Vector3 spawnOffset = transform.forward * 1f + Vector3.up * 0.5f;
            proj.transform.position = transform.position + spawnOffset;

            // Calculate direction
            Vector3 direction;
            if (trackTarget && target != null)
            {
                direction = (target.position - transform.position).normalized;
            }
            else
            {
                direction = transform.forward;
            }

            // Apply spread
            if (spreadAngle > 0)
            {
                float angleX = Random.Range(-spreadAngle, spreadAngle);
                float angleY = Random.Range(-spreadAngle, spreadAngle);
                Quaternion spread = Quaternion.Euler(angleX, angleY, 0);
                direction = spread * direction;
            }

            // Launch
            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Launch(direction * muzzleSpeed);
            }
        }

        private GameObject CreateProjectile()
        {
            if (projectilePrefab != null)
            {
                return Instantiate(projectilePrefab);
            }

            // Auto-create simple cube projectile
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = Vector3.one * projectileSize;
            cube.name = "Projectile";

            // Add Rigidbody
            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.useGravity = true;

            // Add Projectile script
            cube.AddComponent<Projectile>();

            // Random color for visual variety
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(
                    Random.Range(0.5f, 1f),
                    Random.Range(0.5f, 1f),
                    Random.Range(0.5f, 1f)
                );
                renderer.material = mat;
            }

            return cube;
        }
    }
}
