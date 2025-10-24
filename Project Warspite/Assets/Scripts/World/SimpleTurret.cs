using UnityEngine;
using Warspite.UI;

namespace Warspite.World
{
    /// <summary>
    /// Spawns projectiles at intervals to visualize time slowdown.
    /// Auto-creates simple cube projectiles if no prefab is assigned.
    /// Notifies TurningCrosshair when firing.
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

        [Header("Ammo System")]
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private float reloadTime = 2f;
        [SerializeField] private float minSpawnDistance = 1.5f; // Minimum distance to spawn projectile from turret

        [Header("Targeting")]
        [SerializeField] private Transform target;
        [SerializeField] private bool trackTarget = true;
        [SerializeField] private Transform muzzlePoint; // Optional spawn point

        [Header("UI")]
        [SerializeField] private TurningCrosshair crosshair;

        private float lastFireTime;
        private int burstsFired;
        private int currentAmmo;
        private bool isReloading = false;
        private float reloadStartTime;

        public float LastFireTime => lastFireTime;
        public float Interval => interval;
        public bool IsReloading => isReloading;
        public float ReloadProgress => isReloading ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime) : 0f;
        public int CurrentAmmo => currentAmmo;
        public int MagazineSize => magazineSize;

        void Start()
        {
            lastFireTime = Time.time - interval; // Fire immediately on start
            currentAmmo = magazineSize; // Start with full magazine
        }

        void Update()
        {
            // Check if reload is complete
            if (isReloading)
            {
                if (Time.time - reloadStartTime >= reloadTime)
                {
                    CompleteReload();
                }
                return; // Don't fire while reloading
            }

            // Check if we need to reload
            if (currentAmmo <= 0)
            {
                StartReload();
                return;
            }

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
            // Check ammo
            if (currentAmmo <= 0) return;

            // Create projectile
            GameObject proj = CreateProjectile();
            
            // Determine spawn position
            Vector3 spawnPosition;
            Vector3 aimDirection;
            
            if (muzzlePoint != null)
            {
                // Use muzzle point if assigned
                spawnPosition = muzzlePoint.position;
            }
            else
            {
                // Spawn in front of turret to avoid collision with turret itself
                spawnPosition = transform.position + Vector3.up * 0.5f;
            }

            // Calculate initial aim direction
            if (trackTarget && target != null)
            {
                // Calculate ballistic trajectory to target
                aimDirection = CalculateBallisticVelocity(spawnPosition, target.position, muzzleSpeed);
                
                // If ballistic calculation fails (target unreachable), aim directly
                if (aimDirection == Vector3.zero)
                {
                    aimDirection = (target.position - spawnPosition).normalized;
                }
            }
            else
            {
                aimDirection = transform.forward;
            }

            // Ensure spawn position is far enough from turret
            spawnPosition += aimDirection.normalized * minSpawnDistance;
            proj.transform.position = spawnPosition;

            // Apply spread
            if (spreadAngle > 0)
            {
                float angleX = Random.Range(-spreadAngle, spreadAngle);
                float angleY = Random.Range(-spreadAngle, spreadAngle);
                Quaternion spread = Quaternion.Euler(angleX, angleY, 0);
                aimDirection = spread * aimDirection;
            }

            // Launch
            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Launch(aimDirection.normalized * muzzleSpeed);
            }

            // Consume ammo
            currentAmmo--;

            // Notify crosshair
            if (crosshair != null)
            {
                crosshair.OnTurretFired();
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

            // Red color for tracer rounds
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Try to find URP Lit shader, fall back to Standard
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material mat = new Material(shader);
                mat.color = Color.red;

                // Add emission for glow effect
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.red * 2f);
                }

                renderer.material = mat;
            }

            return cube;
        }

        private void StartReload()
        {
            isReloading = true;
            reloadStartTime = Time.time;
            
            // Notify crosshair
            if (crosshair != null)
            {
                crosshair.OnReloadStart();
            }
        }

        private void CompleteReload()
        {
            isReloading = false;
            currentAmmo = magazineSize;
            
            // Notify crosshair
            if (crosshair != null)
            {
                crosshair.OnReloadComplete();
            }
        }

        /// <summary>
        /// Calculate ballistic trajectory velocity to hit a target.
        /// Returns normalized direction * speed, or Vector3.zero if target is unreachable.
        /// </summary>
        private Vector3 CalculateBallisticVelocity(Vector3 origin, Vector3 target, float speed)
        {
            Vector3 toTarget = target - origin;
            Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
            float distance = toTargetXZ.magnitude;
            float heightDiff = toTarget.y;

            // Gravity
            float gravity = Mathf.Abs(Physics.gravity.y);

            // Calculate launch angle using ballistic trajectory formula
            // We use the "low angle" solution for more direct shots
            float speedSq = speed * speed;
            float underRoot = speedSq * speedSq - gravity * (gravity * distance * distance + 2 * heightDiff * speedSq);

            // Check if target is reachable
            if (underRoot < 0)
            {
                return Vector3.zero; // Target unreachable
            }

            float root = Mathf.Sqrt(underRoot);
            float angle = Mathf.Atan((speedSq - root) / (gravity * distance));

            // Calculate velocity direction
            Vector3 direction = toTargetXZ.normalized;
            float verticalComponent = Mathf.Tan(angle);
            
            return (direction + Vector3.up * verticalComponent).normalized;
        }
    }
}
