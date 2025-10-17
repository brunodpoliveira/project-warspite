using UnityEngine;
using Warspite.Core;

namespace Warspite.World
{
    /// <summary>
    /// Turret-specific health management.
    /// Turrets can be destroyed by projectile impacts.
    /// Disables SimpleTurret component when destroyed.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class TurretHealth : MonoBehaviour
    {
        [Header("Destruction")]
        [SerializeField] private float projectileDamage = 50f;
        [SerializeField] private bool disableTurretOnDeath = true;

        private Health health;
        private SimpleTurret turret;

        void Awake()
        {
            health = GetComponent<Health>();
            turret = GetComponent<SimpleTurret>();
        }

        void Start()
        {
            // Subscribe to death event
            health.OnDeath.AddListener(OnTurretDestroyed);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Check if hit by a projectile
            Projectile projectile = collision.gameObject.GetComponent<Projectile>();
            if (projectile != null && projectile.IsCaught)
            {
                // This is a thrown projectile (was caught by player)
                health.TakeDamage(projectileDamage);

                // Destroy the projectile
                Destroy(projectile.gameObject);

                Debug.Log($"Turret hit by thrown projectile! Health: {health.CurrentHealth}/{health.MaxHealth}");
            }
        }

        private void OnTurretDestroyed()
        {
            Debug.Log("Turret destroyed!");

            // Disable turret firing
            if (disableTurretOnDeath && turret != null)
            {
                turret.enabled = false;
            }

            // Visual feedback could go here (explosion, particles, etc.)
        }
    }
}
