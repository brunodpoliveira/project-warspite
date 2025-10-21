using UnityEngine;
using Warspite.Core;

namespace Warspite.UI
{
    /// <summary>
    /// Simple testing script to damage/heal enemies and test health bars.
    /// Attach to any GameObject and use the keyboard shortcuts.
    /// </summary>
    public class HealthBarTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private float damageAmount = 10f;
        [SerializeField] private float healAmount = 20f;
        [SerializeField] private float testRange = 10f;
        
        [Header("Controls")]
        [SerializeField] private KeyCode damageKey = KeyCode.T;
        [SerializeField] private KeyCode healKey = KeyCode.Y;
        [SerializeField] private KeyCode killKey = KeyCode.U;

        void Update()
        {
            if (Input.GetKeyDown(damageKey))
            {
                DamageNearbyEnemies();
            }
            
            if (Input.GetKeyDown(healKey))
            {
                HealNearbyEnemies();
            }
            
            if (Input.GetKeyDown(killKey))
            {
                KillNearbyEnemies();
            }
        }

        private void DamageNearbyEnemies()
        {
            Health[] allHealth = FindObjectsByType<Health>(FindObjectsSortMode.None);
            int count = 0;
            
            foreach (Health health in allHealth)
            {
                if (health.IsDead) continue;
                
                // Skip player
                if (health.GetComponent<Warspite.Player.PlayerHealth>() != null)
                    continue;
                
                float distance = Vector3.Distance(transform.position, health.transform.position);
                if (distance <= testRange)
                {
                    health.TakeDamage(damageAmount);
                    count++;
                }
            }
        }

        private void HealNearbyEnemies()
        {
            Health[] allHealth = FindObjectsByType<Health>(FindObjectsSortMode.None);
            int count = 0;
            
            foreach (Health health in allHealth)
            {
                if (health.IsDead) continue;
                
                // Skip player
                if (health.GetComponent<Warspite.Player.PlayerHealth>() != null)
                    continue;
                
                float distance = Vector3.Distance(transform.position, health.transform.position);
                if (distance <= testRange)
                {
                    health.Heal(healAmount);
                    count++;
                }
            }
        }

        private void KillNearbyEnemies()
        {
            Health[] allHealth = FindObjectsByType<Health>(FindObjectsSortMode.None);
            int count = 0;
            
            foreach (Health health in allHealth)
            {
                if (health.IsDead) continue;
                
                // Skip player
                if (health.GetComponent<Warspite.Player.PlayerHealth>() != null)
                    continue;
                
                float distance = Vector3.Distance(transform.position, health.transform.position);
                if (distance <= testRange)
                {
                    health.TakeDamage(health.CurrentHealth);
                    count++;
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            // Visualize test range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, testRange);
        }
    }
}
