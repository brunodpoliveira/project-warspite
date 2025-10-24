using UnityEngine;

namespace Warspite.Player
{
    /// <summary>
    /// Third-person orbit camera with mouse look.
    /// Always looks at target, maintains distance, and smoothly follows.
    /// Uses real-time delta for consistent responsiveness regardless of timeScale.
    /// </summary>
    public class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Orbit Settings")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private float height = 2f;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 3f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 60f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothing = 5f;
        [SerializeField] private float rotationSmoothing = 10f;

        private float currentYaw;
        private float currentPitch;
        private Vector3 currentPosition;

        void Start()
        {
            if (target == null)
            {
                Debug.LogError("ThirdPersonOrbitCamera: No target assigned!");
                enabled = false;
                return;
            }

            // Initialize camera behind target
            currentYaw = target.eulerAngles.y;
            currentPitch = 20f;
            currentPosition = CalculateDesiredPosition();
            transform.position = currentPosition;

            // Set up camera occlusion if not already present
            SetupCameraOcclusion();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetupCameraOcclusion()
        {
            // Check if CameraOcclusion component exists
            CameraOcclusion occlusion = GetComponent<CameraOcclusion>();
            if (occlusion == null)
            {
                // Add the component
                occlusion = gameObject.AddComponent<CameraOcclusion>();
                Debug.Log("CameraOcclusion component added automatically to camera.");
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            HandleMouseInput();
            UpdateCameraPosition();
            HandleCursorToggle();
        }

        private void HandleMouseInput()
        {
            // Use unscaled delta time for consistent mouse feel
            float realDelta = Time.unscaledDeltaTime;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            currentYaw += mouseX;
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            // Mouse wheel zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance = Mathf.Clamp(distance - scroll * 2f, minDistance, maxDistance);
            }
        }

        private void UpdateCameraPosition()
        {
            // Calculate desired position
            Vector3 desiredPosition = CalculateDesiredPosition();

            // Smooth position
            float realDelta = Time.unscaledDeltaTime;
            currentPosition = Vector3.Lerp(
                currentPosition,
                desiredPosition,
                positionSmoothing * realDelta
            );

            // Apply position
            transform.position = currentPosition;

            // Smooth rotation to look at target
            Vector3 targetPoint = target.position + Vector3.up * height;
            Quaternion desiredRotation = Quaternion.LookRotation(targetPoint - transform.position);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationSmoothing * realDelta
            );
        }

        private Vector3 CalculateDesiredPosition()
        {
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -distance);
            return target.position + Vector3.up * height + offset;
        }

        private void HandleCursorToggle()
        {
            // Press Escape to toggle cursor lock
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }
}
