using UnityEngine;
using Warspite.Core;

namespace Warspite.World
{
    /// <summary>
    /// Marks an enemy as "doomed" - will be destroyed by an incoming projectile/object/melee.
    /// Also shows critical status for enemies that can be drained for HP.
    /// Provides visual feedback to prevent player from wasting resources on already-doomed enemies.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DoomedTag : MonoBehaviour
    {
        [Header("Doomed Visual Settings")]
        [SerializeField] private Color doomedColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange
        [SerializeField] private bool useColorOverlay = true;
        [SerializeField] private bool showSkullIcon = false;

        [Header("Critical Status Settings")]
        [SerializeField] private Color criticalColor = new Color(1f, 0f, 0.5f, 0.8f); // Pink/Magenta
        [SerializeField] private bool showCriticalIndicator = true;
        [SerializeField] private float criticalThreshold = 0.25f;

        [Header("Tag Duration")]
        [SerializeField] private float maxTagDuration = 10f; // Auto-remove tag after this time

        private Health health;
        private Renderer[] renderers;
        private Material[] originalMaterials;
        private bool isDoomed = false;
        private bool isCritical = false;
        private float tagTime;
        private GameObject doomedIndicator;
        private GameObject criticalIndicator;

        void Awake()
        {
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
            
            // Store original materials
            if (renderers.Length > 0)
            {
                originalMaterials = new Material[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    originalMaterials[i] = renderers[i].material;
                }
            }
        }

        void Start()
        {
            // Subscribe to death event to clean up
            if (health != null)
            {
                health.OnDeath.AddListener(OnDeath);
            }
        }

        void Update()
        {
            // Auto-remove tag after duration (in case projectile missed)
            if (isDoomed && Time.time - tagTime > maxTagDuration)
            {
                RemoveDoomedTag();
            }

            // Update critical status based on health
            if (health != null && !health.IsDead)
            {
                bool shouldBeCritical = health.HealthPercent <= criticalThreshold;
                if (shouldBeCritical && !isCritical && !isDoomed)
                {
                    ShowCriticalStatus();
                }
                else if (!shouldBeCritical && isCritical)
                {
                    HideCriticalStatus();
                }
            }
        }

        /// <summary>
        /// Marks this enemy as doomed by the specified projectile/object/melee
        /// </summary>
        public void MarkAsDoomed(GameObject source)
        {
            if (isDoomed) return; // Already doomed
            if (health != null && health.IsDead) return; // Already dead

            isDoomed = true;
            tagTime = Time.time;

            // Hide critical indicator if showing
            HideCriticalStatus();

            ApplyDoomedVisualFeedback();
        }

        /// <summary>
        /// Removes the doomed tag (e.g., if projectile missed)
        /// </summary>
        public void RemoveDoomedTag()
        {
            if (!isDoomed) return;

            isDoomed = false;
            RemoveDoomedVisualFeedback();
        }

        private void ShowCriticalStatus()
        {
            if (!showCriticalIndicator) return;
            if (isDoomed) return; // Don't show critical if doomed

            isCritical = true;
            ApplyCriticalVisualFeedback();
        }

        private void HideCriticalStatus()
        {
            if (!isCritical) return;

            isCritical = false;
            RemoveCriticalVisualFeedback();
        }

        private void ApplyDoomedVisualFeedback()
        {
            // Apply color overlay to materials
            if (useColorOverlay && renderers.Length > 0)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null && renderer.material != null)
                    {
                        // Create a new material instance with emission
                        Material mat = renderer.material;
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", doomedColor * 0.5f);
                        
                        // Tint the base color
                        if (mat.HasProperty("_Color"))
                        {
                            Color baseColor = mat.color;
                            mat.color = Color.Lerp(baseColor, doomedColor, 0.3f);
                        }
                    }
                }
            }

            // Create skull icon indicator
            if (showSkullIcon)
            {
                CreateSkullIndicator();
            }
        }

        private void ApplyCriticalVisualFeedback()
        {
            // Apply critical color overlay to materials
            if (useColorOverlay && renderers.Length > 0)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null && renderer.material != null)
                    {
                        Material mat = renderer.material;
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", criticalColor * 0.3f);
                        
                        if (mat.HasProperty("_Color"))
                        {
                            Color baseColor = mat.color;
                            mat.color = Color.Lerp(baseColor, criticalColor, 0.2f);
                        }
                    }
                }
            }

            // Create critical indicator (pulsing sphere)
            CreateCriticalIndicator();
        }

        private void RemoveCriticalVisualFeedback()
        {
            // Restore original materials
            if (originalMaterials != null && renderers.Length > 0)
            {
                for (int i = 0; i < renderers.Length && i < originalMaterials.Length; i++)
                {
                    if (renderers[i] != null && originalMaterials[i] != null)
                    {
                        renderers[i].material = originalMaterials[i];
                    }
                }
            }

            // Remove critical indicator
            if (criticalIndicator != null)
            {
                Destroy(criticalIndicator);
            }
        }

        private void RemoveDoomedVisualFeedback()
        {
            // Restore original materials
            if (originalMaterials != null && renderers.Length > 0)
            {
                for (int i = 0; i < renderers.Length && i < originalMaterials.Length; i++)
                {
                    if (renderers[i] != null && originalMaterials[i] != null)
                    {
                        renderers[i].material = originalMaterials[i];
                    }
                }
            }

            // Remove skull indicator
            if (doomedIndicator != null)
            {
                Destroy(doomedIndicator);
            }
        }

        private void CreateSkullIndicator()
        {
            // Create a simple sphere as skull placeholder (can be replaced with actual skull model/sprite)
            doomedIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            doomedIndicator.name = "DoomedIndicator";
            doomedIndicator.transform.SetParent(transform);
            doomedIndicator.transform.localPosition = Vector3.up * 2f;
            doomedIndicator.transform.localScale = Vector3.one * 0.3f;

            // Remove collider
            Collider col = doomedIndicator.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set color
            Renderer renderer = doomedIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = doomedColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", doomedColor);
                renderer.material = mat;
            }

            // Make it bob up and down
            doomedIndicator.AddComponent<FloatingIndicator>();
        }

        private void CreateCriticalIndicator()
        {
            // Create a pulsing ring/sphere to indicate critical status (drainable)
            criticalIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            criticalIndicator.name = "CriticalIndicator";
            criticalIndicator.transform.SetParent(transform);
            criticalIndicator.transform.localPosition = Vector3.up * 1.5f;
            criticalIndicator.transform.localScale = Vector3.one * 0.25f;

            // Remove collider
            Collider col = criticalIndicator.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set color
            Renderer renderer = criticalIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = criticalColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", criticalColor * 2f);
                renderer.material = mat;
            }

            // Make it pulse
            PulsingIndicator pulser = criticalIndicator.AddComponent<PulsingIndicator>();
            pulser.pulseSpeed = 3f;
            pulser.minScale = 0.2f;
            pulser.maxScale = 0.35f;
        }

        private void OnDeath()
        {
            // Clean up on death
            if (doomedIndicator != null)
            {
                Destroy(doomedIndicator);
            }
            if (criticalIndicator != null)
            {
                Destroy(criticalIndicator);
            }
        }

        void OnDestroy()
        {
            if (doomedIndicator != null)
            {
                Destroy(doomedIndicator);
            }
            if (criticalIndicator != null)
            {
                Destroy(criticalIndicator);
            }
        }

        public bool IsDoomed => isDoomed;
        public bool IsCritical => isCritical;
    }

    /// <summary>
    /// Simple component to make the doomed indicator float/bob
    /// </summary>
    public class FloatingIndicator : MonoBehaviour
    {
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;

        private Vector3 startPosition;

        void Start()
        {
            startPosition = transform.localPosition;
        }

        void Update()
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);
            
            // Rotate slowly
            transform.Rotate(Vector3.up, 50f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Simple component to make the critical indicator pulse
    /// </summary>
    public class PulsingIndicator : MonoBehaviour
    {
        public float pulseSpeed = 3f;
        public float minScale = 0.2f;
        public float maxScale = 0.35f;

        private Vector3 startPosition;

        void Start()
        {
            startPosition = transform.localPosition;
        }

        void Update()
        {
            // Pulse scale
            float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            transform.localScale = Vector3.one * scale;

            // Keep position stable
            transform.localPosition = startPosition;
        }
    }
}
