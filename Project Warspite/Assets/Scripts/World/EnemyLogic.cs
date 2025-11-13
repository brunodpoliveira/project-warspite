using UnityEngine;
using System.Collections.Generic;
using Warspite.UI;
using Warspite.Systems;

namespace Warspite.World
{
    [System.Serializable]
    public class EnemyTrackingSettings
    {
        [Tooltip("Base rotation speed in degrees per second before modifiers.")]
        public float baseSpeed = 5f;
        [Tooltip("If enabled, time dilation scales tracking using the multipliers below.")]
        public bool scaleWithTime = true;
        [Tooltip("Multiplier applied per time dilation level (index 0 = normal, 3 = deepest slow).")]
        public float[] levelMultipliers = new float[4] { 1f, 0.65f, 0.4f, 0.2f };
        [Tooltip("Lower bound on all computed multipliers so enemies never completely freeze.")]
        [Range(0.01f, 1f)] public float minMultiplier = 0.05f;
    }

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
        
        // Cached laser aim direction for snipers, so the beam does not perfectly snap to the player
        private Vector3 laserAimDirection;
        
        [Header("Movement")]
        [SerializeField] private bool enableMovement = true;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float strafeInterval = 2f; // Change direction every N seconds
        [SerializeField] private float minDistanceToTarget = 5f;
        [SerializeField] private float maxDistanceToTarget = 15f;
        
        [Header("Tracking")]
        [SerializeField] private EnemyTrackingSettings trackingSettings = new EnemyTrackingSettings();
        [SerializeField] private TimeDilationController timeController;

        private float lastFireTime;
        private int burstsFired;
        private int currentAmmo;
        private bool isReloading = false;
        private float reloadStartTime;
        private bool isChargingShot = false; // For sniper telegraph
        private float chargeStartTime;
        
        // Movement state
        private Vector3 moveDirection;
        private float nextMoveChangeTime;

        // Public properties for UI/debugging
        public EnemyConfig Config => config;
        public float LastFireTime => lastFireTime;
        public float FireInterval => GetFireInterval(); // Exposed for UI
        public bool IsReloading => isReloading;
        public float ReloadProgress => isReloading ? Mathf.Clamp01((Time.time - reloadStartTime) / GetReloadTime()) : 0f;
        public int CurrentAmmo => currentAmmo;
        public int MagazineSize => GetMagazineSize();

        void OnValidate()
        {
            EnsureTrackingSettings();
        }

        private void EnsureTrackingSettings()
        {
            if (trackingSettings == null)
            {
                trackingSettings = new EnemyTrackingSettings();
            }

            if (trackingSettings.levelMultipliers == null || trackingSettings.levelMultipliers.Length == 0)
            {
                trackingSettings.levelMultipliers = new float[4] { 1f, 0.65f, 0.4f, 0.2f };
            }
        }

        // Helper methods to get values with overrides
        private float GetFireRate() => fireRateOverride > 0 ? fireRateOverride : config.fireRate;
        private float GetFireInterval() => 1f / GetFireRate();
        private float GetDamage() => damageOverride > 0 ? damageOverride : config.baseDamage;
        private int GetMagazineSize() => magazineSizeOverride > 0 ? magazineSizeOverride : config.magazineSize;
        private float GetReloadTime() => reloadTimeOverride > 0 ? reloadTimeOverride : config.reloadTime;

        void Start()
        {
            EnsureTrackingSettings();

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

            if (trackingSettings.scaleWithTime && timeController == null)
            {
                timeController = FindFirstObjectByType<TimeDilationController>();
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
            
            // Handle movement (except for snipers and if disabled)
            if (enableMovement && config.enemyType != EnemyType.Sniper && target != null)
            {
                UpdateMovement();
            }

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

            // Initialize laser aim direction toward current target at telegraph start
            if (target != null)
            {
                Vector3 startPos = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 0.5f;
                Vector3 initialDir = (target.position - startPos);
                if (initialDir.sqrMagnitude > 0.0001f)
                {
                    laserAimDirection = initialDir.normalized;
                }
                else
                {
                    laserAimDirection = transform.forward;
                }
            }
            else
            {
                laserAimDirection = transform.forward;
            }
        }

        private void UpdateLaserTelegraph()
        {
            if (laserRenderer == null || target == null) return;

            Vector3 startPos = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 0.5f;
            Vector3 toTarget = (target.position - startPos);
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                toTarget = transform.forward;
            }

            Vector3 targetDir = toTarget.normalized;

            // Rotate the cached laser direction toward the player with a capped angular speed
            float rotationMultiplier = trackingSettings.scaleWithTime ? EvaluateTrackingMultiplier() : 1f;
            float rotationSpeed = trackingSettings.baseSpeed * rotationMultiplier;
            float maxRadiansThisFrame = rotationSpeed * Mathf.Deg2Rad * Time.deltaTime;

            if (laserAimDirection.sqrMagnitude < 0.0001f)
            {
                laserAimDirection = targetDir;
            }
            else
            {
                laserAimDirection = Vector3.RotateTowards(laserAimDirection, targetDir, maxRadiansThisFrame, 0f);
            }

            laserRenderer.SetPosition(0, startPos);
            laserRenderer.SetPosition(1, startPos + laserAimDirection * 100f); // Long laser
        }

        private void FireProjectile()
        {
            // Check ammo
            if (currentAmmo <= 0) return;

            // Shotgun fires multiple pellets
            int projectileCount = config.weaponType == WeaponType.Shotgun ? config.pelletsPerShot : 1;
            
            // Store all pellets to disable collision between them
            List<GameObject> pellets = new List<GameObject>();

            for (int i = 0; i < projectileCount; i++)
            {
                GameObject pellet = FireSingleProjectile(i, projectileCount);
                if (pellet != null)
                {
                    pellets.Add(pellet);
                }
            }
            
            // Disable collision between pellets from the same shot
            if (pellets.Count > 1)
            {
                for (int i = 0; i < pellets.Count; i++)
                {
                    for (int j = i + 1; j < pellets.Count; j++)
                    {
                        Collider col1 = pellets[i].GetComponent<Collider>();
                        Collider col2 = pellets[j].GetComponent<Collider>();
                        if (col1 != null && col2 != null)
                        {
                            Physics.IgnoreCollision(col1, col2);
                        }
                    }
                }
            }

            // Consume ammo (one ammo per shot, even if multiple pellets)
            currentAmmo--;

            // Notify crosshair
            if (crosshair != null)
            {
                crosshair.OnTurretFired();
            }
        }

        private GameObject FireSingleProjectile(int pelletIndex, int totalPellets)
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
            
            // For shotgun pellets, offset spawn position slightly to prevent collision
            if (totalPellets > 1)
            {
                // Spread pellets in a small circle around the spawn point
                float angle = (pelletIndex / (float)totalPellets) * 360f * Mathf.Deg2Rad;
                float offsetRadius = 0.1f; // Small offset to prevent collision
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * offsetRadius;
                spawnPosition += offset;
            }

            // Calculate aim direction
            bool isGrenade = config.usesGrenades;
            
            if (trackTarget && target != null)
            {
                float distanceToTarget = Vector3.Distance(spawnPosition, target.position);

                if (isGrenade)
                {
                    aimDirection = CalculateBallisticVelocity(spawnPosition, target.position, config.muzzleSpeed, config.grenadeArcPreference);

                    if (aimDirection == Vector3.zero)
                    {
                        Vector3 toTarget = (target.position - spawnPosition).normalized;
                        float fallbackAngle = Mathf.Lerp(30f, 60f, config.grenadeArcPreference) * Mathf.Deg2Rad;
                        aimDirection = toTarget * config.muzzleSpeed * Mathf.Cos(fallbackAngle) + Vector3.up * config.muzzleSpeed * Mathf.Sin(fallbackAngle);
                    }
                    
                    if (config.useAccuracyCone)
                    {
                        float totalSpread = config.baseSpreadAngle + (distanceToTarget * config.spreadMultiplier);
                        aimDirection = ApplySpread(aimDirection.normalized, totalSpread) * aimDirection.magnitude;
                    }
                }
                else
                {
                    bool useLaserAim = config.enemyType == EnemyType.Sniper && config.usesLaserTelegraph && laserAimDirection.sqrMagnitude > 0.0001f;
                    if (useLaserAim)
                    {
                        aimDirection = laserAimDirection.normalized;
                    }
                    else
                    {
                        aimDirection = (target.position - spawnPosition).normalized;
                    }
                    
                    if (config.useAccuracyCone)
                    {
                        float totalSpread = config.baseSpreadAngle + (distanceToTarget * config.spreadMultiplier);
                        aimDirection = ApplySpread(aimDirection, totalSpread);
                    }

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
            
            return proj;
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
        
        private void UpdateMovement()
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            // Change movement direction periodically
            if (Time.time >= nextMoveChangeTime)
            {
                ChooseNewMoveDirection(distanceToTarget);
                nextMoveChangeTime = Time.time + strafeInterval;
            }
            
            // Apply movement
            if (moveDirection != Vector3.zero)
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }
            
            // Rotate toward target (scaled by time dilation if enabled)
            Vector3 directionToTarget = (target.position - transform.position);
            directionToTarget.y = 0; // Keep on horizontal plane
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                
                // Scale rotation speed by time dilation curve (simulates player reflex advantage)
                float rotationMultiplier = trackingSettings.scaleWithTime ? EvaluateTrackingMultiplier() : 1f;
                float rotationSpeed = trackingSettings.baseSpeed * rotationMultiplier;
                float step = rotationSpeed * Time.deltaTime;
                
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, step);
            }
        }
        
        private float EvaluateTrackingMultiplier()
        {
            float multiplier = Mathf.Max(Time.timeScale, trackingSettings.minMultiplier);

            if (timeController == null && trackingSettings.scaleWithTime)
            {
                timeController = FindFirstObjectByType<TimeDilationController>();
            }

            if (timeController != null && trackingSettings.levelMultipliers != null && trackingSettings.levelMultipliers.Length > 0)
            {
                int levelIndex = Mathf.Clamp(timeController.CurrentLevel, 0, trackingSettings.levelMultipliers.Length - 1);
                float levelMultiplier = Mathf.Max(trackingSettings.levelMultipliers[levelIndex], trackingSettings.minMultiplier);
                multiplier *= levelMultiplier;
            }

            return Mathf.Max(multiplier, trackingSettings.minMultiplier);
        }
        
        private void ChooseNewMoveDirection(float distanceToTarget)
        {
            // Decide whether to move forward/back or strafe
            bool shouldMoveCloser = distanceToTarget > maxDistanceToTarget;
            bool shouldMoveAway = distanceToTarget < minDistanceToTarget;
            
            Vector3 toTarget = (target.position - transform.position).normalized;
            toTarget.y = 0; // Keep on horizontal plane
            
            Vector3 right = Vector3.Cross(Vector3.up, toTarget); // Perpendicular to target direction
            
            if (shouldMoveCloser)
            {
                // Move forward (toward target) with some strafe
                moveDirection = toTarget + right * Random.Range(-0.5f, 0.5f);
            }
            else if (shouldMoveAway)
            {
                // Move backward (away from target) with some strafe
                moveDirection = -toTarget + right * Random.Range(-0.5f, 0.5f);
            }
            else
            {
                // Strafe left or right, occasionally forward/back
                float choice = Random.value;
                if (choice < 0.4f)
                {
                    moveDirection = right; // Strafe right
                }
                else if (choice < 0.8f)
                {
                    moveDirection = -right; // Strafe left
                }
                else if (choice < 0.9f)
                {
                    moveDirection = toTarget; // Move forward
                }
                else
                {
                    moveDirection = -toTarget; // Move back
                }
            }
            
            moveDirection.Normalize();
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
