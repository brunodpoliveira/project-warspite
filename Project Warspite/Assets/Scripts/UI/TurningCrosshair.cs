using UnityEngine;
using UnityEngine.UI;
using Warspite.World;

namespace Warspite.UI
{
    /// <summary>
    /// Crosshair that rotates to indicate turret firing cadence.
    /// Spins after turret fires, locks when ready to fire again.
    /// Provides visual feedback for turret timing.
    /// </summary>
    public class TurningCrosshair : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SimpleTurret turret;
        [SerializeField] private RectTransform crosshairTransform;

        [Header("Visual Settings")]
        [SerializeField] private float spinSpeed = 360f; // Degrees per second
        [SerializeField] private Color readyColor = Color.green;
        [SerializeField] private Color chargingColor = Color.yellow;
        [SerializeField] private Color firingColor = Color.red;
        [SerializeField] private float crosshairSize = 50f;

        [Header("Position")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
        [SerializeField] private bool worldSpace = true;

        private Image crosshairImage;
        private Canvas canvas;
        private Camera mainCamera;
        private float lastFireTime;
        private bool isReady = true;

        void Awake()
        {
            mainCamera = Camera.main;

            // Auto-find turret if not assigned
            if (turret == null)
            {
                turret = GetComponentInParent<SimpleTurret>();
            }

            // Create crosshair UI if not assigned
            if (crosshairTransform == null)
            {
                CreateCrosshair();
            }
            else
            {
                crosshairImage = crosshairTransform.GetComponent<Image>();
            }
        }

        void Start()
        {
            if (turret == null)
            {
                Debug.LogWarning("TurningCrosshair: No SimpleTurret assigned!");
                enabled = false;
                return;
            }

            // Initialize lastFireTime to current time so crosshair starts in correct state
            lastFireTime = Time.time;
        }

        void Update()
        {
            if (turret == null || crosshairTransform == null) return;

            UpdatePosition();
            UpdateRotation();
            UpdateColor();
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
                canvasRect.sizeDelta = new Vector2(100f, 100f);
                canvasRect.localScale = Vector3.one * 0.01f; // Scale down for world space
            }

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // Create crosshair image
            GameObject crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(canvasObj.transform);
            crosshairObj.transform.localPosition = Vector3.zero;
            crosshairObj.transform.localScale = Vector3.one;

            crosshairTransform = crosshairObj.AddComponent<RectTransform>();
            crosshairTransform.sizeDelta = new Vector2(crosshairSize, crosshairSize);
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

        private void UpdatePosition()
        {
            if (!worldSpace) return;

            // Position above turret
            Vector3 worldPos = turret.transform.position + offset;
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
            if (crosshairImage == null || turret == null) return;

            // Get turret's actual fire timing
            float turretLastFireTime = turret.LastFireTime;
            float fireInterval = turret.Interval;

            // Calculate time since turret last fired
            float timeSinceFire = Time.time - turretLastFireTime;

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

        /// <summary>
        /// Call this when turret fires to reset the indicator
        /// </summary>
        public void OnTurretFired()
        {
            lastFireTime = Time.time;
            isReady = false;
        }

        /// <summary>
        /// Manual update for last fire time (if turret doesn't call OnTurretFired)
        /// </summary>
        public void SetLastFireTime(float time)
        {
            lastFireTime = time;
        }
    }
}
