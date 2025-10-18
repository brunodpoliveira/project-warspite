using UnityEngine;
using Warspite.Core;

namespace Warspite.UI
{
    /// <summary>
    /// Simple component that automatically creates a health bar for the attached GameObject.
    /// Just attach this to any enemy with a Health component and it will create a health bar.
    /// This is an alternative to using the EnemyHealthBarManager for more manual control.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class AutoHealthBar : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
        [SerializeField] private Vector2 size = new Vector2(0.5f, 0.05f);
        [SerializeField] private bool createOnStart = true;
        
        [Header("Optional Prefab")]
        [SerializeField] private GameObject healthBarPrefab;
        
        private Health health;
        private EnemyHealthBar healthBar;
        private Canvas worldCanvas;

        void Start()
        {
            health = GetComponent<Health>();
            
            if (health == null)
            {
                Debug.LogError($"AutoHealthBar: No Health component found on {gameObject.name}!");
                enabled = false;
                return;
            }
            
            if (createOnStart)
            {
                CreateHealthBar();
            }
        }

        /// <summary>
        /// Create the health bar for this enemy
        /// </summary>
        public void CreateHealthBar()
        {
            if (healthBar != null)
            {
                Debug.LogWarning($"AutoHealthBar: Health bar already exists for {gameObject.name}");
                return;
            }
            
            // Get or create world canvas
            worldCanvas = WorldSpaceCanvas.GetOrCreate();
            
            // Create health bar GameObject
            GameObject barObj;
            if (healthBarPrefab != null)
            {
                barObj = Instantiate(healthBarPrefab, worldCanvas.transform);
            }
            else
            {
                barObj = new GameObject($"HealthBar_{gameObject.name}");
                barObj.transform.SetParent(worldCanvas.transform, false);
                
                RectTransform rect = barObj.AddComponent<RectTransform>();
                rect.sizeDelta = size;
            }
            
            // Get or add health bar component
            healthBar = barObj.GetComponent<EnemyHealthBar>();
            if (healthBar == null)
            {
                healthBar = barObj.AddComponent<EnemyHealthBar>();
            }
            
            // Initialize
            healthBar.Initialize(health, transform);
            healthBar.SetOffset(offset);
            healthBar.SetSize(size);
            
            Debug.Log($"AutoHealthBar: Created health bar for {gameObject.name}");
            Debug.Log($"  - Health Bar GameObject: {barObj.name}");
            Debug.Log($"  - Parent Canvas: {worldCanvas.name}");
            Debug.Log($"  - Health: {health.CurrentHealth}/{health.MaxHealth}");
        }

        /// <summary>
        /// Destroy the health bar
        /// </summary>
        public void DestroyHealthBar()
        {
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
                healthBar = null;
            }
        }

        void OnDestroy()
        {
            // Clean up health bar when enemy is destroyed
            DestroyHealthBar();
        }

        /// <summary>
        /// Update the offset of the health bar
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
            if (healthBar != null)
            {
                healthBar.SetOffset(offset);
            }
        }

        /// <summary>
        /// Update the size of the health bar
        /// </summary>
        public void SetSize(Vector2 newSize)
        {
            size = newSize;
            if (healthBar != null)
            {
                healthBar.SetSize(size);
            }
        }
    }
}
