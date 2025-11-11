using UnityEngine;
using UnityEngine.Events;

namespace Warspite.Core
{
    /// <summary>
    /// Generic health component for any entity.
    /// Handles damage, healing, death, and events.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float destroyDelay = 0f;
        [SerializeField] private bool invulnerable = false;

        [Header("Events")]
        public UnityEvent<float> OnDamaged;
        public UnityEvent<float> OnHealed;
        public UnityEvent OnDeath;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => invulnerable;

        void Awake()
        {
            currentHealth = maxHealth;
            IsDead = false;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || invulnerable) return;

            currentHealth -= amount;
            currentHealth = Mathf.Max(currentHealth, 0);

            OnDamaged?.Invoke(amount);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            OnHealed?.Invoke(amount);
        }

        public void SetHealth(float amount)
        {
            currentHealth = Mathf.Clamp(amount, 0, maxHealth);
            if (currentHealth <= 0 && !IsDead)
            {
                Die();
            }
        }

        private void Die()
        {
            if (IsDead) return;

            IsDead = true;
            OnDeath?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        public bool IsCritical(float threshold = 0.25f)
        {
            return HealthPercent <= threshold;
        }

        public void SetInvulnerable(bool value)
        {
            invulnerable = value;
        }
    }
}
