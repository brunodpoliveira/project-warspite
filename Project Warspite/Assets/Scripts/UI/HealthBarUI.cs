using UnityEngine;
using UnityEngine.UI;
using Warspite.Core;

namespace Warspite.UI
{
    /// <summary>
    /// World-space health bar that follows an entity.
    /// Automatically scales and fades based on health percentage.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class HealthBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private bool hideWhenDead = true;
        [SerializeField] private float fadeSpeed = 2f;

        [Header("Colors")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private float criticalThreshold = 0.25f;
        [SerializeField] private float damagedThreshold = 0.6f;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Camera mainCamera;
        private Transform target;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Auto-find health component if not assigned
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            // Set up canvas
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
        }

        void Start()
        {
            mainCamera = Camera.main;
            
            if (health != null)
            {
                target = health.transform;
                
                // Subscribe to health events
                health.OnDamaged.AddListener(OnHealthChanged);
                health.OnHealed.AddListener(OnHealthChanged);
                health.OnDeath.AddListener(OnDeath);
            }

            UpdateHealthBar();
        }

        void LateUpdate()
        {
            if (target == null || mainCamera == null) return;

            // Position above target
            transform.position = target.position + offset;

            // Face camera
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);

            // Update health bar every frame (fallback in case events don't fire)
            UpdateHealthBar();

            // Update visibility
            UpdateVisibility();
        }

        private void UpdateHealthBar()
        {
            if (health == null || fillImage == null)
            {
                Debug.LogWarning("HealthBarUI: health or fillImage is null!");
                return;
            }

            float healthPercent = health.HealthPercent;
            Debug.Log($"HealthBarUI: Updating health bar - {health.CurrentHealth}/{health.MaxHealth} ({healthPercent:P0})");

            // Update using UI Image fillAmount for robust behavior
            if (fillImage.type != Image.Type.Filled)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            fillImage.fillAmount = Mathf.Clamp01(healthPercent);

            // Update color based on health
            if (healthPercent <= criticalThreshold)
            {
                fillImage.color = criticalColor;
            }
            else if (healthPercent <= damagedThreshold)
            {
                fillImage.color = damagedColor;
            }
            else
            {
                fillImage.color = healthyColor;
            }
        }

        private void UpdateVisibility()
        {
            if (health == null || canvasGroup == null) return;

            float targetAlpha = 1f;

            // Hide when full
            if (hideWhenFull && health.HealthPercent >= 0.99f)
            {
                targetAlpha = 0f;
            }

            // Hide when dead
            if (hideWhenDead && health.IsDead)
            {
                targetAlpha = 0f;
            }

            // Smooth fade
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
        }

        private void OnHealthChanged(float amount)
        {
            UpdateHealthBar();
        }

        private void OnDeath()
        {
            UpdateHealthBar();
        }

        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        public void SetHideWhenFull(bool hide)
        {
            hideWhenFull = hide;
        }

        public void SetHideWhenDead(bool hide)
        {
            hideWhenDead = hide;
        }

        /// <summary>
        /// Creates a health bar UI for the given health component.
        /// </summary>
        public static GameObject CreateHealthBar(Health health, Transform parent = null)
        {
            // Create canvas
            GameObject healthBarObj = new GameObject("HealthBar");
            healthBarObj.transform.SetParent(parent != null ? parent : health.transform);
            healthBarObj.transform.localPosition = Vector3.zero;

            Canvas canvas = healthBarObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = healthBarObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;

            GraphicRaycaster raycaster = healthBarObj.AddComponent<GraphicRaycaster>();

            // Set canvas size
            RectTransform canvasRect = healthBarObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 0.3f);

            // Create background
            GameObject backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(healthBarObj.transform);
            backgroundObj.transform.localPosition = Vector3.zero;
            backgroundObj.transform.localScale = Vector3.one;

            Image backgroundImage = backgroundObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            backgroundImage.sprite = CreateWhiteSprite();

            RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0);
            bgRect.anchorMax = new Vector2(1, 1);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Create fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(backgroundObj.transform);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localScale = Vector3.one;

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = Color.green;
            fillImage.sprite = CreateWhiteSprite();
            // Configure as a filled image that reduces horizontally from left to right
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            // Stretch fill to background and let fillAmount control visible width
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(-4, -4); // Padding inside background

            // Add HealthBarUI component
            HealthBarUI healthBarUI = healthBarObj.AddComponent<HealthBarUI>();
            healthBarUI.health = health;
            healthBarUI.fillImage = fillImage;
            healthBarUI.backgroundImage = backgroundImage;

            return healthBarObj;
        }

        /// <summary>
        /// Creates a simple white 1x1 sprite for UI rendering
        /// </summary>
        private static Sprite CreateWhiteSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
    }
}
