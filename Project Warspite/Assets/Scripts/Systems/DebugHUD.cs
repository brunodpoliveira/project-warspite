using UnityEngine;
using Warspite.Player;
using Warspite.Core;

namespace Warspite.Systems
{
    /// <summary>
    /// Displays debug information: time level, player velocity, FPS.
    /// Attach to any always-active object (camera or GameSystems).
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        [Header("References (optional)")]
        [SerializeField] private TimeDilationController timeController;
        [SerializeField] private TimeDilationResource timeResource;
        [SerializeField] private MomentumLocomotion momentum;
        [SerializeField] private Health playerHealth;

        [Header("Display")]
        [SerializeField] private bool showHUD = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private float fpsUpdateInterval = 0.5f;
        private float fpsAccumulator = 0f;
        private int fpsFrames = 0;
        private float currentFPS = 0f;
        private float lastFPSUpdate = 0f;

        void Update()
        {
            UpdateFPS();

            if (Input.GetKeyDown(toggleKey))
            {
                showHUD = !showHUD;
            }
        }

        void OnGUI()
        {
            if (!showHUD) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;

            // Background
            GUI.Box(new Rect(10, 10, 300, 200), "");

            int yOffset = 20;

            // Player Health
            if (playerHealth != null)
            {
                string healthText = $"Health: {playerHealth.CurrentHealth:F0}/{playerHealth.MaxHealth:F0}";
                GUI.Label(new Rect(20, yOffset, 280, 25), healthText, style);
                yOffset += 25;
            }

            // Time Resource
            if (timeResource != null)
            {
                string resourceText = $"Time Energy: {timeResource.CurrentResource:F0}/{timeResource.MaxResource:F0}";
                GUI.Label(new Rect(20, yOffset, 280, 25), resourceText, style);
                yOffset += 25;
            }

            // Time Level
            if (timeController != null)
            {
                string[] levelNames = { "Normal", "Slow L1", "Slow L2", "Near-Freeze L3" };
                string timeLevelText = $"Time: {levelNames[timeController.CurrentLevel]} ({timeController.CurrentTimeScale:F2}x)";
                GUI.Label(new Rect(20, yOffset, 280, 25), timeLevelText, style);
                yOffset += 25;
            }

            // Player Velocity
            if (momentum != null)
            {
                Vector3 vel = momentum.Velocity;
                float horizontalSpeed = new Vector3(vel.x, 0, vel.z).magnitude;
                string velText = $"Speed: {horizontalSpeed:F1} m/s";
                GUI.Label(new Rect(20, yOffset, 280, 25), velText, style);
                yOffset += 25;

                string groundedText = $"Grounded: {momentum.IsGrounded}";
                GUI.Label(new Rect(20, yOffset, 280, 25), groundedText, style);
                yOffset += 25;
            }

            // FPS
            string fpsText = $"FPS: {currentFPS:F0}";
            GUI.Label(new Rect(20, yOffset, 280, 25), fpsText, style);
            yOffset += 25;

            // Controls hint
            style.fontSize = 12;
            style.fontStyle = FontStyle.Normal;
            GUI.Label(new Rect(20, yOffset, 280, 20), "Q/E: Time | F: Suck | ESC: Cursor | F1: HUD", style);
        }

        private void UpdateFPS()
        {
            fpsAccumulator += Time.unscaledDeltaTime;
            fpsFrames++;

            if (Time.unscaledTime - lastFPSUpdate > fpsUpdateInterval)
            {
                currentFPS = fpsFrames / fpsAccumulator;
                fpsAccumulator = 0f;
                fpsFrames = 0;
                lastFPSUpdate = Time.unscaledTime;
            }
        }
    }
}
