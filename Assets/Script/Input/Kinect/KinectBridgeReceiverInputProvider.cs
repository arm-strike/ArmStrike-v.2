using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KinectBridgeReceiverInputProvider : MonoBehaviour, IPlayerInputProvider
{
    [SerializeField] private KinectBridgeReceiver receiver;
    [SerializeField] private KinectInputTuningProfile tuningProfile;
    [SerializeField] private bool autoCreateRuntimeProfile = true;
    [SerializeField] private bool logStateSummary = false;

    [Header("Debug")]
    [SerializeField] private bool debugAvailable;
    [SerializeField] private bool debugArmed;
    [SerializeField] private string debugStatus;
    [SerializeField] private string debugLastReason;
    [SerializeField] private long debugLockedTrackingId;
    [SerializeField] private bool debugHasLockedTrackingId;
    [SerializeField] private float debugTurnAxis;
    [SerializeField] private bool debugChargeHeld;
    [SerializeField] private bool debugLeftAttackHeld;
    [SerializeField] private bool debugRightAttackHeld;
    [SerializeField] private float debugNeutralStableSeconds;
    [SerializeField] private float debugBodyLeanX;
    [SerializeField] private bool debugIsTPose;
    [SerializeField] private bool debugIsTurnLeft;
    [SerializeField] private bool debugIsTurnRight;
    [SerializeField] private bool debugIsMostMuscularChargePose;
    [SerializeField] private bool debugIsReleasePose;
    [SerializeField] private float debugLeftAttackCooldownRemaining;
    [SerializeField] private float debugRightAttackCooldownRemaining;
    [SerializeField] private float debugLeftAttackPulseRemaining;
    [SerializeField] private float debugRightAttackPulseRemaining;
    [SerializeField] private bool debugLeftAttackCandidate;
    [SerializeField] private bool debugRightAttackCandidate;
    [SerializeField] private bool debugLeftAttackTriggered;
    [SerializeField] private bool debugRightAttackTriggered;
    [SerializeField] private float debugLeftAttackScore;
    [SerializeField] private float debugRightAttackScore;
    [SerializeField] private float debugLeftArmExtensionNormalized;
    [SerializeField] private float debugRightArmExtensionNormalized;
    [SerializeField] private float debugLeftHandSpeedMetersPerSecond;
    [SerializeField] private float debugRightHandSpeedMetersPerSecond;
    [SerializeField] private float debugLeftHandAccelerationMetersPerSecondSquared;
    [SerializeField] private float debugRightHandAccelerationMetersPerSecondSquared;
    [SerializeField] private float debugLeftHandTowardKinectSpeedMetersPerSecond;
    [SerializeField] private float debugRightHandTowardKinectSpeedMetersPerSecond;
    [SerializeField] private float debugLeftHandForwardFromShoulderMeters;
    [SerializeField] private float debugRightHandForwardFromShoulderMeters;
    [SerializeField] private float debugLeftAttackArmedForwardFromShoulderMeters;
    [SerializeField] private float debugRightAttackArmedForwardFromShoulderMeters;
    [SerializeField] private float debugLeftAttackForwardDeltaMeters;
    [SerializeField] private float debugRightAttackForwardDeltaMeters;
    [SerializeField] private bool debugLeftAttackArmed;
    [SerializeField] private bool debugRightAttackArmed;
    [SerializeField] private float debugLeftElbowAngleDegrees;
    [SerializeField] private float debugRightElbowAngleDegrees;
    [SerializeField, TextArea(6, 16)] private string debugSnapshot;

    private KinectGestureMapper mapper;
    private PlayerInputState cachedState = PlayerInputState.Neutral;
    private KinectGestureDebugState cachedDebugState;
    private KinectGestureAnalysis cachedAnalysis;
    private long? lastObservedTrackingId;
    private int lastEvaluatedFrame = -1;
    private bool hasLoggedMissingProfileWarning;

    public string ProviderName
    {
        get { return "KinectBridgeReceiver"; }
    }

    public bool IsAvailable
    {
        get
        {
            if (!isActiveAndEnabled || receiver == null)
            {
                return false;
            }

            return receiver.IsConnected;
        }
    }

    public KinectBridgeReceiver Receiver
    {
        get { return receiver; }
    }

    public KinectInputTuningProfile TuningProfile
    {
        get { return tuningProfile; }
    }

    public void Configure(KinectBridgeReceiver newReceiver, KinectInputTuningProfile newTuningProfile, bool useRuntimeProfileWhenMissing)
    {
        receiver = newReceiver;
        tuningProfile = newTuningProfile;
        autoCreateRuntimeProfile = useRuntimeProfileWhenMissing;

        if (mapper == null)
        {
            mapper = new KinectGestureMapper();
        }

        mapper.Reset();
        cachedState = PlayerInputState.Neutral;
        cachedDebugState = default(KinectGestureDebugState);
        cachedAnalysis = null;
        lastObservedTrackingId = null;
        lastEvaluatedFrame = -1;
        hasLoggedMissingProfileWarning = false;
        ResetDebugFields();
    }

    public PlayerInputState ReadState()
    {
        EvaluateIfNeeded();
        return cachedState;
    }

    private void Reset()
    {
        CacheDependencies();
    }

    private void Awake()
    {
        CacheDependencies();
    }

    private void OnEnable()
    {
        CacheDependencies();
    }

    private void OnValidate()
    {
        CacheDependencies();
    }

    private void CacheDependencies()
    {
        if (receiver == null)
        {
            receiver = GetComponent<KinectBridgeReceiver>();
        }

        if (tuningProfile == null && autoCreateRuntimeProfile)
        {
            tuningProfile = KinectInputTuningProfile.CreateRuntimeDefaults();
        }

        if (mapper == null)
        {
            mapper = new KinectGestureMapper();
        }
    }

    private void EvaluateIfNeeded()
    {
        if (lastEvaluatedFrame == Time.frameCount)
        {
            return;
        }

        lastEvaluatedFrame = Time.frameCount;
        CacheDependencies();

        if (receiver == null)
        {
            ResetState("KinectBridgeReceiver が未設定です。");
            return;
        }

        if (!receiver.IsConnected)
        {
            ResetState("Kinect の接続がありません。");
            return;
        }

        if (tuningProfile == null)
        {
            if (!hasLoggedMissingProfileWarning)
            {
                hasLoggedMissingProfileWarning = true;
                Debug.LogWarning("[Kinect] KinectInputTuningProfile が未設定のため、ランタイム既定値を使用します。");
            }

            tuningProfile = KinectInputTuningProfile.CreateRuntimeDefaults();
        }

        long? lockedTrackingId = receiver.LockedTrackingId;
        if (lockedTrackingId != lastObservedTrackingId)
        {
            mapper.Reset();
            cachedState = PlayerInputState.Neutral;
            cachedDebugState = default(KinectGestureDebugState);
            cachedAnalysis = null;
            lastObservedTrackingId = lockedTrackingId;
        }

        KinectTrackingFrame frame = receiver.LatestFrame;
        KinectGestureAnalysis analysis = mapper.Analyze(frame, tuningProfile);
        cachedAnalysis = analysis;

        float deltaTime = Mathf.Max(0f, Time.deltaTime);
        debugAvailable = true;
        debugArmed = true;
        debugHasLockedTrackingId = lockedTrackingId.HasValue;
        debugLockedTrackingId = lockedTrackingId.HasValue ? lockedTrackingId.Value : 0L;
        debugStatus = receiver.Status.ToString();
        debugLastReason = analysis.hasValidFrame ? string.Empty : DescribeInvalidFrame(frame);

        if (!analysis.hasValidFrame)
        {
            cachedState = mapper.EvaluateInvalidFrame(out cachedDebugState);
            UpdateDebugFields(cachedDebugState);
            debugStatus = receiver.Status.ToString();
            return;
        }

        cachedState = mapper.Evaluate(analysis, tuningProfile, deltaTime, out cachedDebugState);
        UpdateDebugFields(cachedDebugState);

        if (logStateSummary && debugAvailable && debugArmed)
        {
            Debug.Log("[Kinect] turn=" + debugTurnAxis.ToString("0.00") + " charge=" + debugChargeHeld + " left=" + debugLeftAttackHeld + " right=" + debugRightAttackHeld);
        }
    }

    private void UpdateDebugFields(KinectGestureDebugState debugState)
    {
        debugNeutralStableSeconds = debugState.neutralStableSeconds;
        debugArmed = debugState.armed;
        debugTurnAxis = debugState.smoothedTurnAxis;
        debugChargeHeld = cachedState.chargeHeld;
        debugLeftAttackHeld = cachedState.leftAttackHeld;
        debugRightAttackHeld = cachedState.rightAttackHeld;
        debugBodyLeanX = debugState.bodyLeanX;
        debugIsTPose = debugState.isTPose;
        debugIsTurnLeft = debugState.isTurnLeft;
        debugIsTurnRight = debugState.isTurnRight;
        debugIsMostMuscularChargePose = debugState.isMostMuscularChargePose;
        debugIsReleasePose = debugState.isReleasePose;
        debugLeftAttackCooldownRemaining = debugState.leftAttackCooldownRemaining;
        debugRightAttackCooldownRemaining = debugState.rightAttackCooldownRemaining;
        debugLeftAttackPulseRemaining = debugState.leftAttackPulseRemaining;
        debugRightAttackPulseRemaining = debugState.rightAttackPulseRemaining;
        debugLeftAttackCandidate = debugState.leftAttackCandidate;
        debugRightAttackCandidate = debugState.rightAttackCandidate;
        debugLeftAttackTriggered = debugState.leftAttackTriggered;
        debugRightAttackTriggered = debugState.rightAttackTriggered;
        debugLeftAttackScore = debugState.leftAttackScore;
        debugRightAttackScore = debugState.rightAttackScore;
        debugLeftArmExtensionNormalized = debugState.leftArmExtensionNormalized;
        debugRightArmExtensionNormalized = debugState.rightArmExtensionNormalized;
        debugLeftHandSpeedMetersPerSecond = debugState.leftHandSpeedMetersPerSecond;
        debugRightHandSpeedMetersPerSecond = debugState.rightHandSpeedMetersPerSecond;
        debugLeftHandAccelerationMetersPerSecondSquared = debugState.leftHandAccelerationMetersPerSecondSquared;
        debugRightHandAccelerationMetersPerSecondSquared = debugState.rightHandAccelerationMetersPerSecondSquared;
        debugLeftHandTowardKinectSpeedMetersPerSecond = debugState.leftHandTowardKinectSpeedMetersPerSecond;
        debugRightHandTowardKinectSpeedMetersPerSecond = debugState.rightHandTowardKinectSpeedMetersPerSecond;
        debugLeftHandForwardFromShoulderMeters = debugState.leftHandForwardFromShoulderMeters;
        debugRightHandForwardFromShoulderMeters = debugState.rightHandForwardFromShoulderMeters;
        debugLeftAttackArmedForwardFromShoulderMeters = debugState.leftAttackArmedForwardFromShoulderMeters;
        debugRightAttackArmedForwardFromShoulderMeters = debugState.rightAttackArmedForwardFromShoulderMeters;
        debugLeftAttackForwardDeltaMeters = debugState.leftAttackForwardDeltaMeters;
        debugRightAttackForwardDeltaMeters = debugState.rightAttackForwardDeltaMeters;
        debugLeftAttackArmed = debugState.leftAttackArmed;
        debugRightAttackArmed = debugState.rightAttackArmed;
        debugLeftElbowAngleDegrees = debugState.leftElbowAngleDegrees;
        debugRightElbowAngleDegrees = debugState.rightElbowAngleDegrees;
    }

    private void ResetDebugFields()
    {
        debugAvailable = false;
        debugArmed = false;
        debugStatus = string.Empty;
        debugLastReason = string.Empty;
        debugHasLockedTrackingId = false;
        debugLockedTrackingId = 0L;
        debugTurnAxis = 0f;
        debugChargeHeld = false;
        debugLeftAttackHeld = false;
        debugRightAttackHeld = false;
        debugNeutralStableSeconds = 0f;
        debugBodyLeanX = 0f;
        debugIsTPose = false;
        debugIsTurnLeft = false;
        debugIsTurnRight = false;
        debugIsMostMuscularChargePose = false;
        debugIsReleasePose = false;
        debugLeftAttackCooldownRemaining = 0f;
        debugRightAttackCooldownRemaining = 0f;
        debugLeftAttackPulseRemaining = 0f;
        debugRightAttackPulseRemaining = 0f;
        debugLeftAttackCandidate = false;
        debugRightAttackCandidate = false;
        debugLeftAttackTriggered = false;
        debugRightAttackTriggered = false;
        debugLeftAttackScore = 0f;
        debugRightAttackScore = 0f;
        debugLeftArmExtensionNormalized = 0f;
        debugRightArmExtensionNormalized = 0f;
        debugLeftHandSpeedMetersPerSecond = 0f;
        debugRightHandSpeedMetersPerSecond = 0f;
        debugLeftHandAccelerationMetersPerSecondSquared = 0f;
        debugRightHandAccelerationMetersPerSecondSquared = 0f;
        debugLeftHandTowardKinectSpeedMetersPerSecond = 0f;
        debugRightHandTowardKinectSpeedMetersPerSecond = 0f;
        debugLeftHandForwardFromShoulderMeters = 0f;
        debugRightHandForwardFromShoulderMeters = 0f;
        debugLeftAttackArmedForwardFromShoulderMeters = 0f;
        debugRightAttackArmedForwardFromShoulderMeters = 0f;
        debugLeftAttackForwardDeltaMeters = 0f;
        debugRightAttackForwardDeltaMeters = 0f;
        debugLeftAttackArmed = false;
        debugRightAttackArmed = false;
        debugLeftElbowAngleDegrees = 0f;
        debugRightElbowAngleDegrees = 0f;
    }

    private void ResetState(string reason)
    {
        if (mapper != null)
        {
            mapper.Reset();
        }

        cachedState = PlayerInputState.Neutral;
        cachedDebugState = default(KinectGestureDebugState);
        cachedAnalysis = null;
        lastObservedTrackingId = null;
        ResetDebugFields();
        debugStatus = reason;
        debugLastReason = reason;
    }

    [ContextMenu("Log Kinect Debug Snapshot")]
    private void LogKinectDebugSnapshot()
    {
        string snapshot = BuildDebugSnapshot();
        debugSnapshot = snapshot;
        Debug.Log(snapshot);
    }

    [ContextMenu("Copy Kinect Debug Snapshot")]
    private void CopyKinectDebugSnapshot()
    {
        string snapshot = BuildDebugSnapshot();
        debugSnapshot = snapshot;
        GUIUtility.systemCopyBuffer = snapshot;
        Debug.Log("[Kinect] Debug snapshot copied to clipboard.");
    }

    private string BuildDebugSnapshot()
    {
        StringBuilder sb = new StringBuilder(2048);
        sb.AppendLine("[Kinect Debug Snapshot]");
        sb.AppendLine("time.frameCount=" + Time.frameCount);
        sb.AppendLine("provider.available=" + debugAvailable);
        sb.AppendLine("provider.armed=" + debugArmed);
        sb.AppendLine("provider.status=" + debugStatus);
        sb.AppendLine("provider.lastReason=" + debugLastReason);
        sb.AppendLine("provider.lockedTrackingId=" + (debugHasLockedTrackingId ? debugLockedTrackingId.ToString() : "(none)"));
        sb.AppendLine("state.turnAxis=" + debugTurnAxis.ToString("0.000"));
        sb.AppendLine("state.chargeHeld=" + debugChargeHeld);
        sb.AppendLine("state.leftAttackHeld=" + debugLeftAttackHeld);
        sb.AppendLine("state.rightAttackHeld=" + debugRightAttackHeld);
        sb.AppendLine("state.neutralStableSeconds=" + debugNeutralStableSeconds.ToString("0.000"));
        sb.AppendLine("analysis.bodyLeanX=" + debugBodyLeanX.ToString("0.000"));
        sb.AppendLine("analysis.isTPose=" + debugIsTPose);
        sb.AppendLine("analysis.isTurnLeft=" + debugIsTurnLeft);
        sb.AppendLine("analysis.isTurnRight=" + debugIsTurnRight);
        sb.AppendLine("analysis.isMostMuscularChargePose=" + debugIsMostMuscularChargePose);
        sb.AppendLine("analysis.isReleasePose=" + debugIsReleasePose);
        sb.AppendLine("analysis.leftArmExtensionNormalized=" + debugLeftArmExtensionNormalized.ToString("0.000"));
        sb.AppendLine("analysis.rightArmExtensionNormalized=" + debugRightArmExtensionNormalized.ToString("0.000"));
        sb.AppendLine("analysis.leftHandSpeedMetersPerSecond=" + debugLeftHandSpeedMetersPerSecond.ToString("0.000"));
        sb.AppendLine("analysis.rightHandSpeedMetersPerSecond=" + debugRightHandSpeedMetersPerSecond.ToString("0.000"));
        sb.AppendLine("analysis.leftHandAccelerationMetersPerSecondSquared=" + debugLeftHandAccelerationMetersPerSecondSquared.ToString("0.000"));
        sb.AppendLine("analysis.rightHandAccelerationMetersPerSecondSquared=" + debugRightHandAccelerationMetersPerSecondSquared.ToString("0.000"));
        sb.AppendLine("analysis.leftHandTowardKinectSpeedMetersPerSecond=" + debugLeftHandTowardKinectSpeedMetersPerSecond.ToString("0.000"));
        sb.AppendLine("analysis.rightHandTowardKinectSpeedMetersPerSecond=" + debugRightHandTowardKinectSpeedMetersPerSecond.ToString("0.000"));
        sb.AppendLine("analysis.leftHandForwardFromShoulderMeters=" + debugLeftHandForwardFromShoulderMeters.ToString("0.000"));
        sb.AppendLine("analysis.rightHandForwardFromShoulderMeters=" + debugRightHandForwardFromShoulderMeters.ToString("0.000"));
        sb.AppendLine("analysis.leftAttackArmedForwardFromShoulderMeters=" + debugLeftAttackArmedForwardFromShoulderMeters.ToString("0.000"));
        sb.AppendLine("analysis.rightAttackArmedForwardFromShoulderMeters=" + debugRightAttackArmedForwardFromShoulderMeters.ToString("0.000"));
        sb.AppendLine("analysis.leftAttackForwardDeltaMeters=" + debugLeftAttackForwardDeltaMeters.ToString("0.000"));
        sb.AppendLine("analysis.rightAttackForwardDeltaMeters=" + debugRightAttackForwardDeltaMeters.ToString("0.000"));
        sb.AppendLine("analysis.leftAttackArmed=" + debugLeftAttackArmed);
        sb.AppendLine("analysis.rightAttackArmed=" + debugRightAttackArmed);
        sb.AppendLine("analysis.leftAttackCandidate=" + debugLeftAttackCandidate);
        sb.AppendLine("analysis.rightAttackCandidate=" + debugRightAttackCandidate);
        sb.AppendLine("analysis.leftAttackTriggered=" + debugLeftAttackTriggered);
        sb.AppendLine("analysis.rightAttackTriggered=" + debugRightAttackTriggered);
        sb.AppendLine("analysis.leftAttackScore=" + debugLeftAttackScore.ToString("0.000"));
        sb.AppendLine("analysis.rightAttackScore=" + debugRightAttackScore.ToString("0.000"));
        sb.AppendLine("analysis.leftElbowAngleDegrees=" + debugLeftElbowAngleDegrees.ToString("0.000"));
        sb.AppendLine("analysis.rightElbowAngleDegrees=" + debugRightElbowAngleDegrees.ToString("0.000"));
        sb.AppendLine("analysis.leftAttackCooldownRemaining=" + debugLeftAttackCooldownRemaining.ToString("0.000"));
        sb.AppendLine("analysis.rightAttackCooldownRemaining=" + debugRightAttackCooldownRemaining.ToString("0.000"));
        sb.AppendLine("analysis.leftAttackPulseRemaining=" + debugLeftAttackPulseRemaining.ToString("0.000"));
        sb.AppendLine("analysis.rightAttackPulseRemaining=" + debugRightAttackPulseRemaining.ToString("0.000"));
        return sb.ToString();
    }

    private static string DescribeInvalidFrame(KinectTrackingFrame frame)
    {
        if (frame == null)
        {
            return "フレームがありません。";
        }

        if (!frame.Tracked)
        {
            return "追跡中のフレームがありません。";
        }

        if (!frame.IsNormalizedValid)
        {
            return "正規化に失敗しました。";
        }

        if (frame.NormalizedJoints == null)
        {
            return "正規化ジョイントがありません。";
        }

        if (frame.Metrics == null)
        {
            return "解析メトリクスがありません。";
        }

        return "無効なフレームです。";
    }
}
