using UnityEngine;

namespace Warspite.World
{
    /// <summary>
    /// ScriptableObject that defines enemy type stats.
    /// Create different configs for Pistol, Shotgun, Grenadier, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Config", menuName = "Warspite/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Enemy Identity")]
        public string enemyName = "Enemy";
        public EnemyType enemyType = EnemyType.PistolInfantry;

        [Header("Health")]
        public float maxHealth = 100f;

        [Header("Weapon Stats")]
        public WeaponType weaponType = WeaponType.Pistol;
        public int magazineSize = 15;
        public float reloadTime = 2f;
        public float fireRate = 2f; // Rounds per second
        public int burstCount = 1; // 1 = semi-auto, 3 = burst, etc.
        public float burstDelay = 0.1f; // Delay between shots in a burst

        [Header("Damage")]
        public float baseDamage = 20f;
        public bool useDamageFalloff = true;
        
        [Header("Projectile")]
        public float muzzleSpeed = 75f;
        public float projectileSize = 0.2f;
        public GameObject projectilePrefab; // Optional custom projectile

        [Header("Accuracy")]
        public float baseSpreadAngle = 2f; // Base cone angle in degrees
        public float spreadMultiplier = 0.05f; // Additional spread per meter distance
        public bool useAccuracyCone = true;

        [Header("Special Weapon Behavior")]
        [Tooltip("For shotguns: number of pellets per shot")]
        public int pelletsPerShot = 1;
        [Tooltip("For shotguns: spread pattern")]
        public float pelletSpread = 8f;

        [Header("Grenade Settings (Grenadier only)")]
        public bool usesGrenades = false;
        public float grenadeBlastDamage = 80f;
        public float grenadeShrapnelDamage = 40f;
        public float grenadeTimer = 3f;
        public float grenadeBlastRadius = 5f;

        [Header("Sniper Settings (Sniper only)")]
        public bool usesLaserTelegraph = false;
        public float telegraphDuration = 1.5f;
        public Color laserColor = Color.red;

        [Header("Visual")]
        public Color projectileColor = Color.red;
        public float projectileEmission = 2f;

        // Calculated properties
        public float FireInterval => 1f / fireRate;
    }

    public enum EnemyType
    {
        PistolInfantry,
        ShotgunRusher,
        Grenadier,
        AssaultRifle,
        MachineGunner,
        Sniper
    }

    public enum WeaponType
    {
        Pistol,
        Shotgun,
        Grenade,
        AssaultRifle,
        MachineGun,
        SniperRifle
    }
}
