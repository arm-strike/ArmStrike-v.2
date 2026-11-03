using UnityEngine;

[CreateAssetMenu(fileName = "KinectInputTuningProfile", menuName = "Arm Strike/Kinect Input Tuning Profile")]
public sealed class KinectInputTuningProfile : ScriptableObject
{
    [Header("Neutral Gate")]
    [Min(0f)] public float inputArmNeutralHoldSeconds = 0.25f;
    [Min(0f)] public float neutralTurnAxisThreshold = 0.12f;
    [Min(0f)] public float neutralAttackSpeedThreshold = 0.18f;
    [Min(0f)] public float neutralChargeExtensionThreshold = 1.05f;
    [Min(0f)] public float neutralChargeElbowThreshold = 125f;

    [Header("Turn")]
    [Min(0f)] public float tPoseHandHeightTolerance = 0.20f;
    [Min(0f)] public float tPoseSideExtensionThreshold = 0.85f;
    [Min(0f)] public float turnEnterLeanThreshold = 0.18f;
    [Min(0f)] public float turnExitLeanThreshold = 0.10f;
    [Min(0f)] public float turnSensitivity = 2.25f;
    [Min(0f)] public float turnDepthWeight = 0.45f;
    [Min(0f)] public float turnLateralWeight = 1.0f;
    [Min(0f)] public float turnSmoothing = 10f;
    [Min(0f)] public float turnDeadZone = 0.05f;

    [Header("Attack")]
    [Min(0f)] public float attackArmedForwardMaxMeters = 0.12f;
    [Min(0f)] public float attackArmedHandHeightMinOffset = -0.20f;
    [Min(0f)] public float attackArmedHandHeightMaxOffset = 0.20f;
    [Min(0f)] public float attackArmedExtensionThreshold = 1.05f;
    [Min(0f)] public float attackForwardDeltaThresholdMeters = 0.13f;
    [Min(0f)] public float attackElbowAngleThresholdDegrees = 135f;
    [Min(0f)] public float attackSpeedThreshold = 0.45f;
    [Min(0f)] public float attackAccelerationThreshold = 3.0f;
    [Range(0f, 1f)] public float attackSmoothing = 0.35f;
    [Min(0f)] public float attackExtensionThreshold = 0.85f;
    [Min(0f)] public float attackPulseSeconds = 0.16f;
    [Min(0f)] public float attackCooldownSeconds = 0.30f;

    [Header("Charge")]
    [Min(0f)] public float chargeEnterExtensionThreshold = 0.95f;
    [Min(0f)] public float chargeReleaseExtensionThreshold = 1.10f;
    [Min(0f)] public float chargeElbowAngleThreshold = 125f;
    [Min(0f)] public float chargeReleaseElbowAngleThreshold = 135f;
    [Min(0f)] public float chargeHandMinY = 0.15f;
    [Min(0f)] public float chargeHandMaxY = 0.75f;
    [Min(0f)] public float releaseHandAboveHeadThreshold = 0.10f;
    [Min(0f)] public float chargeEnterHoldSeconds = 0.15f;
    [Min(0f)] public float chargeReleaseHoldSeconds = 0.12f;

    public static KinectInputTuningProfile CreateRuntimeDefaults()
    {
        KinectInputTuningProfile profile = CreateInstance<KinectInputTuningProfile>();
        profile.hideFlags = HideFlags.DontSave;
        return profile;
    }
}
