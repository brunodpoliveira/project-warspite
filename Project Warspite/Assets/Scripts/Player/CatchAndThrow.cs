using UnityEngine;
using Warspite.Systems;
using Warspite.World;

namespace Warspite.Player
{
    /// <summary>
    /// Allows catching and throwing projectiles in deepest slow (L3).
    /// Hold RMB near a bullet to catch it, release to throw.
    /// </summary>
    public class CatchAndThrow : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TimeDilationController timeController;
        [SerializeField] private Transform handAnchor; // Where caught projectiles go

        [Header("Catch Settings")]
        [SerializeField] private float catchRadius = 2f;
        [SerializeField] private float catchAngle = 90f; // Cone in front of player
        [SerializeField] private KeyCode catchKey = KeyCode.Mouse1; // Right mouse button

        [Header("Throw Settings")]
        [SerializeField] private float throwSpeed = 30f;
        [SerializeField] private KeyCode altThrowKey = KeyCode.F;

        private Projectile caughtProjectile;
        private Camera playerCamera;

        void Start()
        {
            playerCamera = Camera.main;

            // Create hand anchor if not assigned
            if (handAnchor == null)
            {
                GameObject anchor = new GameObject("HandAnchor");
                anchor.transform.SetParent(transform);
                anchor.transform.localPosition = Vector3.forward * 1f + Vector3.up * 0.5f;
                handAnchor = anchor.transform;
            }
        }

        void Update()
        {
            if (timeController == null || !timeController.IsDeepestSlow())
            {
                // Not in L3, can't catch
                return;
            }

            if (caughtProjectile == null)
            {
                HandleCatch();
            }
            else
            {
                HandleThrow();
                UpdateCaughtProjectile();
            }
        }

        private void HandleCatch()
        {
            if (Input.GetKey(catchKey))
            {
                // Find nearby projectiles
                Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, catchRadius);

                Projectile closest = null;
                float closestDist = float.MaxValue;

                foreach (Collider col in nearbyColliders)
                {
                    Projectile proj = col.GetComponent<Projectile>();
                    if (proj != null && !proj.IsCaught)
                    {
                        // Check if in front of player
                        Vector3 toProj = proj.transform.position - transform.position;
                        float angle = Vector3.Angle(transform.forward, toProj);

                        if (angle < catchAngle * 0.5f)
                        {
                            float dist = toProj.magnitude;
                            if (dist < closestDist)
                            {
                                closest = proj;
                                closestDist = dist;
                            }
                        }
                    }
                }

                if (closest != null)
                {
                    CatchProjectile(closest);
                }
            }
        }

        private void CatchProjectile(Projectile projectile)
        {
            caughtProjectile = projectile;
            caughtProjectile.IsCaught = true;
            caughtProjectile.Freeze();
            
            // Parent to hand
            caughtProjectile.transform.SetParent(handAnchor);
            caughtProjectile.transform.localPosition = Vector3.zero;
        }

        private void HandleThrow()
        {
            bool releasePressed = Input.GetKeyUp(catchKey) || Input.GetKeyDown(altThrowKey);

            if (releasePressed)
            {
                ThrowProjectile();
            }
        }

        private void ThrowProjectile()
        {
            if (caughtProjectile == null) return;

            // Unparent and unfreeze
            caughtProjectile.transform.SetParent(null);
            caughtProjectile.Unfreeze();

            // Throw in camera look direction
            Vector3 throwDirection = playerCamera.transform.forward;
            caughtProjectile.Launch(throwDirection * throwSpeed);

            caughtProjectile.IsCaught = false;
            caughtProjectile = null;
        }

        private void UpdateCaughtProjectile()
        {
            // Keep projectile in hand (already parented, but ensure position)
            if (caughtProjectile != null)
            {
                caughtProjectile.transform.localPosition = Vector3.zero;
            }
        }

        void OnDrawGizmosSelected()
        {
            // Visualize catch radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, catchRadius);

            // Visualize catch cone
            if (playerCamera != null)
            {
                Gizmos.color = Color.green;
                Vector3 forward = transform.forward * catchRadius;
                Quaternion leftRot = Quaternion.Euler(0, -catchAngle * 0.5f, 0);
                Quaternion rightRot = Quaternion.Euler(0, catchAngle * 0.5f, 0);
                Gizmos.DrawLine(transform.position, transform.position + leftRot * forward);
                Gizmos.DrawLine(transform.position, transform.position + rightRot * forward);
            }
        }
    }
}
