using UnityEngine;

namespace Warspite.UI
{
    public class DebugHUD : MonoBehaviour
    {
        public Warspite.TimeSystem.TimeDilationController timeController;
        public Warspite.Movement.MomentumLocomotion momentum;

        void OnGUI()
        {
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            float y = 10f;
            if (timeController)
            {
                GUI.Label(new Rect(10, y, 600, 25), $"Time Level: {timeController.CurrentIndex}  Scale: {timeController.CurrentScale:F2}", style);
                y += 22;
            }
            if (momentum)
            {
                GUI.Label(new Rect(10, y, 600, 25), $"Speed: {momentum.CurrentSpeed:F2} m/s", style);
                y += 28;
            }

            // Minimal control hints (fallback keys)
            GUI.Label(new Rect(10, y, 600, 22), "Controls:", style);
            y += 20;
            GUI.Label(new Rect(10, y, 800, 22), "- Time: Q / E", style);
            y += 20;
            GUI.Label(new Rect(10, y, 800, 22), "- Catch (L3 only): Hold RMB near bullet", style);
            y += 20;
            GUI.Label(new Rect(10, y, 800, 22), "- Throw: Release RMB or press F", style);
            y += 20;
            GUI.Label(new Rect(10, y, 800, 22), "- Restart: R", style);
            }
        }
    }
}
