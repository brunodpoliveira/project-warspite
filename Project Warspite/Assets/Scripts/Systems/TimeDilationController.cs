using UnityEngine;

namespace Warspite.Systems
{
    /// <summary>
    /// Manages global time dilation levels.
    /// Press Q to slow time, E to speed it up.
    /// Automatically adjusts Time.timeScale and fixedDeltaTime.
    /// </summary>
    public class TimeDilationController : MonoBehaviour
    {
        [Header("Time Levels")]
        [SerializeField] private float normalTimeScale = 1f;
        [SerializeField] private float slowLevel1 = 0.5f;
        [SerializeField] private float slowLevel2 = 0.2f;
        [SerializeField] private float slowLevel3 = 0.05f; // Near-freeze for catch/throw

        [Header("Input (leave empty for fallback)")]
        [SerializeField] private KeyCode slowerKey = KeyCode.Q;
        [SerializeField] private KeyCode fasterKey = KeyCode.E;

        [Header("Resource (optional)")]
        [SerializeField] private TimeDilationResource resource;

        private int currentLevel = 0; // 0=Normal, 1=Slow1, 2=Slow2, 3=Slow3
        private float baseFixedDeltaTime;

        public int CurrentLevel => currentLevel;
        public float CurrentTimeScale => Time.timeScale;

        void Awake()
        {
            baseFixedDeltaTime = Time.fixedDeltaTime;
        }

        void Start()
        {
            if (resource == null)
            {
                resource = GetComponent<TimeDilationResource>();
            }
        }

        void Update()
        {
            HandleInput();
            CheckResourceDepletion();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(slowerKey))
            {
                IncreaseSlowdown();
            }
            else if (Input.GetKeyDown(fasterKey))
            {
                DecreaseSlowdown();
            }
        }

        private void IncreaseSlowdown()
        {
            // Check if we have resource available
            if (resource != null && !resource.CanUseDilation() && currentLevel == 0)
            {
                // Can't activate dilation without resource
                return;
            }

            currentLevel = Mathf.Min(currentLevel + 1, 3);
            ApplyTimeLevel();
        }

        private void DecreaseSlowdown()
        {
            currentLevel = Mathf.Max(currentLevel - 1, 0);
            ApplyTimeLevel();
        }

        private void CheckResourceDepletion()
        {
            // If resource is depleted, force back to normal time
            if (resource != null && resource.IsEmpty && currentLevel > 0)
            {
                currentLevel = 0;
                ApplyTimeLevel();
            }
        }

        private void ApplyTimeLevel()
        {
            float targetScale = currentLevel switch
            {
                0 => normalTimeScale,
                1 => slowLevel1,
                2 => slowLevel2,
                3 => slowLevel3,
                _ => normalTimeScale
            };

            Time.timeScale = targetScale;
            Time.fixedDeltaTime = baseFixedDeltaTime * targetScale;
        }

        public bool IsDeepestSlow() => currentLevel == 3;
    }
}
