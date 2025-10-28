using UnityEngine;
using UnityEditor;

namespace Warspite.World.Editor
{
    /// <summary>
    /// Editor utility to quickly create all 6 enemy config assets.
    /// Menu: Warspite > Create All Enemy Configs
    /// </summary>
    public static class EnemyConfigCreator
    {
        [MenuItem("Warspite/Create All Enemy Configs")]
        public static void CreateAllEnemyConfigs()
        {
            string folderPath = "Assets/Data/EnemyConfigs";
            
            // Create folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Data", "EnemyConfigs");
            }

            CreatePistolInfantry(folderPath);
            CreateShotgunRusher(folderPath);
            CreateGrenadier(folderPath);
            CreateAssaultRifleSoldier(folderPath);
            CreateMachineGunner(folderPath);
            CreateSniper(folderPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created all 6 enemy configs in {folderPath}");
        }

        private static void CreatePistolInfantry(string folderPath)
        {
            EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();
            
            config.enemyName = "Pistol Infantry";
            config.enemyType = EnemyType.PistolInfantry;
            config.maxHealth = 100f;
            
            config.weaponType = WeaponType.Pistol;
            config.magazineSize = 15;
            config.reloadTime = 2f;
            config.fireRate = 2f; // 2 rounds/sec
            config.burstCount = 1; // Semi-auto
            config.burstDelay = 0.1f;
            
            config.baseDamage = 20f;
            config.useDamageFalloff = true;
            
            config.muzzleSpeed = 75f;
            config.projectileSize = 0.2f;
            
            config.baseSpreadAngle = 2f;
            config.spreadMultiplier = 0.05f;
            config.useAccuracyCone = true;
            
            config.projectileColor = Color.red;
            config.projectileEmission = 2f;

            AssetDatabase.CreateAsset(config, $"{folderPath}/PistolInfantry.asset");
        }

        private static void CreateShotgunRusher(string folderPath)
        {
            EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();
            
            config.enemyName = "Shotgun Rusher";
            config.enemyType = EnemyType.ShotgunRusher;
            config.maxHealth = 120f;
            
            config.weaponType = WeaponType.Shotgun;
            config.magazineSize = 7;
            config.reloadTime = 2.5f;
            config.fireRate = 1f; // 1 round/sec (pump delay)
            config.burstCount = 1;
            config.burstDelay = 0.1f;
            
            config.baseDamage = 80f; // High damage at close range
            config.useDamageFalloff = true;
            
            config.muzzleSpeed = 60f; // Slower projectiles
            config.projectileSize = 0.15f;
            
            config.baseSpreadAngle = 8f; // Wide spread
            config.spreadMultiplier = 0.1f; // Falls off quickly
            config.useAccuracyCone = true;
            
            config.pelletsPerShot = 8; // Multiple pellets
            config.pelletSpread = 8f;
            
            config.projectileColor = new Color(1f, 0.5f, 0f); // Orange
            config.projectileEmission = 2f;

            AssetDatabase.CreateAsset(config, $"{folderPath}/ShotgunRusher.asset");
        }

        private static void CreateGrenadier(string folderPath)
        {
            EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();
            
            config.enemyName = "Grenadier";
            config.enemyType = EnemyType.Grenadier;
            config.maxHealth = 100f;
            
            config.weaponType = WeaponType.Grenade;
            config.magazineSize = 5;
            config.reloadTime = 3f;
            config.fireRate = 0.5f; // Slow fire rate (2 sec between grenades)
            config.burstCount = 1;
            config.burstDelay = 0.1f;
            
            config.baseDamage = 80f; // Blast damage
            config.useDamageFalloff = false; // Grenades use radius
            
            config.muzzleSpeed = 30f; // Arc trajectory
            config.projectileSize = 0.3f;
            
            config.baseSpreadAngle = 1f; // Accurate throw
            config.spreadMultiplier = 0.02f;
            config.useAccuracyCone = true;
            
            config.usesGrenades = true;
            config.grenadeBlastDamage = 80f;
            config.grenadeShrapnelDamage = 40f;
            config.grenadeTimer = 3f;
            config.grenadeBlastRadius = 5f;
            
            config.projectileColor = Color.green;
            config.projectileEmission = 1.5f;

            AssetDatabase.CreateAsset(config, $"{folderPath}/Grenadier.asset");
        }

        private static void CreateAssaultRifleSoldier(string folderPath)
        {
            EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();
            
            config.enemyName = "Assault Rifle Soldier";
            config.enemyType = EnemyType.AssaultRifle;
            config.maxHealth = 100f;
            
            config.weaponType = WeaponType.AssaultRifle;
            config.magazineSize = 30;
            config.reloadTime = 2f;
            config.fireRate = 6f; // 6 rounds/sec (but in bursts)
            config.burstCount = 3; // 3-round burst
            config.burstDelay = 0.1f;
            
            config.baseDamage = 30f;
            config.useDamageFalloff = true;
            
            config.muzzleSpeed = 100f; // Fast projectiles
            config.projectileSize = 0.18f;
            
            config.baseSpreadAngle = 1f; // Tight grouping
            config.spreadMultiplier = 0.03f;
            config.useAccuracyCone = true;
            
            config.projectileColor = new Color(1f, 1f, 0f); // Yellow
            config.projectileEmission = 2f;

            AssetDatabase.CreateAsset(config, $"{folderPath}/AssaultRifleSoldier.asset");
        }

        private static void CreateMachineGunner(string folderPath)
        {
            EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();
            
            config.enemyName = "Machine Gunner";
            config.enemyType = EnemyType.MachineGunner;
            config.maxHealth = 150f; // Tougher
            
            config.weaponType = WeaponType.MachineGun;
            config.magazineSize = 150;
            config.reloadTime = 4f; // Long reload
            config.fireRate = 10f; // 10 rounds/sec sustained
            config.burstCount = 1; // Continuous fire
            config.burstDelay = 0.1f;
            
            config.baseDamage = 40f;
            config.useDamageFalloff = true;
            
            config.muzzleSpeed = 90f;
            config.projectileSize = 0.22f;
            
            config.baseSpreadAngle = 3f; // More spread due to recoil
            config.spreadMultiplier = 0.04f;
            config.useAccuracyCone = true;
            
            config.projectileColor = new Color(1f, 0.3f, 0f); // Orange-red
            config.projectileEmission = 2.5f;

            AssetDatabase.CreateAsset(config, $"{folderPath}/MachineGunner.asset");
        }

        private static void CreateSniper(string folderPath)
        {
            EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();
            
            config.enemyName = "Sniper";
            config.enemyType = EnemyType.Sniper;
            config.maxHealth = 80f; // Fragile
            
            config.weaponType = WeaponType.SniperRifle;
            config.magazineSize = 1; // Single shot
            config.reloadTime = 2f;
            config.fireRate = 0.5f; // Slow (2 sec between shots)
            config.burstCount = 1;
            config.burstDelay = 0.1f;
            
            config.baseDamage = 120f; // High damage
            config.useDamageFalloff = false; // Maintains damage at range
            
            config.muzzleSpeed = 150f; // Very fast
            config.projectileSize = 0.15f;
            
            config.baseSpreadAngle = 0.5f; // Very accurate
            config.spreadMultiplier = 0.01f;
            config.useAccuracyCone = true;
            
            config.usesLaserTelegraph = true;
            config.telegraphDuration = 1.5f;
            config.laserColor = Color.red;
            
            config.projectileColor = new Color(1f, 0f, 1f); // Magenta
            config.projectileEmission = 3f;

            AssetDatabase.CreateAsset(config, $"{folderPath}/Sniper.asset");
        }
    }
}
