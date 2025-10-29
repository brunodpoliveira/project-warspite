using UnityEngine;
using Warspite.Core;

namespace Warspite.World
{
    /// <summary>
    /// Grenade projectile with timed explosion, blast damage, and shrapnel.
    /// Can be caught and thrown back by player in L3.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Grenade : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [SerializeField] private float timer = 3f;
        [SerializeField] private float blastDamage = 80f;
        [SerializeField] private float shrapnelDamage = 40f;
        [SerializeField] private float blastRadius = 5f;
        [SerializeField] private bool debugExplosion = false; // Disabled to reduce console spam
        
        [Header("Visual Settings")]
        [SerializeField] private Color grenadeColor = Color.green;
        [SerializeField] private bool showDangerZone = true;
        [SerializeField] private Color dangerZoneColor = new Color(1f, 0f, 0f, 0.3f); // Red transparent
        [SerializeField] private bool showTimerUI = true;
        [SerializeField] private Color timerColor = new Color(1f, 0.5f, 0f, 1f); // Orange
        [SerializeField] private float timerUISize = 1f;
        [SerializeField] private Vector3 timerUIOffset = new Vector3(0, 1f, 0); // Above grenade
        
        private Rigidbody rb;
        private float spawnTime;
        private Renderer grenadeRenderer;
        private Material grenadeMaterial;
        private bool hasExploded = false;
        private GameObject dangerZoneVisual;
        private GameObject timerUI;
        private UnityEngine.UI.Image timerCircle;
        
        public bool IsCaught { get; set; }
        
        // Public setters for EnemyLogic
        public void SetExplosionStats(float blastDmg, float shrapnelDmg, float radius, float time)
        {
            blastDamage = blastDmg;
            shrapnelDamage = shrapnelDmg;
            blastRadius = radius;
            timer = time;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            grenadeRenderer = GetComponent<Renderer>();
            
            if (grenadeRenderer != null)
            {
                grenadeMaterial = grenadeRenderer.material;
                grenadeMaterial.color = grenadeColor;
            }
        }

        void Start()
        {
            spawnTime = Time.time;
            
            // Create danger zone visual
            if (showDangerZone)
            {
                CreateDangerZone();
            }
            
            // Create timer UI
            if (showTimerUI)
            {
                CreateTimerUI();
            }
            
            if (debugExplosion)
            {
                Debug.Log($"[Grenade] Spawned at {transform.position} | Timer: {timer}s | Blast: {blastDamage} | Shrapnel: {shrapnelDamage} | Radius: {blastRadius}m");
            }
        }

        void Update()
        {
            // Check if timer expired
            float timeRemaining = timer - (Time.time - spawnTime);
            
            if (timeRemaining <= 0 && !hasExploded)
            {
                Explode();
                return;
            }
            
            // Update timer UI
            if (timerUI != null && timerCircle != null)
            {
                // Position above grenade
                timerUI.transform.position = transform.position + timerUIOffset;
                
                // Face camera
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    timerUI.transform.rotation = Quaternion.LookRotation(timerUI.transform.position - mainCam.transform.position);
                }
                
                // Update fill amount (1 = full time, 0 = explode)
                float fillAmount = Mathf.Clamp01(timeRemaining / timer);
                timerCircle.fillAmount = fillAmount;
                
                // Color changes as time runs out (green -> yellow -> red)
                if (fillAmount > 0.66f)
                {
                    timerCircle.color = Color.Lerp(Color.yellow, Color.green, (fillAmount - 0.66f) / 0.34f);
                }
                else if (fillAmount > 0.33f)
                {
                    timerCircle.color = Color.Lerp(timerColor, Color.yellow, (fillAmount - 0.33f) / 0.33f);
                }
                else
                {
                    timerCircle.color = Color.Lerp(Color.red, timerColor, fillAmount / 0.33f);
                }
            }
            
            // Update danger zone position to follow grenade
            if (dangerZoneVisual != null)
            {
                // Position at ground level below grenade
                Vector3 groundPos = transform.position;
                groundPos.y = 0.1f; // Slightly above ground to avoid z-fighting
                dangerZoneVisual.transform.position = groundPos;
            }
        }

        public void Launch(Vector3 velocity)
        {
            rb.linearVelocity = velocity;
        }

        public Vector3 GetVelocity()
        {
            return rb.linearVelocity;
        }

        public void Freeze()
        {
            rb.isKinematic = true;
        }

        public void Unfreeze()
        {
            rb.isKinematic = false;
        }

        void OnCollisionEnter(Collision collision)
        {
            // Grenades bounce, don't explode on impact
            // They only explode when timer runs out
            
            if (debugExplosion)
            {
                Debug.Log($"[Grenade] Bounced off {collision.gameObject.name} at {transform.position}");
            }
            
            // Optional: Add bounce sound effect here
        }
        
        void OnDestroy()
        {
            if (!hasExploded && debugExplosion)
            {
                Debug.LogWarning($"[Grenade] Destroyed without exploding! Position: {transform.position} | Time alive: {Time.time - spawnTime:F2}s");
            }
            
            // Clean up visuals
            if (dangerZoneVisual != null)
            {
                Destroy(dangerZoneVisual);
            }
            
            if (timerUI != null)
            {
                Destroy(timerUI);
            }
        }
        
        private void CreateDangerZone()
        {
            // Create a flat cylinder to show blast radius on ground
            dangerZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dangerZoneVisual.name = "GrenadeBlastRadius";
            
            // Scale to match blast radius (cylinder is 2 units tall by default)
            float diameter = blastRadius * 2f;
            dangerZoneVisual.transform.localScale = new Vector3(diameter, 0.05f, diameter); // Very flat
            
            // Position at ground level
            Vector3 groundPos = transform.position;
            groundPos.y = 0.1f;
            dangerZoneVisual.transform.position = groundPos;
            
            // Remove collider (visual only)
            Collider dzCollider = dangerZoneVisual.GetComponent<Collider>();
            if (dzCollider != null)
            {
                Destroy(dzCollider);
            }
            
            // Setup transparent material
            Renderer dzRenderer = dangerZoneVisual.GetComponent<Renderer>();
            if (dzRenderer != null)
            {
                // Find appropriate shader
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                
                Material mat = new Material(shader);
                mat.color = dangerZoneColor;
                
                // Enable transparency
                if (mat.HasProperty("_Surface"))
                {
                    // URP
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0); // Alpha blend
                }
                else
                {
                    // Standard
                    mat.SetFloat("_Mode", 3); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                }
                
                dzRenderer.material = mat;
            }
        }
        
        private void CreateTimerUI()
        {
            // Create world-space canvas for timer
            timerUI = new GameObject("GrenadeTimer");
            timerUI.transform.position = transform.position + timerUIOffset;
            
            Canvas canvas = timerUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Scale canvas
            RectTransform canvasRect = timerUI.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100, 100);
            canvasRect.localScale = Vector3.one * timerUISize * 0.01f; // Scale down
            
            // Create circle image (background)
            GameObject circleObj = new GameObject("TimerCircle");
            circleObj.transform.SetParent(timerUI.transform, false);
            
            timerCircle = circleObj.AddComponent<UnityEngine.UI.Image>();
            timerCircle.type = UnityEngine.UI.Image.Type.Filled;
            timerCircle.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            timerCircle.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            timerCircle.fillClockwise = false; // Counter-clockwise (drains down)
            timerCircle.fillAmount = 1f; // Start full
            timerCircle.color = Color.green;
            
            // Create circle sprite (simple white circle)
            Texture2D circleTex = new Texture2D(128, 128);
            Color[] pixels = new Color[128 * 128];
            Vector2 center = new Vector2(64, 64);
            float radius = 60f;
            
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01((dist - radius + 5f) / 5f); // Smooth edge
                    pixels[y * 128 + x] = new Color(1, 1, 1, alpha);
                }
            }
            
            circleTex.SetPixels(pixels);
            circleTex.Apply();
            
            Sprite circleSprite = Sprite.Create(circleTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            timerCircle.sprite = circleSprite;
            
            // Size the circle
            RectTransform circleRect = circleObj.GetComponent<RectTransform>();
            circleRect.sizeDelta = new Vector2(80, 80);
            circleRect.anchoredPosition = Vector2.zero;
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;
            
            if (debugExplosion)
            {
                Debug.Log($"[Grenade] Exploded at {transform.position} | Blast: {blastDamage} | Shrapnel: {shrapnelDamage} | Radius: {blastRadius}m");
            }
            
            // Find all colliders in blast radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, blastRadius);
            
            foreach (Collider hitCollider in hitColliders)
            {
                Health health = hitCollider.GetComponentInParent<Health>();
                if (health != null && !health.IsDead)
                {
                    // Check if grenade was thrown by player (caught)
                    bool isPlayerGrenade = IsCaught;
                    
                    // Don't damage enemies if thrown by enemy (friendly fire prevention)
                    EnemyLogic enemy = hitCollider.GetComponentInParent<EnemyLogic>();
                    if (!isPlayerGrenade && enemy != null)
                    {
                        continue; // Skip enemy damage from enemy grenades
                    }
                    
                    // Calculate distance from explosion center
                    float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                    
                    // Calculate damage falloff based on distance
                    // Full damage at center, reduced at edges
                    float distanceRatio = 1f - (distance / blastRadius);
                    distanceRatio = Mathf.Clamp01(distanceRatio);
                    
                    // Blast damage (full at center, 0 at edge)
                    float finalBlastDamage = blastDamage * distanceRatio;
                    
                    // Shrapnel damage (more consistent, 50% at edge)
                    float finalShrapnelDamage = shrapnelDamage * Mathf.Lerp(0.5f, 1f, distanceRatio);
                    
                    float totalDamage = finalBlastDamage + finalShrapnelDamage;
                    
                    if (debugExplosion)
                    {
                        Debug.Log($"[Grenade] Hit {hitCollider.name} | Distance: {distance:F1}m | Blast: {finalBlastDamage:F1} | Shrapnel: {finalShrapnelDamage:F1} | Total: {totalDamage:F1}");
                    }
                    
                    health.TakeDamage(totalDamage);
                    
                    // Optional: Apply knockback force
                    Rigidbody targetRb = hitCollider.GetComponent<Rigidbody>();
                    if (targetRb != null)
                    {
                        Vector3 explosionDir = (hitCollider.transform.position - transform.position).normalized;
                        float force = 500f * distanceRatio;
                        targetRb.AddForce(explosionDir * force, ForceMode.Impulse);
                    }
                }
            }
            
            // TODO: Spawn explosion VFX
            // TODO: Play explosion sound
            
            // Destroy grenade
            Destroy(gameObject);
        }

        // Draw blast radius in editor
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, blastRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, blastRadius);
        }
    }
}
