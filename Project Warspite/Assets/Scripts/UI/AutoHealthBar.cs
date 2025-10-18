using UnityEngine;
using Warspite.Core;

namespace Warspite.UI
{
    /// <summary>
    /// Automatically creates a health bar for this entity on start.
    /// Attach to any GameObject with a Health component.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class AutoHealthBar : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
        [SerializeField] private bool hideWhenFull = false; // Changed to false for debugging
        [SerializeField] private bool hideWhenDead = true;

        private GameObject healthBarInstance;

        void Start()
        {
            CreateHealthBar();
        }

        private void CreateHealthBar()
        {
            Health health = GetComponent<Health>();
            if (health == null)
            {
                Debug.LogWarning($"AutoHealthBar on {gameObject.name}: No Health component found!");
                return;
            }

            // Create robust world-space bar under global canvas
            healthBarInstance = WorldHealthBar.CreateFor(health, offset, hideWhenFull, hideWhenDead);
        }

        void OnDestroy()
        {
            if (healthBarInstance != null)
            {
                Destroy(healthBarInstance);
            }
        }
    }
}
