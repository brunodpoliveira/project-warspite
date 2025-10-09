using UnityEngine;

namespace Warspite.Player
{
    /// <summary>
    /// Compensates player movement to maintain real-time speed despite global Time.timeScale.
    /// Runs in LateUpdate to avoid interfering with normal movement calculations.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerTimeCompensator : MonoBehaviour
    {
        private CharacterController controller;
        private Vector3 lastPosition;
        private bool isFirstFrame = true;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        void Start()
        {
            lastPosition = transform.position;
        }

        void LateUpdate()
        {
            if (isFirstFrame)
            {
                isFirstFrame = false;
                lastPosition = transform.position;
                return;
            }

            // Skip if time is normal
            if (Mathf.Approximately(Time.timeScale, 1f))
            {
                lastPosition = transform.position;
                return;
            }

            // Calculate how much movement happened this frame
            Vector3 scaledMovement = transform.position - lastPosition;

            // Calculate how much SHOULD have happened at real-time
            float compensationFactor = (1f / Time.timeScale) - 1f;
            Vector3 additionalMovement = scaledMovement * compensationFactor;

            // Apply compensation
            if (additionalMovement.sqrMagnitude > 0.0001f)
            {
                controller.Move(additionalMovement);
            }

            lastPosition = transform.position;
        }
    }
}
