using UnityEngine;
using Warspite.Core;
using Warspite.Systems;
using Warspite.World;

namespace Warspite.Player
{
    /// <summary>
    /// Unified player combat system handling:
    /// - Melee punching (with doom tagging)
    /// - Projectile catching and throwing (L3 only)
    /// - Vampire health drain from critical enemies
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TimeDilationController timeController;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Transform handAnchor;
        [SerializeField] private AudioPulse audioPulse;

        [Header("Melee Settings")]
        [SerializeField] private float punchRange = 2f;
        [SerializeField] private float punchRadius = 1f;
        [SerializeField] private float punchDamage = 25f;
        [SerializeField] private float punchCooldown = 0.5f;
        [SerializeField] private bool applyKnockback = true;
        [SerializeField] private float knockbackForce = 10f;

        [Header("Catch & Throw Settings")]
        [SerializeField] private float catchRadius = 2f;
        [SerializeField] private float catchAngle = 180f; // Full sphere catch (was 90)
        [SerializeField] private float throwSpeed = 30f;
        [SerializeField] private float minThrowDistance = 1.5f; // Minimum distance to spawn projectile from player
        [SerializeField] private bool showTrajectory = true;

        [Header("Vampire Settings")]
        [SerializeField] private float drainRange = 3f;
        [SerializeField] private float drainAmount = 30f;

        [Header("Controls")]
        [SerializeField] private KeyCode punchKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode catchKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode vampireKey = KeyCode.F;

        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugGizmos = true;

        private Camera playerCamera;
        private float lastPunchTime = -999f;
        private Projectile caughtProjectile;
        private TrajectoryIndicator trajectoryIndicator;

        // Public properties for UI
        public bool IsPunchReady => Time.time >= lastPunchTime + punchCooldown;
        public float GetCooldownProgress() => Mathf.Clamp01(1f - ((Time.time - lastPunchTime) / punchCooldown));
        public bool HasCaughtProjectile => caughtProjectile != null;

        void Start()
        {
            playerCamera = Camera.main;

            // Auto-find references if not assigned
            if (timeController == null)
                timeController = FindFirstObjectByType<TimeDilationController>();
            
            if (playerHealth == null)
                playerHealth = GetComponent<PlayerHealth>();
            
            if (audioPulse == null)
                audioPulse = GetComponent<AudioPulse>();

            // Create hand anchor if not assigned
            if (handAnchor == null)
            {
                GameObject anchor = new GameObject("HandAnchor");
                anchor.transform.SetParent(transform);
                anchor.transform.localPosition = Vector3.forward * 1f + Vector3.up * 0.5f;
                handAnchor = anchor.transform;
            }

            // Create trajectory indicator
            if (showTrajectory)
            {
                GameObject trajObj = new GameObject("TrajectoryIndicator");
                trajObj.transform.SetParent(transform);
                trajectoryIndicator = trajObj.AddComponent<TrajectoryIndicator>();
                trajectoryIndicator.Hide();
            }
        }

        void Update()
        {
            HandleMelee();
            HandleCatchAndThrow();
            HandleVampire();
        }

        #region Melee Combat

        private void HandleMelee()
        {
            // Can't punch while holding projectile
            if (caughtProjectile != null) return;

            if (Input.GetKeyDown(punchKey) && IsPunchReady)
            {
                PerformPunch();
            }
        }

        private void PerformPunch()
        {
            lastPunchTime = Time.time;

            Vector3 punchDirection = playerCamera.transform.forward;
            Vector3 punchOrigin = transform.position + Vector3.up * 1f;

            RaycastHit[] hits = Physics.SphereCastAll(punchOrigin, punchRadius, punchDirection, punchRange);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue;

                Health targetHealth = hit.collider.GetComponent<Health>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    // Check if lethal and mark as doomed
                    DoomedTag doomedTag = hit.collider.GetComponent<DoomedTag>();
                    if (doomedTag != null && punchDamage >= targetHealth.CurrentHealth)
                    {
                        doomedTag.MarkAsDoomed(gameObject);
                    }

                    // Deal damage
                    targetHealth.TakeDamage(punchDamage);

                    // Apply knockback
                    if (applyKnockback)
                    {
                        Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            Vector3 knockbackDirection = (hit.point - punchOrigin).normalized;
                            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                        }
                    }

                    break; // Only hit first target
                }
            }
        }

        #endregion

        #region Catch & Throw

        private void HandleCatchAndThrow()
        {
            // Only works in L3 time dilation
            if (timeController == null)
            {
                Debug.LogWarning("PlayerCombat: TimeDilationController is null!");
                return;
            }

            if (!timeController.IsDeepestSlow())
            {
                // Hide trajectory if exiting L3
                if (trajectoryIndicator != null && trajectoryIndicator.IsVisible)
                {
                    trajectoryIndicator.Hide();
                }
                return;
            }

            if (caughtProjectile == null)
            {
                TryToCatch();
            }
            else
            {
                HoldAndAim();
                TryToThrow();
            }
        }

        private void TryToCatch()
        {
            if (!Input.GetKey(catchKey)) return;

            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, catchRadius);
            Projectile closest = null;
            float closestDist = float.MaxValue;

            foreach (Collider col in nearbyColliders)
            {
                Projectile proj = col.GetComponent<Projectile>();
                if (proj != null && !proj.IsCaught)
                {
                    Vector3 toProj = proj.transform.position - transform.position;
                    Vector3 lookDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;
                    float angle = Vector3.Angle(lookDirection, toProj);
                    float dist = toProj.magnitude;

                    if (angle < catchAngle * 0.5f && dist < closestDist)
                    {
                        closest = proj;
                        closestDist = dist;
                    }
                }
            }

            if (closest != null)
            {
                CatchProjectile(closest);
            }
        }

        private void CatchProjectile(Projectile projectile)
        {
            caughtProjectile = projectile;
            caughtProjectile.IsCaught = true;
            caughtProjectile.Freeze();
            caughtProjectile.transform.SetParent(handAnchor);
            caughtProjectile.transform.localPosition = Vector3.zero;
        }

        private void HoldAndAim()
        {
            // Keep projectile in hand
            if (caughtProjectile != null)
            {
                caughtProjectile.transform.localPosition = Vector3.zero;
            }

            // Show trajectory
            if (showTrajectory && trajectoryIndicator != null && caughtProjectile != null)
            {
                Vector3 throwDirection = playerCamera.transform.forward;
                // Calculate spawn position away from player
                Vector3 startPos = handAnchor.position + throwDirection * minThrowDistance;
                Vector3 velocity = throwDirection * throwSpeed;
                trajectoryIndicator.ShowTrajectory(startPos, velocity, 0.1f);
            }
        }

        private void TryToThrow()
        {
            bool shouldThrow = Input.GetKeyUp(catchKey);

            if (shouldThrow)
            {
                ThrowProjectile();
            }
        }

        private void ThrowProjectile()
        {
            if (caughtProjectile == null) return;

            // Hide trajectory
            if (trajectoryIndicator != null)
            {
                trajectoryIndicator.Hide();
            }

            // Calculate throw direction and spawn position
            Vector3 throwDirection = playerCamera.transform.forward;
            Vector3 spawnPosition = handAnchor.position + throwDirection * minThrowDistance;

            // Unparent and unfreeze
            caughtProjectile.transform.SetParent(null);
            caughtProjectile.transform.position = spawnPosition;
            caughtProjectile.Unfreeze();

            // Throw in camera direction
            caughtProjectile.Launch(throwDirection * throwSpeed);

            // Keep IsCaught = true for doom prediction
            caughtProjectile = null;
        }

        #endregion

        #region Vampire Drain

        private void HandleVampire()
        {
            // Can't drain while holding projectile
            if (caughtProjectile != null) return;

            if (Input.GetKeyDown(vampireKey))
            {
                TryToDrain();
            }
        }

        private void TryToDrain()
        {
            if (playerHealth == null) return;

            // Find nearby critical enemies
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, drainRange);

            foreach (Collider col in nearbyColliders)
            {
                Health targetHealth = col.GetComponent<Health>();
                if (targetHealth != null && targetHealth.IsCritical() && !targetHealth.IsDead)
                {
                    // Drain enemy
                    targetHealth.TakeDamage(drainAmount);
                    
                    // Heal player (get Health component from PlayerHealth)
                    Health playerHealthComponent = playerHealth.GetComponent<Health>();
                    if (playerHealthComponent != null)
                    {
                        playerHealthComponent.Heal(drainAmount);
                    }

                    return; // Only drain one enemy per press
                }
            }
        }

        #endregion

        #region Debug Visualization

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            Vector3 origin = transform.position + Vector3.up * 1f;

            // Punch range
            Gizmos.color = Color.red;
            Vector3 punchDir = playerCamera != null ? playerCamera.transform.forward : transform.forward;
            Gizmos.DrawWireSphere(origin, punchRadius);
            Gizmos.DrawWireSphere(origin + punchDir * punchRange, punchRadius);
            Gizmos.DrawLine(origin, origin + punchDir * punchRange);

            // Catch radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, catchRadius);

            // Catch cone (uses camera forward direction)
            Gizmos.color = Color.green;
            Vector3 lookDir = playerCamera != null ? playerCamera.transform.forward : transform.forward;
            Vector3 forward = lookDir * catchRadius;
            Quaternion leftRot = Quaternion.Euler(0, -catchAngle * 0.5f, 0);
            Quaternion rightRot = Quaternion.Euler(0, catchAngle * 0.5f, 0);
            Gizmos.DrawLine(transform.position, transform.position + leftRot * forward);
            Gizmos.DrawLine(transform.position, transform.position + rightRot * forward);

            // Vampire drain range
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, drainRange);
        }

        #endregion
    }
}
