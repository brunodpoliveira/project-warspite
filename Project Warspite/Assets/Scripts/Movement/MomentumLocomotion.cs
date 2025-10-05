using UnityEngine;

namespace Warspite.Movement
{
    [DefaultExecutionOrder(50)]
    public class MomentumLocomotion : MonoBehaviour
    {
        [Header("Momentum Tuning")]
        public float maxAcceleration = 40f;
        public AnimationCurve turnRateBySpeed = AnimationCurve.Linear(0, 360, 20, 90);
        public float carryOverFactor = 0.9f; // fraction of velocity kept when changing direction

        private Vector3 _lastPosition;
        private Vector3 _velocity;

        public Vector3 Velocity => _velocity;
        public float CurrentSpeed => _velocity.magnitude;

        void Start()
        {
            _lastPosition = transform.position;
        }

        void LateUpdate()
        {
            // Estimate velocity from displacement (character controller drives motion)
            Vector3 pos = transform.position;
            _velocity = (pos - _lastPosition) / Mathf.Max(Time.deltaTime, 1e-5f);
            _lastPosition = pos;
        }

        // Helper to blend desired move with momentum (for future integration into input path)
        public Vector3 GetBiasedMove(Vector3 desiredMove)
        {
            if (desiredMove.sqrMagnitude < 1e-4f) return desiredMove;

            Vector3 vDir = _velocity.sqrMagnitude > 1e-3f ? _velocity.normalized : Vector3.zero;
            float dot = Vector3.Dot(desiredMove.normalized, vDir);
            // Reduce steering when moving fast against momentum
            float speed = _velocity.magnitude;
            float maxTurn = turnRateBySpeed.Evaluate(speed);
            // This is a placeholder for a turn-rate limiter; actual application should occur in input layer
            Vector3 blended = Vector3.Slerp(vDir == Vector3.zero ? desiredMove : vDir, desiredMove, Mathf.Clamp01(maxTurn / 360f));
            // Carry-over keeps some of current velocity direction
            blended = Vector3.Slerp(blended, vDir, 1f - carryOverFactor);
            return blended.normalized;
        }

        public void ReflectVelocity(Vector3 normal, float damp)
        {
            if (_velocity.sqrMagnitude < 1e-4f) return;
            _velocity = Vector3.Reflect(_velocity, normal) * Mathf.Clamp01(damp);
            // Nudge position using new velocity to avoid sticky contacts
            transform.position += _velocity * Time.deltaTime * 0.02f;
        }
    }
}
