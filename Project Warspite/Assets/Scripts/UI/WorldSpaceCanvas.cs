using UnityEngine;
using UnityEngine.UI;

namespace Warspite.UI
{
    /// <summary>
    /// Ensures there is a single world-space canvas in the scene for 3D UI like health bars.
    /// Avoids inheriting arbitrary scaling from enemies by keeping bars under a top-level canvas.
    /// </summary>
    public static class WorldSpaceCanvas
    {
        private const string CanvasName = "WorldSpaceUI";
        private static Canvas cached;

        public static Canvas GetOrCreate()
        {
            if (cached != null) return cached;

            var existing = GameObject.Find(CanvasName);
            if (existing != null)
            {
                cached = existing.GetComponent<Canvas>();
                if (cached != null) return cached;
            }

            var go = new GameObject(CanvasName);
            cached = go.AddComponent<Canvas>();
            cached.renderMode = RenderMode.WorldSpace;
            cached.worldCamera = Camera.main;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            go.AddComponent<GraphicRaycaster>();

            // Reasonable default size; bars will position themselves in world space.
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);

            return cached;
        }
    }
}
