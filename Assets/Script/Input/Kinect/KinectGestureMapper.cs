using UnityEngine;

public sealed class KinectGestureAnalysis
{
    public bool hasValidFrame;
    public bool isTracked;
    public bool isNormalizedValid;
    public float bodyLeanX;
    public float leftShoulderNormalizedY;
    public float rightShoulderNormalizedY;
    public float shoulderCenterNormalizedY;
    public bool isTPose;
    public bool isMostMuscularChargePose;
    public bool isReleasePose;
    public float leftArmExtensionNormalized;
    public float rightArmExtensionNormalized;
    public float leftHandSpeedMetersPerSecond;
    public float rightHandSpeedMetersPerSecond;
    public float leftHandTowardKinectSpeedMetersPerSecond;
    public float rightHandTowardKinectSpeedMetersPerSecond;
    public float leftHandAccelerationMetersPerSecondSquared;
    public float rightHandAccelerationMetersPerSecondSquared;
    public float leftHandForwardFromShoulderMeters;
    public float rightHandForwardFromShoulderMeters;
    public float leftElbowAngleDegrees;
    public float rightElbowAngleDegrees;
    public float leftHandNormalizedY;
    public float rightHandNormalizedY;
    public bool leftAttackCandidate;
    public bool rightAttackCandidate;
    public bool leftAttackTriggered;
    public bool rightAttackTriggered;
    public float leftAttackScore;
    public float rightAttackScore;
}

public struct KinectGestureDebugState
{
    public bool armed;
    public bool chargeLatched;
    public float neutralStableSeconds;
    public float bodyLeanX;
    public float rawTurnAxis;
    public float smoothedTurnAxis;
    public bool isTPose;
    public bool isTurnLeft;
    public bool isTurnRight;
    public bool isMostMuscularChargePose;
    public bool isReleasePose;
    public bool leftAttackPulseActive;
    public bool rightAttackPulseActive;
    public float leftAttackPulseRemaining;
    public float rightAttackPulseRemaining;
    public float leftAttackCooldownRemaining;
    public float rightAttackCooldownRemaining;
    public bool leftAttackCandidate;
    public bool rightAttackCandidate;
    public bool leftAttackTriggered;
    public bool rightAttackTriggered;
    public float leftAttackScore;
    public float rightAttackScore;
    public float leftArmExtensionNormalized;
    public float rightArmExtensionNormalized;
    public float leftHandSpeedMetersPerSecond;
    public float rightHandSpeedMetersPerSecond;
    public float leftHandAccelerationMetersPerSecondSquared;
    public float rightHandAccelerationMetersPerSecondSquared;
    public float leftHandTowardKinectSpeedMetersPerSecond;
    public float rightHandTowardKinectSpeedMetersPerSecond;
    public float leftElbowAngleDegrees;
    public float rightElbowAngleDegrees;
    public float leftHandForwardFromShoulderMeters;
    public float rightHandForwardFromShoulderMeters;
    public float leftAttackArmedForwardFromShoulderMeters;
    public float rightAttackArmedForwardFromShoulderMeters;
    public float leftAttackForwardDeltaMeters;
    public float rightAttackForwardDeltaMeters;
    public bool leftAttackArmed;
    public bool rightAttackArmed;
}

public sealed class KinectGestureMapper
{
    private float currentTurnAxis;
    private float rawTurnSignal;
    private float smoothedTurnSignal;
    private float chargeEnterStableSeconds;
    private float chargeReleaseStableSeconds;
    private bool chargeLatched;
    private bool leftAttackArmed;
    private bool rightAttackArmed;
    private float leftAttackArmedBaselineForwardFromShoulder;
    private float rightAttackArmedBaselineForwardFromShoulder;
    private float previousLeftHandSpeedMetersPerSecond;
    private float previousRightHandSpeedMetersPerSecond;
    private float smoothedLeftHandAccelerationMetersPerSecondSquared;
    private float smoothedRightHandAccelerationMetersPerSecondSquared;
    private bool hasMotionSample;
    private float leftAttackPulseRemaining;
    private float rightAttackPulseRemaining;
    private float leftAttackCooldownRemaining;
    private float rightAttackCooldownRemaining;
    private bool leftAttackGestureLatched;
    private bool rightAttackGestureLatched;

    public void Reset()
    {
        currentTurnAxis = 0f;
        rawTurnSignal = 0f;
        smoothedTurnSignal = 0f;
        chargeEnterStableSeconds = 0f;
        chargeReleaseStableSeconds = 0f;
        chargeLatched = false;
        leftAttackArmed = false;
        rightAttackArmed = false;
        leftAttackArmedBaselineForwardFromShoulder = 0f;
        rightAttackArmedBaselineForwardFromShoulder = 0f;
        previousLeftHandSpeedMetersPerSecond = 0f;
        previousRightHandSpeedMetersPerSecond = 0f;
        smoothedLeftHandAccelerationMetersPerSecondSquared = 0f;
        smoothedRightHandAccelerationMetersPerSecondSquared = 0f;
        hasMotionSample = false;
        leftAttackPulseRemaining = 0f;
        rightAttackPulseRemaining = 0f;
        leftAttackCooldownRemaining = 0f;
        rightAttackCooldownRemaining = 0f;
        leftAttackGestureLatched = false;
        rightAttackGestureLatched = false;
    }

    public KinectGestureAnalysis Analyze(KinectTrackingFrame frame, KinectInputTuningProfile profile)
    {
        KinectGestureAnalysis analysis = new KinectGestureAnalysis();
        if (frame == null || profile == null)
        {
            return analysis;
        }

        analysis.isTracked = frame.Tracked;
        analysis.isNormalizedValid = frame.IsNormalizedValid;

        if (!frame.Tracked || !frame.IsNormalizedValid || frame.NormalizedJoints == null || frame.Metrics == null)
        {
            return analysis;
        }

        analysis.hasValidFrame = true;
        analysis.leftArmExtensionNormalized = GetLeftArmExtension(frame);
        analysis.rightArmExtensionNormalized = GetRightArmExtension(frame);
        analysis.leftHandSpeedMetersPerSecond = frame.Metrics.leftHandSpeedMetersPerSecond;
        analysis.rightHandSpeedMetersPerSecond = frame.Metrics.rightHandSpeedMetersPerSecond;
        analysis.leftHandTowardKinectSpeedMetersPerSecond = frame.Metrics.leftHandTowardKinectSpeedMetersPerSecond;
        analysis.rightHandTowardKinectSpeedMetersPerSecond = frame.Metrics.rightHandTowardKinectSpeedMetersPerSecond;
        analysis.leftElbowAngleDegrees = frame.Metrics.leftElbowAngleDegrees;
        analysis.rightElbowAngleDegrees = frame.Metrics.rightElbowAngleDegrees;
        analysis.leftHandForwardFromShoulderMeters = frame.Metrics.leftHandForwardFromShoulderMeters;
        analysis.rightHandForwardFromShoulderMeters = frame.Metrics.rightHandForwardFromShoulderMeters;
        analysis.leftHandNormalizedY = GetHandNormalizedY(frame, true);
        analysis.rightHandNormalizedY = GetHandNormalizedY(frame, false);
        analysis.leftShoulderNormalizedY = GetShoulderNormalizedY(frame, true);
        analysis.rightShoulderNormalizedY = GetShoulderNormalizedY(frame, false);
        analysis.shoulderCenterNormalizedY = GetShoulderCenterNormalizedY(frame);
        analysis.bodyLeanX = ComputeBodyLeanX(frame);
        analysis.isTPose = IsTPoseCandidate(analysis, profile);
        analysis.isMostMuscularChargePose = IsChargePoseCandidate(analysis, profile);
        analysis.isReleasePose = IsReleasePoseCandidate(analysis, profile);
        return analysis;
    }

    public PlayerInputState EvaluateInvalidFrame(out KinectGestureDebugState debugState)
    {
        debugState = new KinectGestureDebugState();
        currentTurnAxis = 0f;
        rawTurnSignal = 0f;
        smoothedTurnSignal = 0f;
        chargeEnterStableSeconds = 0f;
        chargeReleaseStableSeconds = 0f;
        leftAttackArmed = false;
        rightAttackArmed = false;
        leftAttackArmedBaselineForwardFromShoulder = 0f;
        rightAttackArmedBaselineForwardFromShoulder = 0f;
        leftAttackGestureLatched = false;
        rightAttackGestureLatched = false;
        previousLeftHandSpeedMetersPerSecond = 0f;
        previousRightHandSpeedMetersPerSecond = 0f;
        smoothedLeftHandAccelerationMetersPerSecondSquared = 0f;
        smoothedRightHandAccelerationMetersPerSecondSquared = 0f;
        hasMotionSample = false;
        leftAttackPulseRemaining = 0f;
        rightAttackPulseRemaining = 0f;

        debugState.armed = false;
        debugState.chargeLatched = chargeLatched;
        debugState.neutralStableSeconds = 0f;
        debugState.bodyLeanX = 0f;
        debugState.rawTurnAxis = 0f;
        debugState.smoothedTurnAxis = 0f;
        debugState.isTPose = false;
        debugState.isTurnLeft = false;
        debugState.isTurnRight = false;
        debugState.isMostMuscularChargePose = false;
        debugState.isReleasePose = false;
        debugState.leftAttackPulseActive = false;
        debugState.rightAttackPulseActive = false;
        debugState.leftAttackPulseRemaining = 0f;
        debugState.rightAttackPulseRemaining = 0f;
        debugState.leftAttackCooldownRemaining = leftAttackCooldownRemaining;
        debugState.rightAttackCooldownRemaining = rightAttackCooldownRemaining;
        debugState.leftAttackCandidate = false;
        debugState.rightAttackCandidate = false;
        debugState.leftAttackTriggered = false;
        debugState.rightAttackTriggered = false;
        debugState.leftAttackScore = 0f;
        debugState.rightAttackScore = 0f;
        debugState.leftArmExtensionNormalized = 0f;
        debugState.rightArmExtensionNormalized = 0f;
        debugState.leftHandSpeedMetersPerSecond = 0f;
        debugState.rightHandSpeedMetersPerSecond = 0f;
        debugState.leftHandAccelerationMetersPerSecondSquared = 0f;
        debugState.rightHandAccelerationMetersPerSecondSquared = 0f;
        debugState.leftHandTowardKinectSpeedMetersPerSecond = 0f;
        debugState.rightHandTowardKinectSpeedMetersPerSecond = 0f;
        debugState.leftHandForwardFromShoulderMeters = 0f;
        debugState.rightHandForwardFromShoulderMeters = 0f;
        debugState.leftElbowAngleDegrees = 0f;
        debugState.rightElbowAngleDegrees = 0f;
        debugState.leftAttackArmedForwardFromShoulderMeters = 0f;
        debugState.rightAttackArmedForwardFromShoulderMeters = 0f;
        debugState.leftAttackForwardDeltaMeters = 0f;
        debugState.rightAttackForwardDeltaMeters = 0f;
        debugState.leftAttackArmed = false;
        debugState.rightAttackArmed = false;

        return new PlayerInputState(0f, chargeLatched, false, false);
    }

    public bool IsNeutralPose(KinectGestureAnalysis analysis, KinectInputTuningProfile profile)
    {
        if (analysis == null || profile == null || !analysis.hasValidFrame)
        {
            return false;
        }

        return !analysis.isTPose &&
               !analysis.isMostMuscularChargePose &&
               !analysis.isReleasePose &&
               analysis.leftHandSpeedMetersPerSecond <= profile.neutralAttackSpeedThreshold &&
               analysis.rightHandSpeedMetersPerSecond <= profile.neutralAttackSpeedThreshold &&
               analysis.leftArmExtensionNormalized <= profile.neutralChargeExtensionThreshold &&
               analysis.rightArmExtensionNormalized <= profile.neutralChargeExtensionThreshold &&
               analysis.leftElbowAngleDegrees >= profile.neutralChargeElbowThreshold &&
               analysis.rightElbowAngleDegrees >= profile.neutralChargeElbowThreshold &&
               Mathf.Abs(analysis.bodyLeanX) <= profile.neutralTurnAxisThreshold;
    }

    public PlayerInputState Evaluate(KinectGestureAnalysis analysis, KinectInputTuningProfile profile, float deltaTime, out KinectGestureDebugState debugState)
    {
        debugState = new KinectGestureDebugState();
        if (analysis == null || profile == null || !analysis.hasValidFrame)
        {
            return EvaluateInvalidFrame(out debugState);
        }

        float frameDeltaTime = Mathf.Max(0f, deltaTime);
        DecayAttackTimers(frameDeltaTime);
        UpdateMotionAcceleration(ref previousLeftHandSpeedMetersPerSecond, ref smoothedLeftHandAccelerationMetersPerSecondSquared, analysis.leftHandSpeedMetersPerSecond, profile.attackSmoothing, frameDeltaTime);
        UpdateMotionAcceleration(ref previousRightHandSpeedMetersPerSecond, ref smoothedRightHandAccelerationMetersPerSecondSquared, analysis.rightHandSpeedMetersPerSecond, profile.attackSmoothing, frameDeltaTime);
        analysis.leftHandAccelerationMetersPerSecondSquared = smoothedLeftHandAccelerationMetersPerSecondSquared;
        analysis.rightHandAccelerationMetersPerSecondSquared = smoothedRightHandAccelerationMetersPerSecondSquared;

        UpdateChargeState(analysis, profile, frameDeltaTime);
        UpdateTurnState(analysis, profile, frameDeltaTime);

        bool suppressAttack = analysis.isTPose || analysis.isMostMuscularChargePose || (chargeLatched && analysis.isReleasePose);
        bool leftCandidate = false;
        bool rightCandidate = false;
        bool leftTriggered = false;
        bool rightTriggered = false;
        float leftScore = 0f;
        float rightScore = 0f;
        float leftForwardDeltaMeters = 0f;
        float rightForwardDeltaMeters = 0f;

        if (suppressAttack)
        {
            leftAttackArmed = false;
            rightAttackArmed = false;
            leftAttackArmedBaselineForwardFromShoulder = 0f;
            rightAttackArmedBaselineForwardFromShoulder = 0f;
            leftAttackGestureLatched = false;
            rightAttackGestureLatched = false;
            CancelAttackPulse(ref leftAttackGestureLatched, ref leftAttackPulseRemaining);
            CancelAttackPulse(ref rightAttackGestureLatched, ref rightAttackPulseRemaining);
        }
        else
        {
            UpdateAttackArmedState(ref leftAttackArmed, ref leftAttackArmedBaselineForwardFromShoulder, ref leftAttackGestureLatched, analysis.leftHandForwardFromShoulderMeters, IsAttackArmedPose(analysis, profile, true));
            UpdateAttackArmedState(ref rightAttackArmed, ref rightAttackArmedBaselineForwardFromShoulder, ref rightAttackGestureLatched, analysis.rightHandForwardFromShoulderMeters, IsAttackArmedPose(analysis, profile, false));

            leftCandidate = TryEvaluateAttackCandidate(analysis, profile, true, leftAttackArmed, leftAttackArmedBaselineForwardFromShoulder, out leftForwardDeltaMeters, out leftScore);
            rightCandidate = TryEvaluateAttackCandidate(analysis, profile, false, rightAttackArmed, rightAttackArmedBaselineForwardFromShoulder, out rightForwardDeltaMeters, out rightScore);

            if (leftCandidate && rightCandidate)
            {
                if (leftScore >= rightScore)
                {
                    rightCandidate = false;
                }
                else
                {
                    leftCandidate = false;
                }
            }

            if (leftCandidate)
            {
                leftTriggered = TryStartAttackPulse(ref leftAttackGestureLatched, leftCandidate, ref leftAttackPulseRemaining, ref leftAttackCooldownRemaining, profile.attackPulseSeconds, profile.attackCooldownSeconds);
            }

            if (rightCandidate)
            {
                rightTriggered = TryStartAttackPulse(ref rightAttackGestureLatched, rightCandidate, ref rightAttackPulseRemaining, ref rightAttackCooldownRemaining, profile.attackPulseSeconds, profile.attackCooldownSeconds);
            }
        }

        analysis.leftAttackCandidate = leftCandidate;
        analysis.rightAttackCandidate = rightCandidate;
        analysis.leftAttackTriggered = leftTriggered;
        analysis.rightAttackTriggered = rightTriggered;
        analysis.leftAttackScore = leftScore;
        analysis.rightAttackScore = rightScore;

        debugState.armed = leftAttackArmed || rightAttackArmed || chargeLatched;
        debugState.chargeLatched = chargeLatched;
        debugState.neutralStableSeconds = chargeLatched ? chargeReleaseStableSeconds : chargeEnterStableSeconds;
        debugState.bodyLeanX = analysis.bodyLeanX;
        debugState.rawTurnAxis = rawTurnSignal;
        debugState.smoothedTurnAxis = smoothedTurnSignal;
        debugState.isTPose = analysis.isTPose;
        debugState.isTurnLeft = currentTurnAxis < 0f;
        debugState.isTurnRight = currentTurnAxis > 0f;
        debugState.isMostMuscularChargePose = analysis.isMostMuscularChargePose;
        debugState.isReleasePose = analysis.isReleasePose;
        debugState.leftAttackPulseActive = leftAttackPulseRemaining > 0f;
        debugState.rightAttackPulseActive = rightAttackPulseRemaining > 0f;
        debugState.leftAttackPulseRemaining = leftAttackPulseRemaining;
        debugState.rightAttackPulseRemaining = rightAttackPulseRemaining;
        debugState.leftAttackCooldownRemaining = leftAttackCooldownRemaining;
        debugState.rightAttackCooldownRemaining = rightAttackCooldownRemaining;
        debugState.leftAttackCandidate = analysis.leftAttackCandidate;
        debugState.rightAttackCandidate = analysis.rightAttackCandidate;
        debugState.leftAttackTriggered = analysis.leftAttackTriggered;
        debugState.rightAttackTriggered = analysis.rightAttackTriggered;
        debugState.leftAttackScore = analysis.leftAttackScore;
        debugState.rightAttackScore = analysis.rightAttackScore;
        debugState.leftArmExtensionNormalized = analysis.leftArmExtensionNormalized;
        debugState.rightArmExtensionNormalized = analysis.rightArmExtensionNormalized;
        debugState.leftHandSpeedMetersPerSecond = analysis.leftHandSpeedMetersPerSecond;
        debugState.rightHandSpeedMetersPerSecond = analysis.rightHandSpeedMetersPerSecond;
        debugState.leftHandAccelerationMetersPerSecondSquared = analysis.leftHandAccelerationMetersPerSecondSquared;
        debugState.rightHandAccelerationMetersPerSecondSquared = analysis.rightHandAccelerationMetersPerSecondSquared;
        debugState.leftHandTowardKinectSpeedMetersPerSecond = analysis.leftHandTowardKinectSpeedMetersPerSecond;
        debugState.rightHandTowardKinectSpeedMetersPerSecond = analysis.rightHandTowardKinectSpeedMetersPerSecond;
        debugState.leftHandForwardFromShoulderMeters = analysis.leftHandForwardFromShoulderMeters;
        debugState.rightHandForwardFromShoulderMeters = analysis.rightHandForwardFromShoulderMeters;
        debugState.leftAttackArmedForwardFromShoulderMeters = leftAttackArmedBaselineForwardFromShoulder;
        debugState.rightAttackArmedForwardFromShoulderMeters = rightAttackArmedBaselineForwardFromShoulder;
        debugState.leftAttackForwardDeltaMeters = leftForwardDeltaMeters;
        debugState.rightAttackForwardDeltaMeters = rightForwardDeltaMeters;
        debugState.leftAttackArmed = leftAttackArmed;
        debugState.rightAttackArmed = rightAttackArmed;
        debugState.leftElbowAngleDegrees = analysis.leftElbowAngleDegrees;
        debugState.rightElbowAngleDegrees = analysis.rightElbowAngleDegrees;

        return new PlayerInputState(currentTurnAxis, chargeLatched, leftAttackPulseRemaining > 0f, rightAttackPulseRemaining > 0f);
    }

    private void DecayAttackTimers(float deltaTime)
    {
        if (leftAttackPulseRemaining > 0f)
        {
            leftAttackPulseRemaining = Mathf.Max(0f, leftAttackPulseRemaining - deltaTime);
        }

        if (rightAttackPulseRemaining > 0f)
        {
            rightAttackPulseRemaining = Mathf.Max(0f, rightAttackPulseRemaining - deltaTime);
        }

        if (leftAttackCooldownRemaining > 0f)
        {
            leftAttackCooldownRemaining = Mathf.Max(0f, leftAttackCooldownRemaining - deltaTime);
        }

        if (rightAttackCooldownRemaining > 0f)
        {
            rightAttackCooldownRemaining = Mathf.Max(0f, rightAttackCooldownRemaining - deltaTime);
        }
    }

    private void UpdateMotionAcceleration(ref float previousSpeedMetersPerSecond, ref float smoothedAccelerationMetersPerSecondSquared, float currentSpeedMetersPerSecond, float smoothing, float deltaTime)
    {
        float rawAcceleration = 0f;
        if (hasMotionSample && deltaTime > 0f)
        {
            rawAcceleration = (currentSpeedMetersPerSecond - previousSpeedMetersPerSecond) / deltaTime;
        }

        float lerpFactor = Mathf.Clamp01(smoothing);
        smoothedAccelerationMetersPerSecondSquared = Mathf.Lerp(smoothedAccelerationMetersPerSecondSquared, rawAcceleration, lerpFactor);
        previousSpeedMetersPerSecond = currentSpeedMetersPerSecond;
        hasMotionSample = true;
    }

    private void UpdateTurnState(KinectGestureAnalysis analysis, KinectInputTuningProfile profile, float deltaTime)
    {
        if (!analysis.isTPose)
        {
            rawTurnSignal = 0f;
            smoothedTurnSignal = 0f;
            currentTurnAxis = 0f;
            return;
        }

        rawTurnSignal = ComputeTurnSignal(analysis.bodyLeanX, profile);
        smoothedTurnSignal = SmoothTurnSignal(smoothedTurnSignal, rawTurnSignal, profile.turnSmoothing, deltaTime);

        if (currentTurnAxis < 0f)
        {
            if (smoothedTurnSignal >= profile.turnEnterLeanThreshold)
            {
                currentTurnAxis = 1f;
            }
            else if (smoothedTurnSignal > -profile.turnExitLeanThreshold)
            {
                currentTurnAxis = 0f;
            }
        }
        else if (currentTurnAxis > 0f)
        {
            if (smoothedTurnSignal <= -profile.turnEnterLeanThreshold)
            {
                currentTurnAxis = -1f;
            }
            else if (smoothedTurnSignal < profile.turnExitLeanThreshold)
            {
                currentTurnAxis = 0f;
            }
        }
        else
        {
            if (smoothedTurnSignal <= -profile.turnEnterLeanThreshold)
            {
                currentTurnAxis = -1f;
            }
            else if (smoothedTurnSignal >= profile.turnEnterLeanThreshold)
            {
                currentTurnAxis = 1f;
            }
        }
    }

    private void UpdateChargeState(KinectGestureAnalysis analysis, KinectInputTuningProfile profile, float deltaTime)
    {
        if (chargeLatched)
        {
            if (analysis.isReleasePose)
            {
                chargeReleaseStableSeconds += deltaTime;
                if (chargeReleaseStableSeconds >= profile.chargeReleaseHoldSeconds)
                {
                    chargeLatched = false;
                    chargeReleaseStableSeconds = 0f;
                    chargeEnterStableSeconds = 0f;
                }
            }
            else
            {
                chargeReleaseStableSeconds = 0f;
            }

            return;
        }

        if (analysis.isMostMuscularChargePose)
        {
            chargeEnterStableSeconds += deltaTime;
            if (chargeEnterStableSeconds >= profile.chargeEnterHoldSeconds)
            {
                chargeLatched = true;
                chargeEnterStableSeconds = 0f;
                chargeReleaseStableSeconds = 0f;
            }
        }
        else
        {
            chargeEnterStableSeconds = 0f;
        }
    }

    private static void CancelAttackPulse(ref bool gestureLatched, ref float pulseRemaining)
    {
        gestureLatched = false;
        pulseRemaining = 0f;
    }

    private static void UpdateAttackArmedState(ref bool attackArmed, ref float armedBaselineForwardFromShoulder, ref bool sessionLatched, float currentForwardFromShoulder, bool isArmedPose)
    {
        if (isArmedPose)
        {
            if (!attackArmed)
            {
                attackArmed = true;
                armedBaselineForwardFromShoulder = currentForwardFromShoulder;
                sessionLatched = false;
            }
            else if (sessionLatched)
            {
                armedBaselineForwardFromShoulder = currentForwardFromShoulder;
                sessionLatched = false;
            }

            return;
        }
    }

    private static bool TryStartAttackPulse(ref bool sessionLatched, bool candidate, ref float pulseRemaining, ref float cooldownRemaining, float pulseSeconds, float cooldownSeconds)
    {
        if (!candidate || sessionLatched || pulseRemaining > 0f || cooldownRemaining > 0f)
        {
            return false;
        }

        sessionLatched = true;
        pulseRemaining = pulseSeconds;
        cooldownRemaining = cooldownSeconds;
        return true;
    }

    private static bool IsAttackArmedPose(KinectGestureAnalysis analysis, KinectInputTuningProfile profile, bool leftSide)
    {
        float currentForwardFromShoulder = leftSide ? analysis.leftHandForwardFromShoulderMeters : analysis.rightHandForwardFromShoulderMeters;
        float handNormalizedY = leftSide ? analysis.leftHandNormalizedY : analysis.rightHandNormalizedY;
        float shoulderNormalizedY = leftSide ? analysis.leftShoulderNormalizedY : analysis.rightShoulderNormalizedY;
        float handHeightOffset = handNormalizedY - shoulderNormalizedY;
        float armExtension = leftSide ? analysis.leftArmExtensionNormalized : analysis.rightArmExtensionNormalized;

        return currentForwardFromShoulder <= profile.attackArmedForwardMaxMeters &&
               handHeightOffset >= profile.attackArmedHandHeightMinOffset &&
               handHeightOffset <= profile.attackArmedHandHeightMaxOffset &&
               armExtension <= profile.attackArmedExtensionThreshold;
    }

    private static bool TryEvaluateAttackCandidate(KinectGestureAnalysis analysis, KinectInputTuningProfile profile, bool leftSide, bool attackArmed, float armedBaselineForwardFromShoulder, out float forwardDeltaMeters, out float score)
    {
        float currentForwardFromShoulder = leftSide ? analysis.leftHandForwardFromShoulderMeters : analysis.rightHandForwardFromShoulderMeters;
        float armExtension = leftSide ? analysis.leftArmExtensionNormalized : analysis.rightArmExtensionNormalized;
        float elbowAngle = leftSide ? analysis.leftElbowAngleDegrees : analysis.rightElbowAngleDegrees;
        float auxSpeed = leftSide ? analysis.leftHandSpeedMetersPerSecond : analysis.rightHandSpeedMetersPerSecond;
        float auxTowardKinectSpeed = leftSide ? analysis.leftHandTowardKinectSpeedMetersPerSecond : analysis.rightHandTowardKinectSpeedMetersPerSecond;

        forwardDeltaMeters = currentForwardFromShoulder - armedBaselineForwardFromShoulder;
        score = ComputeAttackScore(forwardDeltaMeters, armExtension, elbowAngle, auxSpeed, auxTowardKinectSpeed, profile);

        if (!attackArmed)
        {
            return false;
        }

        if (forwardDeltaMeters < profile.attackForwardDeltaThresholdMeters)
        {
            return false;
        }

        bool stretchedEnough = armExtension >= profile.attackExtensionThreshold || elbowAngle >= profile.attackElbowAngleThresholdDegrees;
        if (!stretchedEnough)
        {
            return false;
        }

        return true;
    }

    private static float ComputeAttackScore(float forwardDeltaMeters, float armExtensionNormalized, float elbowAngleDegrees, float handSpeedMetersPerSecond, float handTowardKinectSpeedMetersPerSecond, KinectInputTuningProfile profile)
    {
        float forwardThreshold = Mathf.Max(0.0001f, profile.attackForwardDeltaThresholdMeters);
        float speedThreshold = Mathf.Max(0.0001f, profile.attackSpeedThreshold);
        float forwardScore = Mathf.Max(0f, forwardDeltaMeters / forwardThreshold);
        float extensionScore = Mathf.Max(0f, armExtensionNormalized / Mathf.Max(0.0001f, profile.attackExtensionThreshold));
        float elbowScore = Mathf.Max(0f, (elbowAngleDegrees - profile.attackElbowAngleThresholdDegrees) / Mathf.Max(1f, 180f - profile.attackElbowAngleThresholdDegrees));
        float speedScore = Mathf.Max(0f, handSpeedMetersPerSecond / speedThreshold);
        float towardScore = Mathf.Max(0f, handTowardKinectSpeedMetersPerSecond / speedThreshold);
        return forwardScore + extensionScore + elbowScore + (0.25f * speedScore) + (0.25f * towardScore);
    }

    private static bool IsTPoseCandidate(KinectGestureAnalysis analysis, KinectInputTuningProfile profile)
    {
        bool leftHandAtShoulderHeight = Mathf.Abs(analysis.leftHandNormalizedY - analysis.leftShoulderNormalizedY) <= profile.tPoseHandHeightTolerance;
        bool rightHandAtShoulderHeight = Mathf.Abs(analysis.rightHandNormalizedY - analysis.rightShoulderNormalizedY) <= profile.tPoseHandHeightTolerance;
        bool leftHandExtended = analysis.leftArmExtensionNormalized >= profile.tPoseSideExtensionThreshold;
        bool rightHandExtended = analysis.rightArmExtensionNormalized >= profile.tPoseSideExtensionThreshold;
        return leftHandAtShoulderHeight && rightHandAtShoulderHeight && leftHandExtended && rightHandExtended;
    }

    private static bool IsChargePoseCandidate(KinectGestureAnalysis analysis, KinectInputTuningProfile profile)
    {
        bool leftHandInBand = analysis.leftHandNormalizedY >= analysis.leftShoulderNormalizedY + profile.chargeHandMinY &&
                              analysis.leftHandNormalizedY <= analysis.leftShoulderNormalizedY + profile.chargeHandMaxY;
        bool rightHandInBand = analysis.rightHandNormalizedY >= analysis.rightShoulderNormalizedY + profile.chargeHandMinY &&
                               analysis.rightHandNormalizedY <= analysis.rightShoulderNormalizedY + profile.chargeHandMaxY;
        bool leftBent = analysis.leftElbowAngleDegrees <= profile.chargeElbowAngleThreshold;
        bool rightBent = analysis.rightElbowAngleDegrees <= profile.chargeElbowAngleThreshold;
        bool leftCompact = analysis.leftArmExtensionNormalized <= profile.chargeEnterExtensionThreshold;
        bool rightCompact = analysis.rightArmExtensionNormalized <= profile.chargeEnterExtensionThreshold;
        return leftHandInBand && rightHandInBand && leftBent && rightBent && leftCompact && rightCompact;
    }

    private static bool IsReleasePoseCandidate(KinectGestureAnalysis analysis, KinectInputTuningProfile profile)
    {
        bool leftRaised = analysis.leftHandNormalizedY >= analysis.leftShoulderNormalizedY + profile.releaseHandAboveHeadThreshold;
        bool rightRaised = analysis.rightHandNormalizedY >= analysis.rightShoulderNormalizedY + profile.releaseHandAboveHeadThreshold;
        bool leftOpen = analysis.leftArmExtensionNormalized >= profile.chargeReleaseExtensionThreshold;
        bool rightOpen = analysis.rightArmExtensionNormalized >= profile.chargeReleaseExtensionThreshold;
        bool leftOpenElbow = analysis.leftElbowAngleDegrees >= profile.chargeReleaseElbowAngleThreshold;
        bool rightOpenElbow = analysis.rightElbowAngleDegrees >= profile.chargeReleaseElbowAngleThreshold;
        return leftRaised && rightRaised && leftOpen && rightOpen && leftOpenElbow && rightOpenElbow;
    }

    private static float ComputeTurnSignal(float bodyLeanX, KinectInputTuningProfile profile)
    {
        float scaledLean = bodyLeanX * Mathf.Max(0f, profile.turnSensitivity);
        if (Mathf.Abs(scaledLean) <= profile.turnDeadZone)
        {
            return 0f;
        }

        return Mathf.Clamp(scaledLean, -1f, 1f);
    }

    private static float SmoothTurnSignal(float currentSignal, float targetSignal, float smoothing, float deltaTime)
    {
        if (deltaTime <= 0f || smoothing <= 0f)
        {
            return targetSignal;
        }

        float lerpFactor = 1f - Mathf.Exp(-smoothing * deltaTime);
        return Mathf.Lerp(currentSignal, targetSignal, lerpFactor);
    }

    private static float ComputeBodyLeanX(KinectTrackingFrame frame)
    {
        KinectTrackingJointCollection joints = frame.NormalizedJoints;
        if (joints == null || joints.shoulderLeft == null || joints.shoulderRight == null)
        {
            return 0f;
        }

        return joints.shoulderLeft.y - joints.shoulderRight.y;
    }

    private static float ComputeHandForwardFromShoulder(KinectTrackingFrame frame, bool leftSide)
    {
        KinectTrackingJointCollection rawJoints = frame.RawJoints;
        if (rawJoints == null)
        {
            return 0f;
        }

        KinectTrackingJointSample shoulder = leftSide ? rawJoints.shoulderLeft : rawJoints.shoulderRight;
        KinectTrackingJointSample hand = leftSide ? rawJoints.handLeft : rawJoints.handRight;
        if (shoulder == null || hand == null)
        {
            return 0f;
        }

        return shoulder.z - hand.z;
    }

    private static float GetLeftArmExtension(KinectTrackingFrame frame)
    {
        if (frame.Metrics == null)
        {
            return 0f;
        }

        return frame.Metrics.leftArmExtensionNormalized;
    }

    private static float GetRightArmExtension(KinectTrackingFrame frame)
    {
        if (frame.Metrics == null)
        {
            return 0f;
        }

        return frame.Metrics.rightArmExtensionNormalized;
    }

    private static float GetHandNormalizedY(KinectTrackingFrame frame, bool leftSide)
    {
        KinectTrackingJointCollection joints = frame.NormalizedJoints;
        if (joints == null)
        {
            return 0f;
        }

        KinectTrackingJointSample hand = leftSide ? joints.handLeft : joints.handRight;
        return hand == null ? 0f : hand.y;
    }

    private static float GetShoulderNormalizedY(KinectTrackingFrame frame, bool leftSide)
    {
        KinectTrackingJointCollection joints = frame.NormalizedJoints;
        if (joints == null)
        {
            return 0f;
        }

        KinectTrackingJointSample shoulder = leftSide ? joints.shoulderLeft : joints.shoulderRight;
        return shoulder == null ? 0f : shoulder.y;
    }

    private static float GetShoulderCenterNormalizedY(KinectTrackingFrame frame)
    {
        KinectTrackingJointCollection joints = frame.NormalizedJoints;
        if (joints == null || joints.shoulderCenter == null)
        {
            return 0f;
        }

        return joints.shoulderCenter.y;
    }
}
