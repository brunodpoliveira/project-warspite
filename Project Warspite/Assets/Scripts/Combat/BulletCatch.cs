using UnityEngine;
using UnityEngine.InputSystem;
using Warspite.TimeSystem;

namespace Warspite.Combat
{
    public class BulletCatch : MonoBehaviour
    {
        [Header("Refs")]
        public TimeDilationController timeController;
        public Transform hand;
        public InputActionReference throwAction; // optional

        [Header("Tuning")]
        public float catchRadius = 0.22f;
        public LayerMask projectileLayer;
        public float throwForceMultiplier = 18f;

        private Projectile _held;
        private Rigidbody _heldRb;

        void OnEnable()
        {
            if (throwAction != null) { throwAction.action.performed += OnThrow; throwAction.action.Enable(); }
        }
        void OnDisable()
        {
            if (throwAction != null) { throwAction.action.performed -= OnThrow; throwAction.action.Disable(); }
        }

        void Update()
        {
            if (!timeController || !hand) return;
            if (_held) { // keep held at hand
                _held.transform.position = hand.position;
                _held.transform.rotation = hand.rotation;
                // Fallback: RMB release throws when using mouse fallback
                if ((throwAction == null || !throwAction.action.enabled) && UnityEngine.Input.GetMouseButtonUp(1))
                {
                    Throw();
                }
                // Fallback: F key to throw
                if ((throwAction == null || !throwAction.action.enabled) && UnityEngine.Input.GetKeyDown(KeyCode.F))
                {
                    Throw();
                }
                return;
            }

            if (!timeController.IsDeepestSlow()) return;

            // Fallback: require RMB to be held to catch when actions are not wired
            bool allowCatchNow = true;
            if (throwAction == null || !throwAction.action.enabled)
            {
                allowCatchNow = UnityEngine.Input.GetMouseButton(1); // hold RMB to allow catch window
            }

            if (allowCatchNow)
            {
                // Overlap small sphere to catch projectiles
                int mask = projectileLayer.value != 0 ? projectileLayer.value : ~0; // if not set, use all layers
                var hits = Physics.OverlapSphere(hand.position, catchRadius, mask, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (h.attachedRigidbody && h.TryGetComponent<Projectile>(out var proj))
                    {
                        Catch(proj);
                        break;
                    }
                }
            }
        }

        void Catch(Projectile proj)
        {
            _held = proj;
            _heldRb = proj.GetComponent<Rigidbody>();
            _held.inboundSpeed = _heldRb.velocity.magnitude;
            _heldRb.isKinematic = true;
            _heldRb.velocity = Vector3.zero;
            _held.transform.SetParent(hand, true);
            _held.transform.position = hand.position;
            _held.transform.rotation = hand.rotation;
        }

        void OnDrawGizmosSelected()
        {
            if (hand)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(hand.position, catchRadius);
            }
        }

        void OnThrow(InputAction.CallbackContext _)
        {
            Throw();
        }

        public void Throw()
        {
            if (!_held) return;
            _held.transform.SetParent(null, true);
            _heldRb.isKinematic = false;
            var cam = Camera.main;
            var dir = cam ? cam.transform.forward : transform.forward;
            float speed = Mathf.Max(5f, _held.inboundSpeed * throwForceMultiplier);
            _heldRb.velocity = dir.normalized * speed;
            _held = null;
            _heldRb = null;
        }
    }
}
