using UnityEngine;
using UnityEngine.UI;
using Warspite.Core;

namespace Warspite.UI
{
    /// <summary>
    /// Robust world-space health bar that is parented under a global world-space canvas
    /// and follows a target Transform. Uses Image.fillAmount and billboards to camera.
    /// </summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 2f, 0);
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private bool hideWhenDead = true;
        [SerializeField] private float fadeSpeed = 8f;
        [SerializeField] private float width = 2.0f;   // in world units
        [SerializeField] private float height = 0.25f; // in world units

        [Header("Colors")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private float criticalThreshold = 0.25f;
        [SerializeField] private float damagedThreshold = 0.6f;

        private Camera mainCam;
        private RectTransform rectTransform;

        public static GameObject CreateFor(Health health, Vector3 offset, bool hideWhenFull, bool hideWhenDead)
        {
            var worldCanvas = WorldSpaceCanvas.GetOrCreate();

            var go = new GameObject($"HealthBar_{health.gameObject.name}");
            go.transform.SetParent(worldCanvas.transform, false);

            // Canvas size in world units maps 1:1 to RectTransform when using WorldSpace Canvas
            var rect = go.AddComponent<RectTransform>();

            var bar = go.AddComponent<WorldHealthBar>();
            bar.health = health;
            bar.followTarget = health.transform;
            bar.worldOffset = offset;
            bar.hideWhenFull = hideWhenFull;
            bar.hideWhenDead = hideWhenDead;
            // Set default size via serialized fields and apply to RectTransform
            bar.width = 2f;
            bar.height = 0.3f;
            rect.sizeDelta = new Vector2(bar.width, bar.height);

            // Background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(go.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            bgImg.sprite = CreateWhiteSprite();

            // Fill
            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = CreateWhiteSprite();
            fillImg.color = Color.green;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;

            // CanvasGroup for fading
            var group = go.AddComponent<CanvasGroup>();

            bar.fillImage = fillImg;
            bar.backgroundImage = bgImg;
            bar.canvasGroup = group;

            return go;
        }

        private void OnValidate()
        {
            // Keep RectTransform size in sync when edited in Inspector
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(width, height);
            }
        }

        private void Awake()
        {
            mainCam = Camera.main;
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(width, height);
            }

            if (health != null)
            {
                health.OnDamaged.AddListener(_ => UpdateVisual());
                health.OnHealed.AddListener(_ => UpdateVisual());
                health.OnDeath.AddListener(UpdateVisual);
            }
        }

        private void LateUpdate()
        {
            if (followTarget == null || mainCam == null) return;

            // Follow and face camera
            transform.position = followTarget.position + worldOffset;
            transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);

            UpdateVisual();
            UpdateVisibility();
        }

        private void UpdateVisual()
        {
            if (health == null || fillImage == null) return;
            float pct = Mathf.Clamp01(health.CurrentHealth / Mathf.Max(0.0001f, health.MaxHealth));
            fillImage.fillAmount = pct;

            if (pct <= criticalThreshold) fillImage.color = criticalColor;
            else if (pct <= damagedThreshold) fillImage.color = damagedColor;
            else fillImage.color = healthyColor;
        }

        private void UpdateVisibility()
        {
            if (canvasGroup == null || health == null) return;
            float target = 1f;
            if (hideWhenFull && health.CurrentHealth >= health.MaxHealth - 0.01f) target = 0f;
            if (hideWhenDead && health.IsDead) target = 0f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, target, Time.unscaledDeltaTime * fadeSpeed);
        }

        private static Sprite CreateWhiteSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
    }
}
