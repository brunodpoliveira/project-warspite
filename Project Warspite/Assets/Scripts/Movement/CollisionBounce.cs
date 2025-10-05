using UnityEngine;

namespace Warspite.Movement
{
    [RequireComponent(typeof(MomentumLocomotion))]
    public class CollisionBounce : MonoBehaviour
    {
        public float bounceThreshold = 7.5f; // m/s along normal
        public float bounceDamp = 0.7f;
        public float disruptionSeconds = 0.3f;

        private MomentumLocomotion _momentum;
        private float _disruptTimer;

        public bool IsDisrupted => _disruptTimer > 0f;

        void Awake()
        {
            _momentum = GetComponent<MomentumLocomotion>();
        }

        void Update()
        {
            if (_disruptTimer > 0f) _disruptTimer -= Time.deltaTime;
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Estimate relative speed against the hit normal
            var v = _momentum.Velocity;
            float speedAgainst = Vector3.Dot(v, -hit.normal); // positive if moving into the surface
            if (speedAgainst > bounceThreshold)
            {
                _momentum.ReflectVelocity(hit.normal, bounceDamp);
                _disruptTimer = disruptionSeconds;
            }
        }
    }
}
