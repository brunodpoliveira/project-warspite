using UnityEngine;

namespace Warspite.Config
{
    [CreateAssetMenu(menuName = "Warspite/Prototype Tuning", fileName = "PrototypeTuning")]
    public class PrototypeTuning : ScriptableObject
    {
        [Header("Time")] public float[] timeScales = new float[] { 1f, 0.5f, 0.2f, 0.05f };
        [Header("Momentum")] public float maxAcceleration = 40f; public AnimationCurve turnRateBySpeed = AnimationCurve.Linear(0, 360, 20, 90); public float carryOverFactor = 0.9f;
        [Header("Bounce")] public float bounceThreshold = 7.5f; public float bounceDamp = 0.7f; public float disruptionSeconds = 0.3f;
        [Header("Catch/Throw")] public float catchRadius = 0.22f; public float throwForceMultiplier = 18f;
    }
}
