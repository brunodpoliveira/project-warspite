using UnityEngine;
using UnityEngine.UI;
using Warspite.Core;

namespace Warspite.UI
{
    /// <summary>
    /// World-space health bar that follows an enemy and displays their health.
    /// Automatically updates when the enemy takes damage or heals.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health targetHealth;
        [SerializeField] private Transform targetTransform;
        
        [Header("UI Elements")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
        [SerializeField] private Vector2 barSize = new Vector2(0.5f, 0.05f);
        [SerializeField] private bool hideWhenFull = false;
        [SerializeField] private bool hideWhenDead = true;
        [SerializeField] private float fadeSpeed = 2f;
        
        [Header("Colors")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private float criticalThreshold = 0.25f;
        
        private Camera mainCamera;
        private RectTransform rectTransform;
        private float targetAlpha = 1f;
        private bool isInitialized = false;
        private static Sprite whiteSprite;

        /// <summary>
        /// Initialize the health bar with a target Health component
        /// </summary>
        public void Initialize(Health health, Transform target)
        {
            targetHealth = health;
            targetTransform = target;
            
            if (targetHealth == null)
            {
                Debug.LogError("EnemyHealthBar: Cannot initialize with null Health component!");
                Destroy(gameObject);
                return;
            }
            
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();
            
            // Create UI elements if they don't exist
            CreateUIElements();
            
            // Subscribe to health events
            targetHealth.OnDamaged.AddListener(OnHealthChanged);
            targetHealth.OnHealed.AddListener(OnHealthChanged);
            targetHealth.OnDeath.AddListener(OnTargetDeath);
            
            // Initial update
            UpdateHealthBar();
            
            isInitialized = true;
            
            Debug.Log($"EnemyHealthBar initialized for {targetTransform.name} at position {transform.position}");
        }

        private void CreateUIElements()
        {
            // Create white sprite if needed
            if (whiteSprite == null)
            {
                Texture2D whiteTex = new Texture2D(1, 1);
                whiteTex.SetPixel(0, 0, Color.white);
                whiteTex.Apply();
                whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
            
            // Ensure we have a CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            
            // Create background if it doesn't exist
            if (backgroundImage == null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform, false);
                backgroundImage = bgObj.AddComponent<Image>();
                backgroundImage.sprite = whiteSprite;
                backgroundImage.color = backgroundColor;
                backgroundImage.type = Image.Type.Simple;
                
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.anchoredPosition = Vector2.zero;
            }
            
            // Create fill if it doesn't exist
            if (fillImage == null)
            {
                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(transform, false);
                fillImage = fillObj.AddComponent<Image>();
                fillImage.sprite = whiteSprite;
                fillImage.color = healthyColor;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                
                RectTransform fillRect = fillObj.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;
                fillRect.anchoredPosition = Vector2.zero;
            }
            
            // Set size
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = barSize; // Use size directly in world space
            }
        }

        void LateUpdate()
        {
            if (!isInitialized || targetHealth == null || targetTransform == null)
                return;
            
            // Position the health bar above the enemy
            UpdatePosition();
            
            // Face the camera
            FaceCamera();
            
            // Handle visibility fading
            UpdateVisibility();
        }

        private void UpdatePosition()
        {
            if (targetTransform != null)
            {
                transform.position = targetTransform.position + offset;
            }
        }

        private void FaceCamera()
        {
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }

        private void UpdateVisibility()
        {
            // Determine target alpha
            if (targetHealth.IsDead && hideWhenDead)
            {
                targetAlpha = 0f;
            }
            else if (targetHealth.HealthPercent >= 0.99f && hideWhenFull)
            {
                targetAlpha = 0f;
            }
            else
            {
                targetAlpha = 1f;
            }
            
            // Smoothly fade
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
                
                // Destroy if fully faded and dead
                if (targetHealth.IsDead && canvasGroup.alpha < 0.01f)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void UpdateHealthBar()
        {
            if (targetHealth == null || fillImage == null)
                return;
            
            // Update fill amount
            float healthPercent = targetHealth.HealthPercent;
            fillImage.fillAmount = healthPercent;
            
            // Update color based on health
            if (healthPercent <= criticalThreshold)
            {
                fillImage.color = criticalColor;
            }
            else
            {
                fillImage.color = Color.Lerp(criticalColor, healthyColor, 
                    (healthPercent - criticalThreshold) / (1f - criticalThreshold));
            }
        }

        private void OnHealthChanged(float amount)
        {
            UpdateHealthBar();
        }

        private void OnTargetDeath()
        {
            UpdateHealthBar();
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (targetHealth != null)
            {
                targetHealth.OnDamaged.RemoveListener(OnHealthChanged);
                targetHealth.OnHealed.RemoveListener(OnHealthChanged);
                targetHealth.OnDeath.RemoveListener(OnTargetDeath);
            }
        }

        /// <summary>
        /// Manually update the health bar (useful for testing)
        /// </summary>
        public void ForceUpdate()
        {
            UpdateHealthBar();
        }

        /// <summary>
        /// Set custom offset for this health bar
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        /// <summary>
        /// Set custom size for this health bar
        /// </summary>
        public void SetSize(Vector2 newSize)
        {
            barSize = newSize;
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = barSize;
            }
        }
    }
}
