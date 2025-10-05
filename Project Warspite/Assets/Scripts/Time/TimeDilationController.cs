using UnityEngine;
using UnityEngine.InputSystem;

namespace Warspite.TimeSystem
{
    public class TimeDilationController : MonoBehaviour
    {
        [Header("Levels (index 0..N-1)")]
        public float[] timeScales = new float[] { 1.0f, 0.5f, 0.2f, 0.05f };
        [Range(0, 3)] public int startIndex = 0;
        [Tooltip("Lerp duration when changing timeScale (seconds in realtime)")]
        public float transitionDuration = 0.08f;

        // Fallback keyboard controls when InputActions are not assigned
        [Header("Optional Input Actions")] 
        public InputActionReference nextAction; 
        public InputActionReference prevAction; 
        public InputActionReference restartAction;

        float _baseFixedDelta;
        int _index;
        float _lerpStartScale;
        float _lerpTargetScale;
        float _lerpT;

        public int CurrentIndex => _index;
        public float CurrentScale => Time.timeScale;

        void Awake()
        {
            _baseFixedDelta = Time.fixedDeltaTime;
            _index = Mathf.Clamp(startIndex, 0, timeScales.Length - 1);
            SetScaleImmediate(timeScales[_index]);
        }

        void OnEnable()
        {
            if (nextAction != null) { nextAction.action.performed += OnNext; nextAction.action.Enable(); }
            if (prevAction != null) { prevAction.action.performed += OnPrev; prevAction.action.Enable(); }
            if (restartAction != null) { restartAction.action.performed += OnRestart; restartAction.action.Enable(); }
        }

        void OnDisable()
        {
            if (nextAction != null) { nextAction.action.performed -= OnNext; nextAction.action.Disable(); }
            if (prevAction != null) { prevAction.action.performed -= OnPrev; prevAction.action.Disable(); }
            if (restartAction != null) { restartAction.action.performed -= OnRestart; restartAction.action.Disable(); }
        }

        void Update()
        {
            // Smooth transition to target scale
            if (!Mathf.Approximately(Time.timeScale, _lerpTargetScale))
            {
                _lerpT += Time.unscaledDeltaTime / Mathf.Max(0.0001f, transitionDuration);
                float s = Mathf.Lerp(_lerpStartScale, _lerpTargetScale, Mathf.SmoothStep(0, 1, _lerpT));
                ApplyScale(s);
            }

            // Fallback keyboard controls when InputActions are not assigned
            bool nextEnabled = nextAction != null && nextAction.action.enabled;
            bool prevEnabled = prevAction != null && prevAction.action.enabled;
            bool restartEnabled = restartAction != null && restartAction.action.enabled;

            if (!nextEnabled && UnityEngine.Input.GetKeyDown(KeyCode.E))
            {
                NextLevel();
            }
            if (!prevEnabled && UnityEngine.Input.GetKeyDown(KeyCode.Q))
            {
                PrevLevel();
            }
            if (!restartEnabled && UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
            }
        }

        public void NextLevel()
        {
            _index = Mathf.Clamp(_index + 1, 0, timeScales.Length - 1);
            BeginTransitionTo(timeScales[_index]);
        }

        public void PrevLevel()
        {
            _index = Mathf.Clamp(_index - 1, 0, timeScales.Length - 1);
            BeginTransitionTo(timeScales[_index]);
        }

        public void SetLevel(int index)
        {
            _index = Mathf.Clamp(index, 0, timeScales.Length - 1);
            BeginTransitionTo(timeScales[_index]);
        }

        void BeginTransitionTo(float target)
        {
            _lerpStartScale = Time.timeScale;
            _lerpTargetScale = Mathf.Clamp(target, 0.01f, 1f);
            _lerpT = 0f;
        }

        void SetScaleImmediate(float scale)
        {
            _lerpStartScale = _lerpTargetScale = Mathf.Clamp(scale, 0.01f, 1f);
            _lerpT = 1f;
            ApplyScale(_lerpTargetScale);
        }

        void ApplyScale(float s)
        {
            Time.timeScale = s;
            Time.fixedDeltaTime = _baseFixedDelta * s; // keep physics stable in slowmo
        }

        void OnNext(InputAction.CallbackContext _)
        {
            NextLevel();
        }
        void OnPrev(InputAction.CallbackContext _)
        {
            PrevLevel();
        }
        void OnRestart(InputAction.CallbackContext _)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        public bool IsDeepestSlow() => _index >= timeScales.Length - 1;
    }
}
