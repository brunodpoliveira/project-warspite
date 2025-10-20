using UnityEngine;

namespace Warspite.Player
{
    /// <summary>
    /// Simple expanding ring shockwave effect.
    /// Can be spawned by SonicBoom or other systems.
    /// Uses procedural mesh generation for a placeholder visual.
    /// </summary>
    public class ShockwaveEffect : MonoBehaviour
    {
        [Header("Shockwave Settings")]
        [SerializeField] private float expandSpeed = 10f;
        [SerializeField] private float maxRadius = 5f;
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private AnimationCurve opacityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Visual")]
        [SerializeField] private Color shockwaveColor = new Color(0.5f, 0.8f, 1f, 0.8f); // Cyan
        [SerializeField] private int segments = 32;
        [SerializeField] private float thickness = 0.2f;

        private float currentRadius = 0f;
        private float spawnTime;
        private LineRenderer lineRenderer;

        void Start()
        {
            spawnTime = Time.time;
            SetupLineRenderer();
        }

        void Update()
        {
            float age = Time.time - spawnTime;
            float progress = age / lifetime;

            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Expand radius
            currentRadius = Mathf.Lerp(0, maxRadius, progress);

            // Update visual
            UpdateShockwaveVisual(progress);
        }

        private void SetupLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = thickness;
            lineRenderer.endWidth = thickness;
            lineRenderer.positionCount = segments;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = shockwaveColor;
            lineRenderer.endColor = shockwaveColor;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        private void UpdateShockwaveVisual(float progress)
        {
            if (lineRenderer == null) return;

            // Update ring positions
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    0,
                    Mathf.Sin(angle) * currentRadius
                );
                lineRenderer.SetPosition(i, position);
            }

            // Update opacity
            float opacity = opacityCurve.Evaluate(progress);
            Color color = shockwaveColor;
            color.a = opacity;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        /// <summary>
        /// Static helper to spawn a shockwave at a position
        /// </summary>
        public static void SpawnShockwave(Vector3 position, Color? color = null, float radius = 5f)
        {
            GameObject shockwaveObj = new GameObject("Shockwave");
            shockwaveObj.transform.position = position;
            
            ShockwaveEffect effect = shockwaveObj.AddComponent<ShockwaveEffect>();
            effect.maxRadius = radius;
            
            if (color.HasValue)
            {
                effect.shockwaveColor = color.Value;
            }
        }
    }
}
