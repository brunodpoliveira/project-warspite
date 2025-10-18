using UnityEngine;

namespace Warspite.UI
{
    /// <summary>
    /// Debug helper to visualize where health bars should appear.
    /// Attach this to an enemy to see a gizmo where the health bar will be positioned.
    /// </summary>
    public class HealthBarDebugger : MonoBehaviour
    {
        [Header("Health Bar Settings (match AutoHealthBar)")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
        [SerializeField] private Vector2 size = new Vector2(1f, 0.1f);
        [SerializeField] private Color gizmoColor = Color.yellow;

        void OnDrawGizmos()
        {
            // Draw where the health bar center will be
            Vector3 barPosition = transform.position + offset;
            
            // Draw a sphere at the center
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(barPosition, 0.1f);
            
            // Draw a line from enemy to health bar position
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, barPosition);
            
            // Draw the health bar bounds
            Vector3 halfSize = new Vector3(size.x * 0.5f, size.y * 0.5f, 0.01f);
            
            // Get camera direction for proper orientation
            Camera cam = Camera.main;
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            Vector3 up = Vector3.up;
            
            if (cam != null)
            {
                forward = (barPosition - cam.transform.position).normalized;
                right = Vector3.Cross(Vector3.up, forward).normalized;
                up = Vector3.Cross(forward, right).normalized;
            }
            
            // Draw health bar rectangle
            Vector3 topLeft = barPosition + (-right * halfSize.x) + (up * halfSize.y);
            Vector3 topRight = barPosition + (right * halfSize.x) + (up * halfSize.y);
            Vector3 bottomLeft = barPosition + (-right * halfSize.x) + (-up * halfSize.y);
            Vector3 bottomRight = barPosition + (right * halfSize.x) + (-up * halfSize.y);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
            
            // Draw diagonal cross
            Gizmos.DrawLine(topLeft, bottomRight);
            Gizmos.DrawLine(topRight, bottomLeft);
        }

        void OnDrawGizmosSelected()
        {
            // Draw more detailed info when selected
            Vector3 barPosition = transform.position + offset;
            
            // Draw text info (only visible in Scene view)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(barPosition + Vector3.up * 0.3f, 
                $"Health Bar Position\nOffset: {offset}\nSize: {size}");
            #endif
        }
    }
}
