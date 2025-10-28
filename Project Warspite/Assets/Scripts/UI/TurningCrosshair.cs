using UnityEngine;
using UnityEngine.UI;
using Warspite.World;

namespace Warspite.UI
{
    /// <summary>
    /// Crosshair that rotates to indicate enemy firing cadence.
    /// Spins after enemy fires, locks when ready to fire again.
    /// Provides visual feedback for enemy timing.
    /// Works with both SimpleTurret (legacy) and EnemyLogic (new system).
    /// </summary>
    public class TurningCrosshair : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyLogic enemy;
        [SerializeField] private RectTransform crosshairTransform;
        [SerializeField] private RectTransform reloadIndicatorTransform;

        [Header("Visual Settings")]
        [SerializeField] private float spinSpeed = 360f; // Degrees per second
        [SerializeField] private Color readyColor = Color.green;
        [SerializeField] private Color chargingColor = Color.yellow;
        [SerializeField] private Color firingColor = Color.red;
        [SerializeField] private float crosshairSize = 50f;

        [Header("Position")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0); // Above enemy head
        [SerializeField] private bool worldSpace = true;
        [SerializeField] private bool debugDraw = false;

        private Image crosshairImage;
        private Image reloadIndicatorImage;
        private Canvas canvas;
        private Camera mainCamera;
        private float lastFireTime;
        private bool isReady = true;
        private bool isReloading = false;

        void Awake()
        {
            mainCamera = Camera.main;

            // Auto-find enemy if not assigned
            if (enemy == null)
            {
                enemy = GetComponentInParent<EnemyLogic>();
            }

            // Create crosshair UI if not assigned
            if (crosshairTransform == null)
            {
                CreateCrosshair();
            }
            else
            {
                crosshairImage = crosshairTransform.GetComponent<Image>();
                // Find the canvas if crosshair was manually assigned
                canvas = crosshairTransform.GetComponentInParent<Canvas>();
            }

            // Create reload indicator if not assigned
            if (reloadIndicatorTransform == null)
            {
                CreateReloadIndicator();
            }
            else
            {
                reloadIndicatorImage = reloadIndicatorTransform.GetComponent<Image>();
            }
        }

        void Start()
        {
            if (enemy == null)
            {
                Debug.LogWarning("TurningCrosshair: No EnemyLogic assigned!");
                enabled = false;
                return;
            }

            // Initialize lastFireTime to current time so crosshair starts in correct state
            lastFireTime = Time.time;
        }

        void Update()
        {
            if (enemy == null || crosshairTransform == null) return;

            UpdatePosition();
            
            // Check if enemy is reloading
            if (enemy.IsReloading)
            {
                UpdateReloadIndicator();
            }
            else
            {
                // Ensure reload indicator is hidden when not reloading
                if (isReloading)
                {
                    HideReloadIndicator();
                    isReloading = false;
                }
                
                UpdateRotation();
                UpdateColor();
            }
        }

        private void CreateCrosshair()
        {
            // Create canvas
            GameObject canvasObj = new GameObject("TurretCrosshairCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.zero;

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = worldSpace ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;

            if (worldSpace)
            {
                canvas.worldCamera = mainCamera;
                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(200f, 200f);
                canvasRect.localScale = Vector3.one * 0.02f; // Bigger scale for visibility
                
                Debug.Log($"TurningCrosshair: Created world-space canvas for {transform.parent?.name ?? "unknown"} at position {canvasObj.transform.position}");
            }

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;
            
            // Add GraphicRaycaster for UI interaction (optional but good practice)
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create crosshair image
            GameObject crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(canvasObj.transform);
            crosshairObj.transform.localPosition = Vector3.zero;
            crosshairObj.transform.localScale = Vector3.one;

            crosshairTransform = crosshairObj.AddComponent<RectTransform>();
            crosshairTransform.sizeDelta = new Vector2(crosshairSize * 2f, crosshairSize * 2f); // Make it bigger
            crosshairTransform.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairTransform.pivot = new Vector2(0.5f, 0.5f);

            crosshairImage = crosshairObj.AddComponent<Image>();
            
            // Create simple crosshair sprite (X shape)
            crosshairImage.sprite = CreateCrosshairSprite();
            crosshairImage.color = chargingColor;
        }

        private Sprite CreateCrosshairSprite()
        {
            // Create a simple X-shaped crosshair texture
            int size = 64;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            // Fill with transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            // Draw X shape
            int thickness = 4;
            for (int i = 0; i < size; i++)
            {
                for (int t = -thickness / 2; t < thickness / 2; t++)
                {
                    // Diagonal 1
                    int x1 = i;
                    int y1 = i + t;
                    if (y1 >= 0 && y1 < size)
                        pixels[y1 * size + x1] = Color.white;

                    // Diagonal 2
                    int x2 = i;
                    int y2 = (size - 1 - i) + t;
                    if (y2 >= 0 && y2 < size)
                        pixels[y2 * size + x2] = Color.white;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void CreateReloadIndicator()
        {
            if (canvas == null) return;

            // Create reload indicator image (circle that fills up)
            GameObject reloadObj = new GameObject("ReloadIndicator");
            reloadObj.transform.SetParent(canvas.transform);
            reloadObj.transform.localPosition = Vector3.zero;
            reloadObj.transform.localScale = Vector3.one;

            reloadIndicatorTransform = reloadObj.AddComponent<RectTransform>();
            reloadIndicatorTransform.sizeDelta = new Vector2(crosshairSize, crosshairSize);
            reloadIndicatorTransform.anchorMin = new Vector2(0.5f, 0.5f);
            reloadIndicatorTransform.anchorMax = new Vector2(0.5f, 0.5f);
            reloadIndicatorTransform.pivot = new Vector2(0.5f, 0.5f);

            reloadIndicatorImage = reloadObj.AddComponent<Image>();
            reloadIndicatorImage.sprite = CreateCircleSprite();
            reloadIndicatorImage.type = Image.Type.Filled;
            reloadIndicatorImage.fillMethod = Image.FillMethod.Radial360;
            reloadIndicatorImage.fillOrigin = (int)Image.Origin360.Top;
            reloadIndicatorImage.fillClockwise = true;
            reloadIndicatorImage.fillAmount = 0f;
            reloadIndicatorImage.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange color

            reloadObj.SetActive(false);
        }

        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            int size = 64;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float dist = Vector2.Distance(pos, center);
                    
                    // Create circle with smooth edge
                    if (dist <= radius - 2)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else if (dist <= radius)
                    {
                        float alpha = 1f - (dist - (radius - 2)) / 2f;
                        pixels[y * size + x] = new Color(1, 1, 1, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void UpdatePosition()
        {
            if (!worldSpace) return;

            // Position above enemy
            Vector3 worldPos = enemy.transform.position + offset;
            canvas.transform.position = worldPos;

            // Face camera
            if (mainCamera != null)
            {
                canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - mainCamera.transform.position);
            }
        }

        private void UpdateRotation()
        {
            if (isReady)
            {
                // Lock rotation when ready
                crosshairTransform.localRotation = Quaternion.identity;
            }
            else
            {
                // Spin while charging
                float rotation = crosshairTransform.localEulerAngles.z + spinSpeed * Time.deltaTime;
                crosshairTransform.localRotation = Quaternion.Euler(0, 0, rotation);
            }
        }

        private void UpdateColor()
        {
            if (crosshairImage == null || enemy == null || enemy.Config == null) return;

            // Get enemy's actual fire timing
            float enemyLastFireTime = enemy.LastFireTime;
            float fireInterval = enemy.FireInterval; // Uses overrides if set

            // Calculate time since enemy last fired
            float timeSinceFire = Time.time - enemyLastFireTime;

            if (timeSinceFire >= fireInterval)
            {
                // Ready to fire
                isReady = true;
                crosshairImage.color = readyColor;
            }
            else
            {
                // Charging
                isReady = false;
                float chargePercent = timeSinceFire / fireInterval;
                crosshairImage.color = Color.Lerp(firingColor, chargingColor, chargePercent);
            }
        }

        private void UpdateReloadIndicator()
        {
            if (reloadIndicatorImage == null || enemy == null) return;

            // Show reload indicator, hide crosshair (only once)
            if (!isReloading)
            {
                isReloading = true;
                ShowReloadIndicator();
            }

            // Update fill amount based on reload progress
            float progress = enemy.ReloadProgress;
            reloadIndicatorImage.fillAmount = progress;
        }

        /// <summary>
        /// Call this when enemy fires to reset the indicator
        /// </summary>
        public void OnTurretFired()
        {
            lastFireTime = Time.time;
            isReady = false;
        }

        /// <summary>
        /// Manual update for last fire time (if enemy doesn't call OnTurretFired)
        /// </summary>
        public void SetLastFireTime(float time)
        {
            lastFireTime = time;
        }

        /// <summary>
        /// Called when enemy starts reloading
        /// </summary>
        public void OnReloadStart()
        {
            isReloading = true;
            ShowReloadIndicator();
        }

        /// <summary>
        /// Called when enemy finishes reloading
        /// </summary>
        public void OnReloadComplete()
        {
            isReloading = false;
            HideReloadIndicator();
        }

        private void ShowReloadIndicator()
        {
            if (reloadIndicatorTransform != null)
            {
                reloadIndicatorTransform.gameObject.SetActive(true);
            }
            if (crosshairTransform != null)
            {
                crosshairTransform.gameObject.SetActive(false);
            }
        }

        private void HideReloadIndicator()
        {
            if (reloadIndicatorTransform != null)
            {
                reloadIndicatorTransform.gameObject.SetActive(false);
                // Reset fill amount
                if (reloadIndicatorImage != null)
                {
                    reloadIndicatorImage.fillAmount = 0f;
                }
            }
            if (crosshairTransform != null)
            {
                crosshairTransform.gameObject.SetActive(true);
            }
        }

        void OnDrawGizmos()
        {
            if (!debugDraw || enemy == null) return;

            // Draw debug sphere at crosshair position
            Vector3 worldPos = enemy.transform.position + offset;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(worldPos, 0.5f);
            
            // Draw line from enemy to crosshair position
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(enemy.transform.position, worldPos);
        }
    }
}
