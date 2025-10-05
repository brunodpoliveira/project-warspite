using System.Linq;
using UnityEngine;
using Warspite.Movement;
using Warspite.TimeSystem;
using Warspite.UI;
using Warspite.Combat;

namespace Warspite.Bootstrap
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            // 1) Ensure GameSystems exists with Time + HUD
            var systems = GameObject.Find("GameSystems");
            if (!systems)
            {
                systems = new GameObject("GameSystems");
            }

            var time = systems.GetComponent<TimeDilationController>();
            if (!time) time = systems.AddComponent<TimeDilationController>();

            var hud = systems.GetComponent<DebugHUD>();
            if (!hud) hud = systems.AddComponent<DebugHUD>();

            // 2) Find player and attach movement/collision/catch
            var player = FindPlayer();
            if (player)
            {
                var momentum = player.GetComponent<MomentumLocomotion>();
                if (!momentum) momentum = player.AddComponent<MomentumLocomotion>();

                var bounce = player.GetComponent<CollisionBounce>();
                if (!bounce) bounce = player.AddComponent<CollisionBounce>();

                var catchComp = player.GetComponent<BulletCatch>();
                if (!catchComp) catchComp = player.AddComponent<BulletCatch>();

                // Wire refs
                catchComp.timeController = time;
                catchComp.hand = FindRightHand(player.transform);
                // projectileLayer left default; BulletCatch will accept all layers if not set

                hud.timeController = time;
                hud.momentum = momentum;
            }

            // 3) Ensure a turret exists shooting projectiles
            EnsureTurret(player);
        }

        private static GameObject FindPlayer()
        {
            // Try tag first
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) return tagged;

            // Try Invector typical controller name contains "ThirdPerson" or VBOT skin root
            var candidates = Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                                    .Select(cc => cc.gameObject)
                                    .ToList();
            if (candidates.Count > 0) return candidates[0];

            // Fallback: any root object named like player/vbot
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var guess = roots.FirstOrDefault(r => r.name.ToLower().Contains("player") || r.name.ToLower().Contains("vbot"));
            if (guess) return guess;

            // As last resort, create a dummy capsule to interact with features (not ideal, but keeps bootstrap robust)
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "PlayerAuto";
            var cc = capsule.AddComponent<CharacterController>();
            capsule.transform.position = new Vector3(0, 1, 0);
            return capsule;
        }

        private static Transform FindRightHand(Transform root)
        {
            // Common right-hand name patterns
            string[] patterns = { "RightHand", "Hand_R", "R_Hand", "Right Wrist", "mixamorig:RightHand", "VBOT_:RightHand" };
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name;
                if (patterns.Any(p => n.IndexOf(p, System.StringComparison.OrdinalIgnoreCase) >= 0))
                    return t;
            }
            // If not found, just use root (will still work, just catch near body)
            return root;
        }

        private static void EnsureTurret(GameObject player)
        {
            if (Object.FindFirstObjectByType<TurretSpawner>() != null) return;

            var turret = new GameObject("Turret");
            turret.transform.position = player ? player.transform.position + player.transform.forward * 12f + Vector3.up * 1.2f : new Vector3(0, 1.2f, 12f);
            turret.transform.rotation = player ? Quaternion.LookRotation((player.transform.position + Vector3.up) - turret.transform.position) : Quaternion.identity;

            var spawner = turret.AddComponent<TurretSpawner>();
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(turret.transform);
            muzzle.localPosition = new Vector3(0, 0, 0.5f);
            muzzle.localRotation = Quaternion.identity;
            spawner.muzzle = muzzle;

            // Build a runtime projectile prefab (sphere + rigidbody + Projectile)
            var projGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DontDestroyOnLoad(projGO); // keep a template across reloads
            var rb = projGO.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var proj = projGO.AddComponent<Projectile>();

            projGO.name = "ProjectilePrefab";
            proj.lifetime = 10f;
            spawner.projectilePrefab = proj;
            spawner.interval = 1.25f;
            spawner.muzzleSpeed = 22f;

            // Hide the template
            projGO.hideFlags = HideFlags.HideInHierarchy;
        }
    }
}
