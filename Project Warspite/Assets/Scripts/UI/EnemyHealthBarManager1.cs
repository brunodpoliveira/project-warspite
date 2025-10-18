using UnityEngine;
using System.Collections.Generic;
using Warspite.Core;

namespace Warspite.UI
{
    /// <summary>
    /// Manages health bars for all enemies in the scene.
    /// Automatically creates health bars for enemies with Health components.
    /// Can be configured to auto-detect enemies or manually register them.
    /// </summary>
    public class EnemyHealthBarManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoDetectEnemies = true;
        [SerializeField] private string enemyTag = "Enemy";
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float detectionRadius = 50f;
        [SerializeField] private float updateInterval = 0.5f;
        
        [Header("Health Bar Prefab")]
        [SerializeField] private GameObject healthBarPrefab;
        [SerializeField] private bool createPrefabIfMissing = true;
        
        [Header("Default Settings")]
        [SerializeField] private Vector3 defaultOffset = new Vector3(0, 2f, 0);
        [SerializeField] private Vector2 defaultSize = new Vector2(0.5f, 0.05f);
        
        private Canvas worldCanvas;
        private Dictionary<Health, EnemyHealthBar> activeHealthBars = new Dictionary<Health, EnemyHealthBar>();
        private float lastUpdateTime;

        void Start()
        {
            // Get or create world space canvas
            worldCanvas = WorldSpaceCanvas.GetOrCreate();
            
            // Create health bar prefab if needed
            if (healthBarPrefab == null && createPrefabIfMissing)
            {
                CreateHealthBarPrefab();
            }
            
            // Initial detection
            if (autoDetectEnemies)
            {
                DetectAndCreateHealthBars();
            }
        }

        void Update()
        {
            if (!autoDetectEnemies)
                return;
            
            // Periodically check for new enemies
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                DetectAndCreateHealthBars();
                CleanupDestroyedHealthBars();
            }
        }

        /// <summary>
        /// Automatically detect enemies and create health bars for them
        /// </summary>
        private void DetectAndCreateHealthBars()
        {
            // Find all Health components in the scene
            Health[] allHealthComponents = FindObjectsByType<Health>(FindObjectsSortMode.None);
            
            foreach (Health health in allHealthComponents)
            {
                // Skip if already has a health bar
                if (activeHealthBars.ContainsKey(health))
                    continue;
                
                // Skip player (assuming player has PlayerHealth component)
                if (health.GetComponent<Warspite.Player.PlayerHealth>() != null)
                    continue;
                
                // Check if within detection radius (if using radius)
                if (detectionRadius > 0)
                {
                    float distance = Vector3.Distance(transform.position, health.transform.position);
                    if (distance > detectionRadius)
                        continue;
                }
                
                // Check tag if specified
                if (!string.IsNullOrEmpty(enemyTag) && !health.CompareTag(enemyTag))
                {
                    // If no tag match, skip (unless tag is empty)
                    continue;
                }
                
                // Create health bar for this enemy
                CreateHealthBar(health);
            }
        }

        /// <summary>
        /// Create a health bar for a specific enemy
        /// </summary>
        public EnemyHealthBar CreateHealthBar(Health targetHealth)
        {
            if (targetHealth == null)
            {
                Debug.LogWarning("EnemyHealthBarManager: Cannot create health bar for null Health component!");
                return null;
            }
            
            if (activeHealthBars.ContainsKey(targetHealth))
            {
                Debug.LogWarning($"EnemyHealthBarManager: Health bar already exists for {targetHealth.gameObject.name}");
                return activeHealthBars[targetHealth];
            }
            
            // Instantiate health bar
            GameObject barObj;
            if (healthBarPrefab != null)
            {
                barObj = Instantiate(healthBarPrefab, worldCanvas.transform);
            }
            else
            {
                barObj = CreateHealthBarGameObject();
            }
            
            // Get or add EnemyHealthBar component
            EnemyHealthBar healthBar = barObj.GetComponent<EnemyHealthBar>();
            if (healthBar == null)
            {
                healthBar = barObj.AddComponent<EnemyHealthBar>();
            }
            
            // Initialize the health bar
            healthBar.Initialize(targetHealth, targetHealth.transform);
            healthBar.SetOffset(defaultOffset);
            healthBar.SetSize(defaultSize);
            
            // Track it
            activeHealthBars[targetHealth] = healthBar;
            
            Debug.Log($"Created health bar for {targetHealth.gameObject.name}");
            
            return healthBar;
        }

        /// <summary>
        /// Remove health bar for a specific enemy
        /// </summary>
        public void RemoveHealthBar(Health targetHealth)
        {
            if (activeHealthBars.TryGetValue(targetHealth, out EnemyHealthBar healthBar))
            {
                if (healthBar != null)
                {
                    Destroy(healthBar.gameObject);
                }
                activeHealthBars.Remove(targetHealth);
            }
        }

        /// <summary>
        /// Clean up health bars for destroyed enemies
        /// </summary>
        private void CleanupDestroyedHealthBars()
        {
            List<Health> toRemove = new List<Health>();
            
            foreach (var kvp in activeHealthBars)
            {
                if (kvp.Key == null || kvp.Value == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (Health health in toRemove)
            {
                activeHealthBars.Remove(health);
            }
        }

        /// <summary>
        /// Create a basic health bar GameObject
        /// </summary>
        private GameObject CreateHealthBarGameObject()
        {
            GameObject barObj = new GameObject("EnemyHealthBar");
            barObj.transform.SetParent(worldCanvas.transform, false);
            
            // Add RectTransform
            RectTransform rect = barObj.AddComponent<RectTransform>();
            rect.sizeDelta = defaultSize;
            
            return barObj;
        }

        /// <summary>
        /// Create a default health bar prefab at runtime
        /// </summary>
        private void CreateHealthBarPrefab()
        {
            // Create a simple prefab-like template
            GameObject template = CreateHealthBarGameObject();
            template.name = "HealthBarTemplate";
            
            // Add the health bar component
            EnemyHealthBar healthBar = template.AddComponent<EnemyHealthBar>();
            
            // This will be used as our "prefab"
            healthBarPrefab = template;
            template.SetActive(false);
            
            Debug.Log("Created default health bar prefab");
        }

        /// <summary>
        /// Manually register an enemy to have a health bar
        /// </summary>
        public void RegisterEnemy(GameObject enemy)
        {
            Health health = enemy.GetComponent<Health>();
            if (health != null)
            {
                CreateHealthBar(health);
            }
            else
            {
                Debug.LogWarning($"EnemyHealthBarManager: {enemy.name} does not have a Health component!");
            }
        }

        /// <summary>
        /// Manually unregister an enemy
        /// </summary>
        public void UnregisterEnemy(GameObject enemy)
        {
            Health health = enemy.GetComponent<Health>();
            if (health != null)
            {
                RemoveHealthBar(health);
            }
        }

        /// <summary>
        /// Clear all health bars
        /// </summary>
        public void ClearAllHealthBars()
        {
            foreach (var healthBar in activeHealthBars.Values)
            {
                if (healthBar != null)
                {
                    Destroy(healthBar.gameObject);
                }
            }
            activeHealthBars.Clear();
        }

        /// <summary>
        /// Get the health bar for a specific enemy
        /// </summary>
        public EnemyHealthBar GetHealthBar(Health targetHealth)
        {
            if (activeHealthBars.TryGetValue(targetHealth, out EnemyHealthBar healthBar))
            {
                return healthBar;
            }
            return null;
        }

        void OnDrawGizmosSelected()
        {
            // Visualize detection radius
            if (autoDetectEnemies && detectionRadius > 0)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, detectionRadius);
            }
        }
    }
}
