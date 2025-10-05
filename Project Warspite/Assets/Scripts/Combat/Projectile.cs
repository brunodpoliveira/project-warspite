using UnityEngine;

namespace Warspite.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        public float lifetime = 10f;
        private float _t;
        [HideInInspector] public float inboundSpeed; // set when caught

        void Update()
        {
            _t += Time.deltaTime;
            if (_t >= lifetime) Destroy(gameObject);
        }
    }
}
