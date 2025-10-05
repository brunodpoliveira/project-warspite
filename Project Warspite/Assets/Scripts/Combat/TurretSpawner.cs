using UnityEngine;

namespace Warspite.Combat
{
    public class TurretSpawner : MonoBehaviour
    {
        public Projectile projectilePrefab;
        public float interval = 1.5f;
        public float muzzleSpeed = 20f;
        public Transform muzzle;

        float _timer;

        void Reset()
        {
            if (!muzzle) muzzle = transform;
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer = 0f;
                Fire();
            }
        }

        void Fire()
        {
            if (!projectilePrefab) return;
            var p = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
            var rb = p.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.velocity = muzzle.forward * muzzleSpeed;
        }
    }
}
