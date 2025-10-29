using UnityEngine;
using Warspite.UI;

namespace Warspite.World
{
    /// <summary>
    /// Main enemy behavior script. Uses EnemyConfig for stats.
    /// Handles firing, reloading, and projectile spawning.
    /// Refactored from SimpleTurret to be data-driven.
    /// </summary>
    public class EnemyLogic : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private EnemyConfig config;

        [Header("Per-Instance Overrides (Optional)")]
        [Tooltip("Leave at 0 to use config value")]
        [SerializeField] private float fireRateOverride = 0f;
        [Tooltip("Leave at 0 to use config value")]
        [SerializeField] private float damageOverride = 0f;
        [Tooltip("Leave at 0 to use config value")]
        [SerializeField] private int magazineSizeOverride = 0;
        [Tooltip("Leave at 0 to use config value")]
        [SerializeField] private float reloadTimeOverride = 0f;

        [Header("Targeting")]
        [SerializeField] private Transform target;
        [SerializeField] private bool trackTarget = true;
        [SerializeField] private Transform muzzlePoint; // Optional spawn point
        [SerializeField] private float minSpawnDistance = 1.5f;

        [Header("UI")]
        [SerializeField] private TurningCrosshair crosshair;

        [Header("Sniper Laser (if applicable)")]
        [SerializeField] private LineRenderer laserRenderer;

        private float lastFireTime;
        private int burstsFired;
        private int currentAmmo;
        private bool isReloading = false;
        private float reloadStartTime;
        private bool isChargingShot = false; // For sniper telegraph
        private float chargeStartTime;

        // Public properties for UI/debugging
        public EnemyConfig Config => config;
        public float LastFireTime => lastFireTime;
        public float FireInterval => GetFireInterval(); // Exposed for UI
        public bool IsReloading => isReloading;
        public float ReloadProgress => isReloading ? Mathf.Clamp01((Time.time - reloadStartTime) / GetReloadTime()) : 0f;
        public int CurrentAmmo => currentAmmo;
        public int MagazineSize => GetMagazineSize();

        // Helper methods to get values with overrides
        private float GetFireRate() => fireRateOverride > 0 ? fireRateOverride : config.fireRate;
        private float GetFireInterval() => 1f / GetFireRate();
        private float GetDamage() => damageOverride > 0 ? damageOverride : config.baseDamage;
        private int GetMagazineSize() => magazineSizeOverride > 0 ? magazineSizeOverride : config.magazineSize;
        private float GetReloadTime() => reloadTimeOverride > 0 ? reloadTimeOverride : config.reloadTime;

        void Start()
        {
            if (config == null)
            {
                Debug.LogError($"EnemyLogic on {gameObject.name} has no EnemyConfig assigned!");
                enabled = false;
                return;
            }

            lastFireTime = Time.time - GetFireInterval(); // Fire immediately on start
            currentAmmo = GetMagazineSize();

            // Setup laser for sniper
            if (config.usesLaserTelegraph)
            {
                // Auto-create LineRenderer if needed
                if (laserRenderer == null)
                {
                    laserRenderer = gameObject.AddComponent<LineRenderer>();
                    laserRenderer.positionCount = 2;
                    laserRenderer.useWorldSpace = true;
                    
                    // Find appropriate material/shader
                    Material laserMat = new Material(Shader.Find("Sprites/Default"));
                    laserMat.color = config.laserColor;
                    laserRenderer.material = laserMat;
                }
                
                laserRenderer.enabled = false;
                laserRenderer.startColor = config.laserColor;
                laserRenderer.endColor = config.laserColor;
                laserRenderer.startWidth = 0.05f;
                laserRenderer.endWidth = 0.05f;
            }

            // Auto-find player if no target assigned
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
            
            // Auto-add health bar if missing
            if (GetComponent<Warspite.UI.AutoHealthBar>() == null)
            {
                gameObject.AddComponent<Warspite.UI.AutoHealthBar>();
            }
        }

        void Update()
        {
            if (config == null) return;

            // Handle sniper telegraph charging
            if (config.usesLaserTelegraph && isChargingShot)
            {
                UpdateLaserTelegraph();

                if (Time.time - chargeStartTime >= config.telegraphDuration)
                {
                    // Telegraph complete, fire!
                    FireProjectile();
                    isChargingShot = false;
                    if (laserRenderer != null) laserRenderer.enabled = false;
                    lastFireTime = Time.time;
                }
                return; // Don't process normal firing while charging
            }

            // Check if reload is complete
            if (isReloading)
            {
                if (Time.time - reloadStartTime >= GetReloadTime())
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

            // Fire at intervals (uses scaled time so enemy slows with time dilation)
            if (Time.time - lastFireTime >= GetFireInterval())
            {
                // Sniper uses telegraph
                if (config.usesLaserTelegraph)
                {
                    StartSniperTelegraph();
                }
                else
                {
                    StartBurst();
                }
                lastFireTime = Time.time;
            }

            // Continue burst if in progress
            if (burstsFired < config.burstCount)
            {
                if (Time.time - lastFireTime >= burstsFired * config.burstDelay)
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

        private void StartSniperTelegraph()
        {
            isChargingShot = true;
            chargeStartTime = Time.time;
            if (laserRenderer != null)
            {
                laserRenderer.enabled = true;
            }
        }

        private void UpdateLaserTelegraph()
        {
            if (laserRenderer == null || target == null) return;

            Vector3 startPos = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 0.5f;
            Vector3 aimDir = (target.position - startPos).normalized;

            laserRenderer.SetPosition(0, startPos);
            laserRenderer.SetPosition(1, startPos + aimDir * 100f); // Long laser
        }

        private void FireProjectile()
        {
            // Check ammo
            if (currentAmmo <= 0) return;

            // Shotgun fires multiple pellets
            int projectileCount = config.weaponType == WeaponType.Shotgun ? config.pelletsPerShot : 1;

            for (int i = 0; i < projectileCount; i++)
            {
                FireSingleProjectile(i, projectileCount);
            }

            // Consume ammo (one ammo per shot, even if multiple pellets)
            currentAmmo--;

            // Notify crosshair
            if (crosshair != null)
            {
                crosshair.OnTurretFired();
            }
        }

        private void FireSingleProjectile(int pelletIndex, int totalPellets)
        {
            // Create projectile
            GameObject proj = CreateProjectile();

            // Determine spawn position
            Vector3 spawnPosition;
            Vector3 aimDirection;

            if (muzzlePoint != null)
            {
                spawnPosition = muzzlePoint.position;
            }
            else
            {
                spawnPosition = transform.position + Vector3.up * 0.5f;
            }

            // Calculate aim direction
            bool isGrenade = config.usesGrenades;
            
            if (trackTarget && target != null)
            {
                float distanceToTarget = Vector3.Distance(spawnPosition, target.position);

                if (isGrenade)
                {
                    // For grenades: Calculate ballistic trajectory (returns velocity vector)
                    aimDirection = CalculateBallisticVelocity(spawnPosition, target.position, config.muzzleSpeed, config.grenadeArcPreference);

                    // If ballistic calculation fails, use arc based on preference
                    if (aimDirection == Vector3.zero)
                    {
                        Vector3 toTarget = (target.position - spawnPosition).normalized;
                        float fallbackAngle = Mathf.Lerp(30f, 60f, config.grenadeArcPreference) * Mathf.Deg2Rad;
                        aimDirection = toTarget * config.muzzleSpeed * Mathf.Cos(fallbackAngle) + Vector3.up * config.muzzleSpeed * Mathf.Sin(fallbackAngle);
                    }
                    
                    // Apply slight spread to grenades (but don't normalize, keep velocity)
                    if (config.useAccuracyCone)
                    {
                        float totalSpread = config.baseSpreadAngle + (distanceToTarget * config.spreadMultiplier);
                        aimDirection = ApplySpread(aimDirection.normalized, totalSpread) * aimDirection.magnitude;
                    }
                }
                else
                {
                    // For bullets: Calculate direction (will be multiplied by speed later)
                    aimDirection = (target.position - spawnPosition).normalized;
                    
                    // Apply accuracy cone
                    if (config.useAccuracyCone)
                    {
                        float totalSpread = config.baseSpreadAngle + (distanceToTarget * config.spreadMultiplier);
                        aimDirection = ApplySpread(aimDirection, totalSpread);
                    }

                    // Apply shotgun pellet spread
                    if (config.weaponType == WeaponType.Shotgun && totalPellets > 1)
                    {
                        aimDirection = ApplySpread(aimDirection, config.pelletSpread);
                    }
                }
            }
            else
            {
                aimDirection = transform.forward;
            }

            // Ensure spawn position is far enough from enemy
            spawnPosition += aimDirection.normalized * minSpawnDistance;
            proj.transform.position = spawnPosition;

            // Configure and launch projectile or grenade
            Projectile projectile = proj.GetComponent<Projectile>();
            Grenade grenade = proj.GetComponent<Grenade>();
            
            if (grenade != null)
            {
                // Configure grenade
                grenade.SetExplosionStats(
                    config.grenadeBlastDamage,
                    config.grenadeShrapnelDamage,
                    config.grenadeBlastRadius,
                    config.grenadeTimer
                );
                
                // Launch grenade with ballistic velocity (already includes speed and arc)
                grenade.Launch(aimDirection);
            }
            else if (projectile != null)
            {
                // Set damage (with override if set)
                projectile.SetDamage(GetDamage());
                
                // Set falloff settings from config
                projectile.SetUseDamageFalloff(config.useDamageFalloff);
                
                // Launch projectile (direction * speed)
                projectile.Launch(aimDirection.normalized * config.muzzleSpeed);
            }
        }

        private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            if (spreadAngle <= 0) return direction;

            float angleX = Random.Range(-spreadAngle, spreadAngle);
            float angleY = Random.Range(-spreadAngle, spreadAngle);
            Quaternion spread = Quaternion.Euler(angleX, angleY, 0);
            return spread * direction;
        }

        private GameObject CreateProjectile()
        {
            if (config.projectilePrefab != null)
            {
                return Instantiate(config.projectilePrefab);
            }

            // Check if this enemy uses grenades
            bool isGrenadier = config.usesGrenades;
            
            // Auto-create projectile or grenade
            PrimitiveType shape = isGrenadier ? PrimitiveType.Sphere : PrimitiveType.Cube;
            GameObject obj = GameObject.CreatePrimitive(shape);
            obj.transform.localScale = Vector3.one * (isGrenadier ? config.projectileSize * 2f : config.projectileSize);
            obj.name = isGrenadier ? $"Grenade_{config.enemyName}" : $"Projectile_{config.enemyName}";

            // Add Rigidbody
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = isGrenadier ? 0.5f : 0.1f; // Grenades are heavier
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
            
            // Configure collider for grenades (make them bouncy)
            if (isGrenadier)
            {
                SphereCollider collider = obj.GetComponent<SphereCollider>();
                if (collider != null)
                {
                    // Create bouncy physics material
                    PhysicsMaterial bouncyMat = new PhysicsMaterial("GrenadeBounce");
                    bouncyMat.bounciness = 0.6f;
                    bouncyMat.dynamicFriction = 0.4f;
                    bouncyMat.staticFriction = 0.4f;
                    bouncyMat.frictionCombine = PhysicsMaterialCombine.Average;
                    bouncyMat.bounceCombine = PhysicsMaterialCombine.Maximum;
                    collider.material = bouncyMat;
                }
            }

            // Add appropriate script
            if (isGrenadier)
            {
                Grenade grenade = obj.AddComponent<Grenade>();
            }
            else
            {
                Projectile projectile = obj.AddComponent<Projectile>();
            }

            // Color based on config
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material mat = new Material(shader);
                mat.color = config.projectileColor;

                // Add emission for glow effect
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", config.projectileColor * config.projectileEmission);
                }

                renderer.material = mat;
            }

            return obj;
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
            currentAmmo = GetMagazineSize();

            // Notify crosshair
            if (crosshair != null)
            {
                crosshair.OnReloadComplete();
            }
        }

        /// <summary>
        /// Calculate ballistic trajectory velocity to hit a target.
        /// Returns velocity vector (not normalized), or Vector3.zero if target is unreachable.
        /// </summary>
        /// <param name="arcPreference">0 = low/flat arc, 1 = high/steep arc</param>
        private Vector3 CalculateBallisticVelocity(Vector3 origin, Vector3 target, float speed, float arcPreference = 0.5f)
        {
            Vector3 toTarget = target - origin;
            Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
            float distance = toTargetXZ.magnitude;
            float heightDiff = toTarget.y;

            // Gravity
            float gravity = Mathf.Abs(Physics.gravity.y);
            
            // Prevent division by zero
            if (distance < 0.1f)
            {
                return Vector3.zero;
            }

            // Calculate launch angle using ballistic trajectory formula
            float speedSq = speed * speed;
            float underRoot = speedSq * speedSq - gravity * (gravity * distance * distance + 2 * heightDiff * speedSq);

            // Check if target is reachable
            if (underRoot < 0)
            {
                // Target unreachable, use arc based on preference
                float fallbackAngle = Mathf.Lerp(30f, 60f, arcPreference) * Mathf.Deg2Rad;
                Vector3 direction = toTargetXZ.normalized;
                float hSpeed = speed * Mathf.Cos(fallbackAngle);
                float vSpeed = speed * Mathf.Sin(fallbackAngle);
                return direction * hSpeed + Vector3.up * vSpeed;
            }

            float root = Mathf.Sqrt(underRoot);
            float angle1 = Mathf.Atan((speedSq - root) / (gravity * distance)); // Low arc
            float angle2 = Mathf.Atan((speedSq + root) / (gravity * distance)); // High arc
            
            // Lerp between low and high arc based on preference
            // 0 = angle1 (flat), 1 = angle2 (steep)
            float launchAngle = Mathf.Lerp(angle1, angle2, arcPreference);

            // Calculate velocity components
            Vector3 horizontalDirection = toTargetXZ.normalized;
            float horizontalSpeed = speed * Mathf.Cos(launchAngle);
            float verticalSpeed = speed * Mathf.Sin(launchAngle);
            
            // Return actual velocity vector (NOT normalized)
            return horizontalDirection * horizontalSpeed + Vector3.up * verticalSpeed;
        }

        // Gizmos for debugging
        void OnDrawGizmosSelected()
        {
            if (config == null || target == null) return;

            Vector3 startPos = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 0.5f;
            
            // Draw line to target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPos, target.position);

            // Draw accuracy cone
            if (config.useAccuracyCone)
            {
                float distance = Vector3.Distance(startPos, target.position);
                float totalSpread = config.baseSpreadAngle + (distance * config.spreadMultiplier);
                
                Gizmos.color = Color.red;
                Vector3 toTarget = (target.position - startPos).normalized;
                Vector3 spreadDir1 = Quaternion.Euler(totalSpread, 0, 0) * toTarget;
                Vector3 spreadDir2 = Quaternion.Euler(-totalSpread, 0, 0) * toTarget;
                
                Gizmos.DrawRay(startPos, spreadDir1 * distance);
                Gizmos.DrawRay(startPos, spreadDir2 * distance);
            }
        }
    }
}
