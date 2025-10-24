using UnityEngine;
using System.Collections.Generic;

namespace Warspite.Player
{
    /// <summary>
    /// Makes objects between the camera and player semi-transparent to maintain visibility.
    /// Raycasts from camera to player and fades any occluding objects.
    /// </summary>
    public class CameraOcclusion : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Camera playerCamera;

        [Header("Occlusion Settings")]
        [SerializeField] private float targetAlpha = 0.3f; // Target transparency for occluding objects
        [SerializeField] private float fadeSpeed = 10f; // Speed of fade in/out
        [SerializeField] private LayerMask occlusionLayers = -1; // Layers that can occlude

        [Header("Debug")]
        [SerializeField] private bool showDebugRay = true;
        [SerializeField] private Color debugRayColor = Color.yellow;

        // Track materials that are currently faded
        private class MaterialData
        {
            public Material material;
            public Color originalColor;
            public float originalAlpha;
            public float currentAlpha;
            public bool wasTransparent;
            public int renderQueue;
            public bool isRestoring; // Track if this material is being restored
        }

        private Dictionary<Renderer, List<MaterialData>> occludedRenderers = new Dictionary<Renderer, List<MaterialData>>();
        private HashSet<Renderer> currentlyOccluding = new HashSet<Renderer>();

        void Start()
        {
            // Auto-find references if not assigned
            if (playerCamera == null)
            {
                // Try to get camera from this GameObject first
                playerCamera = GetComponent<Camera>();
                
                // Fall back to Camera.main
                if (playerCamera == null)
                {
                    playerCamera = Camera.main;
                }
            }

            if (player == null)
            {
                // Try to find player by tag
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
                else
                {
                    Debug.LogWarning("CameraOcclusion: No player assigned and couldn't find GameObject with 'Player' tag!");
                }
            }

            if (playerCamera == null)
            {
                Debug.LogError("CameraOcclusion: No camera found!");
                enabled = false;
                return;
            }

            if (player == null)
            {
                Debug.LogError("CameraOcclusion: No player target found!");
                enabled = false;
                return;
            }

            Debug.Log($"CameraOcclusion initialized: Camera={playerCamera.name}, Player={player.name}");
        }

        void LateUpdate()
        {
            if (player == null || playerCamera == null) return;

            // Clear current frame's occluding set
            currentlyOccluding.Clear();

            // Raycast from camera to player
            Vector3 cameraPos = playerCamera.transform.position;
            Vector3 playerPos = player.position;
            Vector3 direction = playerPos - cameraPos;
            float distance = direction.magnitude;

            // Perform raycast to find all objects between camera and player
            RaycastHit[] hits = Physics.RaycastAll(cameraPos, direction.normalized, distance, occlusionLayers);

            // Debug visualization
            if (showDebugRay)
            {
                Debug.DrawRay(cameraPos, direction, debugRayColor);
            }

            // Process all hit objects
            int occludingCount = 0;
            foreach (RaycastHit hit in hits)
            {
                // Skip the player itself
                if (hit.transform == player || hit.transform.IsChildOf(player))
                    continue;

                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null)
                {
                    currentlyOccluding.Add(renderer);
                    FadeRenderer(renderer);
                    occludingCount++;
                }
            }

            // Debug output (only when objects are occluding) - commented out to reduce spam
            // if (showDebugRay && occludingCount > 0)
            // {
            //     Debug.Log($"CameraOcclusion: {occludingCount} objects occluding view");
            // }

            // Mark renderers for restoration if they're no longer occluding
            foreach (var kvp in occludedRenderers)
            {
                if (!currentlyOccluding.Contains(kvp.Key))
                {
                    // Mark all materials as restoring
                    foreach (MaterialData matData in kvp.Value)
                    {
                        matData.isRestoring = true;
                    }
                }
            }

            // Process restoration and remove fully restored renderers
            List<Renderer> toRemove = new List<Renderer>();
            foreach (var kvp in occludedRenderers)
            {
                bool fullyRestored = UpdateRestoration(kvp.Key, kvp.Value);
                if (fullyRestored)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            // Clean up fully restored renderers
            foreach (Renderer renderer in toRemove)
            {
                occludedRenderers.Remove(renderer);
            }
        }

        private void FadeRenderer(Renderer renderer)
        {
            // Initialize material data if this is a new occluding object
            if (!occludedRenderers.ContainsKey(renderer))
            {
                List<MaterialData> materialDataList = new List<MaterialData>();

                // Create material instances (not shared materials)
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material mat = materials[i];
                    
                    MaterialData data = new MaterialData
                    {
                        material = mat,
                        originalColor = mat.color,
                        originalAlpha = mat.color.a,
                        currentAlpha = mat.color.a,
                        wasTransparent = mat.renderQueue >= 3000,
                        renderQueue = mat.renderQueue,
                        isRestoring = false
                    };

                    // Set up material for transparency if it wasn't already
                    if (!data.wasTransparent)
                    {
                        SetupTransparentMaterial(mat);
                    }

                    materialDataList.Add(data);
                }

                occludedRenderers[renderer] = materialDataList;
            }

            // Fade materials toward target alpha
            List<MaterialData> matDataList = occludedRenderers[renderer];
            bool firstMaterial = true;
            foreach (MaterialData matData in matDataList)
            {
                // Smoothly interpolate alpha
                float previousAlpha = matData.currentAlpha;
                matData.currentAlpha = Mathf.Lerp(matData.currentAlpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);

                // Apply alpha to material
                Color color = matData.material.color;
                color.a = matData.currentAlpha;
                matData.material.color = color;

                // Debug log first frame of fading (commented out to reduce spam)
                // if (firstMaterial && Mathf.Abs(previousAlpha - matData.originalAlpha) < 0.01f)
                // {
                //     Debug.Log($"Starting fade on {renderer.name}: {matData.material.name} alpha {matData.originalAlpha} -> {targetAlpha}");
                //     firstMaterial = false;
                // }

                // Also fade emission if present
                if (matData.material.HasProperty("_EmissionColor"))
                {
                    Color emission = matData.material.GetColor("_EmissionColor");
                    emission.a = matData.currentAlpha;
                    matData.material.SetColor("_EmissionColor", emission);
                }
            }
        }

        private void SetupTransparentMaterial(Material mat)
        {
            // URP Lit shader uses _Surface property (0 = Opaque, 1 = Transparent)
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // 1 = Transparent
                // Debug.Log($"SetupTransparentMaterial: URP Lit shader detected, setting _Surface=1");
            }
            // Standard shader uses _Mode property
            else if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3); // 3 = Transparent mode
                // Debug.Log($"SetupTransparentMaterial: Standard shader detected, setting _Mode=3");
            }
            
            // URP blend mode (0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply)
            if (mat.HasProperty("_Blend"))
            {
                mat.SetFloat("_Blend", 0); // 0 = Alpha blend
            }
            
            // Set blend mode for transparency
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            
            // Enable transparency keywords for both Standard and URP
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            
            // Set render queue to transparent
            mat.renderQueue = 3000;
            
            // Debug.Log($"SetupTransparentMaterial: {mat.name} - renderQueue={mat.renderQueue}, shader={mat.shader.name}");
        }

        private bool UpdateRestoration(Renderer renderer, List<MaterialData> matDataList)
        {
            bool allFullyRestored = true;

            foreach (MaterialData matData in matDataList)
            {
                // Skip if not restoring
                if (!matData.isRestoring)
                {
                    allFullyRestored = false;
                    continue;
                }

                // Smoothly fade back to original alpha
                matData.currentAlpha = Mathf.Lerp(matData.currentAlpha, matData.originalAlpha, fadeSpeed * Time.unscaledDeltaTime);

                // Check if we're close enough to original alpha to fully restore
                if (Mathf.Abs(matData.currentAlpha - matData.originalAlpha) < 0.01f)
                {
                    // Fully restore material
                    Color color = matData.originalColor;
                    color.a = matData.originalAlpha;
                    matData.material.color = color;

                    // Restore render mode if it wasn't originally transparent
                    if (!matData.wasTransparent)
                    {
                        // Restore URP Lit shader
                        if (matData.material.HasProperty("_Surface"))
                        {
                            matData.material.SetFloat("_Surface", 0); // 0 = Opaque
                        }
                        // Restore Standard shader
                        else if (matData.material.HasProperty("_Mode"))
                        {
                            matData.material.SetFloat("_Mode", 0); // 0 = Opaque
                        }
                        
                        matData.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        matData.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        matData.material.SetInt("_ZWrite", 1);
                        matData.material.DisableKeyword("_ALPHATEST_ON");
                        matData.material.DisableKeyword("_ALPHABLEND_ON");
                        matData.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        matData.material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        matData.material.renderQueue = matData.renderQueue;
                    }

                    Debug.Log($"Fully restored {renderer.name}: {matData.material.name}");
                }
                else
                {
                    // Still fading, apply current alpha
                    Color color = matData.material.color;
                    color.a = matData.currentAlpha;
                    matData.material.color = color;
                    allFullyRestored = false;
                }
            }

            return allFullyRestored;
        }

        void OnDestroy()
        {
            // Restore all materials when component is destroyed
            foreach (var kvp in occludedRenderers)
            {
                List<MaterialData> matDataList = kvp.Value;
                foreach (MaterialData matData in matDataList)
                {
                    matData.material.color = matData.originalColor;

                    if (!matData.wasTransparent)
                    {
                        // Restore URP Lit shader
                        if (matData.material.HasProperty("_Surface"))
                        {
                            matData.material.SetFloat("_Surface", 0);
                        }
                        // Restore Standard shader
                        else if (matData.material.HasProperty("_Mode"))
                        {
                            matData.material.SetFloat("_Mode", 0);
                        }
                        
                        matData.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        matData.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        matData.material.SetInt("_ZWrite", 1);
                        matData.material.DisableKeyword("_ALPHATEST_ON");
                        matData.material.DisableKeyword("_ALPHABLEND_ON");
                        matData.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        matData.material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        matData.material.renderQueue = matData.renderQueue;
                    }
                }
            }
            occludedRenderers.Clear();
        }
    }
}
