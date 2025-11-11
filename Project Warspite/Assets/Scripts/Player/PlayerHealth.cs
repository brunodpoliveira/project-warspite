using UnityEngine;
using Warspite.Core;

namespace Warspite.Player
{
    /// <summary>
    /// Player-specific health management.
    /// Health degenerates over time to encourage aggressive play.
    /// Can be restored by "sucking" critical enemies (vampire mechanic).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Degeneration")]
        [SerializeField] private bool enableDegeneration = true;
        [SerializeField] private float degenerationRate = 2f; // HP per second
        [SerializeField] private float degenerationDelay = 3f; // Seconds before degeneration starts

        [Header("Vampire Mechanics")]
        [SerializeField] private float suckRange = 3f;
        [SerializeField] private float suckHealAmount = 30f;
        [SerializeField] private float gibbedHealPenalty = 0.5f; // Multiplier if enemy is gibbed
        [SerializeField] private KeyCode suckKey = KeyCode.F;
        
        [Header("Debug")]
        [SerializeField] private bool allowGodModeToggle = true;
        [SerializeField] private KeyCode godModeToggleKey = KeyCode.G;
        [SerializeField] private bool godModeEnabled = false;

        private Health health;
        private float timeSinceLastDamage;

        public bool IsGodModeEnabled => godModeEnabled;

        void Awake()
        {
            health = GetComponent<Health>();

            if (health == null)
            {
                Debug.LogError("PlayerHealth: Missing Health component!");
                return;
            }

            if (godModeEnabled)
            {
                health.SetInvulnerable(true);
            }
        }

        void Start()
        {
            timeSinceLastDamage = 0f;

            // Subscribe to damage events to reset degeneration timer
            if (health.OnDamaged != null)
            {
                health.OnDamaged.AddListener(OnDamaged);
            }
        }

        void Update()
        {
            HandleGodModeToggle();
            HandleDegeneration();
            HandleVampireSuck();
        }

        private void HandleDegeneration()
        {
            if (!enableDegeneration)
            {
                return;
            }
            
            if (health == null)
            {
                Debug.LogError("PlayerHealth: Health component is null!");
                return;
            }
            
            if (health.IsDead)
            {
                return;
            }

            // Use deltaTime (not unscaledDeltaTime) so degeneration works during gameplay
            timeSinceLastDamage += Time.deltaTime;

            if (timeSinceLastDamage >= degenerationDelay)
            {
                float damageThisFrame = degenerationRate * Time.deltaTime;
                health.TakeDamage(damageThisFrame);
            }
        }

        private void HandleVampireSuck()
        {
            if (Input.GetKeyDown(suckKey))
            {
                AttemptSuck();
            }
        }

        private void HandleGodModeToggle()
        {
            if (!allowGodModeToggle || health == null)
            {
                return;
            }

            if (Input.GetKeyDown(godModeToggleKey))
            {
                godModeEnabled = !godModeEnabled;
                health.SetInvulnerable(godModeEnabled);
                Debug.Log($"PlayerHealth: God mode {(godModeEnabled ? "ENABLED" : "disabled")}");
            }
        }

        private void AttemptSuck()
        {
            // Find nearby enemies at critical health
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, suckRange);

            foreach (Collider col in nearbyColliders)
            {
                Health enemyHealth = col.GetComponent<Health>();
                if (enemyHealth != null && enemyHealth != health && enemyHealth.IsCritical())
                {
                    // Suck this enemy
                    float healAmount = suckHealAmount;

                    // Check if enemy is gibbed (very low health)
                    if (enemyHealth.HealthPercent < 0.1f)
                    {
                        healAmount *= gibbedHealPenalty;
                    }

                    // Heal player
                    health.Heal(healAmount);

                    // Kill enemy
                    enemyHealth.TakeDamage(enemyHealth.CurrentHealth);

                    break; // Only suck one enemy per press
                }
            }
        }

        private void OnDamaged(float amount)
        {
            // Reset degeneration timer when taking damage
            timeSinceLastDamage = 0f;
        }

        void OnDrawGizmosSelected()
        {
            // Visualize suck range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, suckRange);
        }
    }
}
