using UnityEngine;

namespace Warspite.Systems
{
    /// <summary>
    /// Manages time dilation resource bar.
    /// Fills linearly over time, drains when time dilation is active.
    /// Deeper slow levels drain faster.
    /// </summary>
    public class TimeDilationResource : MonoBehaviour
    {
        [Header("Resource Settings")]
        [SerializeField] private float maxResource = 100f;
        [SerializeField] private float currentResource;
        [SerializeField] private float rechargeRate = 10f; // Per second at normal time

        [Header("Drain Rates per Level")]
        [SerializeField] private float normalDrain = 0f; // Level 0 - no drain
        [SerializeField] private float level1Drain = 15f; // Slow L1
        [SerializeField] private float level2Drain = 30f; // Deep Slow L2
        [SerializeField] private float level3Drain = 50f; // Near-Freeze L3

        [Header("References")]
        [SerializeField] private TimeDilationController timeController;

        public float MaxResource => maxResource;
        public float CurrentResource => currentResource;
        public float ResourcePercent => currentResource / maxResource;
        public bool IsEmpty => currentResource <= 0;
        public bool IsFull => currentResource >= maxResource;

        void Awake()
        {
            currentResource = maxResource;
        }

        void Start()
        {
            if (timeController == null)
            {
                timeController = FindFirstObjectByType<TimeDilationController>();
            }
        }

        void Update()
        {
            if (timeController == null) return;

            // Use unscaled time for consistent resource management
            float delta = Time.unscaledDeltaTime;

            int currentLevel = timeController.CurrentLevel;

            if (currentLevel == 0)
            {
                // Normal time - recharge
                Recharge(rechargeRate * delta);
            }
            else
            {
                // Time dilation active - drain based on level
                float drainRate = GetDrainRate(currentLevel);
                Drain(drainRate * delta);

                // If depleted, force back to normal time
                if (IsEmpty)
                {
                    // This will be handled by TimeDilationController checking this component
                }
            }
        }

        private float GetDrainRate(int level)
        {
            return level switch
            {
                1 => level1Drain,
                2 => level2Drain,
                3 => level3Drain,
                _ => normalDrain
            };
        }

        public void Drain(float amount)
        {
            currentResource -= amount;
            currentResource = Mathf.Max(currentResource, 0);
        }

        public void Recharge(float amount)
        {
            currentResource += amount;
            currentResource = Mathf.Min(currentResource, maxResource);
        }

        public bool CanUseDilation()
        {
            return currentResource > 0;
        }

        public void SetResource(float amount)
        {
            currentResource = Mathf.Clamp(amount, 0, maxResource);
        }
    }
}
