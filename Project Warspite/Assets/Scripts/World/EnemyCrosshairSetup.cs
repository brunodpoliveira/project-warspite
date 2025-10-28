using UnityEngine;
using Warspite.UI;

namespace Warspite.World
{
    /// <summary>
    /// Helper component to automatically create and setup TurningCrosshair for enemies.
    /// Add this to enemy prefab and it will auto-create the crosshair UI.
    /// </summary>
    [RequireComponent(typeof(EnemyLogic))]
    public class EnemyCrosshairSetup : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        [SerializeField] private bool autoCreateCrosshair = true;
        [SerializeField] private Vector3 crosshairOffset = new Vector3(0, 2f, 0); // Above enemy head
        [SerializeField] private float crosshairSize = 50f;
        [SerializeField] private Color readyColor = Color.green;
        [SerializeField] private Color chargingColor = Color.yellow;
        [SerializeField] private Color firingColor = Color.red;

        private TurningCrosshair crosshair;
        private EnemyLogic enemyLogic;

        void Awake()
        {
            enemyLogic = GetComponent<EnemyLogic>();

            if (autoCreateCrosshair)
            {
                CreateCrosshair();
            }
        }

        private void CreateCrosshair()
        {
            // Check if crosshair already exists
            crosshair = GetComponentInChildren<TurningCrosshair>();
            if (crosshair != null)
            {
                Debug.Log($"TurningCrosshair already exists on {gameObject.name}");
                return;
            }

            // Create crosshair GameObject
            GameObject crosshairObj = new GameObject("TurningCrosshair");
            crosshairObj.transform.SetParent(transform);
            crosshairObj.transform.localPosition = crosshairOffset;

            // Add TurningCrosshair component
            crosshair = crosshairObj.AddComponent<TurningCrosshair>();

            // The TurningCrosshair will auto-create its UI in Awake
            // We just need to configure it via reflection or wait for it to initialize

            Debug.Log($"Created TurningCrosshair for {gameObject.name}");
        }

        // Optional: Expose crosshair for manual configuration
        public TurningCrosshair GetCrosshair() => crosshair;
    }
}
