using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerInputProviderTests
{
    private sealed class FakeKeyQuery : IPlayerKeyQuery
    {
        private readonly System.Collections.Generic.Dictionary<KeyCode, bool> keys = new System.Collections.Generic.Dictionary<KeyCode, bool>();

        public void Set(KeyCode key, bool value)
        {
            keys[key] = value;
        }

        public bool GetKey(KeyCode key)
        {
            bool value;
            return keys.TryGetValue(key, out value) && value;
        }
    }

    private sealed class FakeProviderComponent : MonoBehaviour, IPlayerInputProvider
    {
        public PlayerInputState state;
        public bool available = true;
        public string providerName = "Fake";

        public string ProviderName
        {
            get { return providerName; }
        }

        public bool IsAvailable
        {
            get { return available; }
        }

        public PlayerInputState ReadState()
        {
            return state;
        }
    }

    private sealed class FakeProvider : IPlayerInputProvider
    {
        private readonly PlayerInputState state;
        private readonly bool available;

        public FakeProvider(PlayerInputState state, bool available = true)
        {
            this.state = state;
            this.available = available;
        }

        public string ProviderName
        {
            get { return "Fake"; }
        }

        public bool IsAvailable
        {
            get { return available; }
        }

        public PlayerInputState ReadState()
        {
            return state;
        }
    }

    [Test]
    public void ResolveTurnAxis_ReturnsZeroWhenBothOrNeitherAreHeld()
    {
        Assert.AreEqual(0f, PlayerInputMapping.ResolveTurnAxis(false, false));
        Assert.AreEqual(0f, PlayerInputMapping.ResolveTurnAxis(true, true));
        Assert.AreEqual(-1f, PlayerInputMapping.ResolveTurnAxis(true, false));
        Assert.AreEqual(1f, PlayerInputMapping.ResolveTurnAxis(false, true));
    }

    [Test]
    public void KeyboardInputProvider_MapsKeyboardState()
    {
        FakeKeyQuery keys = new FakeKeyQuery();
        keys.Set(KeyCode.A, true);
        keys.Set(KeyCode.RightArrow, true);
        keys.Set(KeyCode.Space, true);
        keys.Set(KeyCode.J, true);

        KeyboardInputProvider provider = new KeyboardInputProvider(keys);
        PlayerInputState state = provider.ReadState();

        Assert.AreEqual(0f, state.turnAxis);
        Assert.IsTrue(state.chargeHeld);
        Assert.IsTrue(state.leftAttackHeld);
        Assert.IsFalse(state.rightAttackHeld);
    }

    [Test]
    public void DummyInputProvider_ReturnsConfiguredState()
    {
        PlayerInputDebugConfig config = new PlayerInputDebugConfig
        {
            turnAxis = 1f,
            chargeHeld = true,
            leftAttackHeld = false,
            rightAttackHeld = true
        };

        DummyInputProvider provider = new DummyInputProvider(config);
        PlayerInputState state = provider.ReadState();

        Assert.AreEqual(1f, state.turnAxis);
        Assert.IsTrue(state.chargeHeld);
        Assert.IsFalse(state.leftAttackHeld);
        Assert.IsTrue(state.rightAttackHeld);
    }

    [Test]
    public void PlayerInputSourceHost_UsesAvailableKinectProvider()
    {
        GameObject go = new GameObject("InputHostTest");
        try
        {
            PlayerInputSourceHost host = go.AddComponent<PlayerInputSourceHost>();
            FakeProviderComponent provider = go.AddComponent<FakeProviderComponent>();
            provider.state = new PlayerInputState(-1f, true, true, false);
            host.inputSource = PlayerInputSourceKind.KinectBridge;
            host.kinectBridgeReceiverProvider = provider;

            PlayerInputState state = host.ReadState();

            Assert.AreEqual(-1f, state.turnAxis);
            Assert.IsTrue(state.chargeHeld);
            Assert.IsTrue(state.leftAttackHeld);
            Assert.IsFalse(state.rightAttackHeld);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void KinectBridgeReceiverInputProvider_StaysAvailableWhenTrackedFrameCannotBeNormalized()
    {
        GameObject go = new GameObject("KinectProviderAvailabilityTest");
        try
        {
            KinectBridgeReceiver receiver = go.AddComponent<KinectBridgeReceiver>();
            KinectBridgeReceiverInputProvider provider = go.AddComponent<KinectBridgeReceiverInputProvider>();
            KinectInputTuningProfile profile = CreateKinectProfile();
            provider.Configure(receiver, profile, false);

            SeedConnectedTrackedFrame(receiver);

            Assert.IsTrue(receiver.IsConnected);
            Assert.IsNotNull(receiver.LatestFrame);
            Assert.IsTrue(receiver.LatestFrame.Tracked);
            Assert.IsFalse(receiver.LatestFrame.IsNormalizedValid);

            Assert.IsTrue(provider.IsAvailable);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PlayerInputSourceHost_FallsBackToKeyboardOnceWhenKinectProviderIsMissing()
    {
        GameObject go = new GameObject("InputHostFallbackTest");
        try
        {
            PlayerInputSourceHost host = go.AddComponent<PlayerInputSourceHost>();
            host.inputSource = PlayerInputSourceKind.KinectBridge;
            host.keyboardFallbackOverride = new FakeProvider(new PlayerInputState(1f, true, false, true));

            LogAssert.Expect(LogType.Warning, "[PlayerInput] Selected input source is unavailable. Falling back to Keyboard.");
            PlayerInputState first = host.ReadState();
            PlayerInputState second = host.ReadState();

            Assert.AreEqual(1f, first.turnAxis);
            Assert.IsTrue(first.chargeHeld);
            Assert.IsFalse(first.leftAttackHeld);
            Assert.IsTrue(first.rightAttackHeld);

            Assert.AreEqual(1f, second.turnAxis);
            Assert.IsTrue(second.chargeHeld);
            Assert.IsFalse(second.leftAttackHeld);
            Assert.IsTrue(second.rightAttackHeld);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void KinectGestureMapper_HoldsChargeFromChestPose()
    {
        KinectInputTuningProfile profile = CreateKinectProfile();
        profile.chargeEnterHoldSeconds = 0.05f;
        profile.chargeReleaseHoldSeconds = 0.05f;
        profile.chargeEnterExtensionThreshold = 0.95f;
        profile.chargeElbowAngleThreshold = 125f;
        profile.chargeHandMinY = 0.15f;
        profile.chargeHandMaxY = 0.75f;

        KinectGestureMapper mapper = new KinectGestureMapper();
        try
        {
            KinectTrackingFrame frame = CreateTrackedFrame(0.05f, 0.05f, 0.78f, 0.80f, 104f, 102f, 0.25f, 0.28f);

            KinectGestureDebugState debugState;
            PlayerInputState state = mapper.Evaluate(mapper.Analyze(frame, profile), profile, 0.06f, out debugState);

            Assert.IsTrue(state.chargeHeld);
            Assert.IsFalse(state.leftAttackHeld);
            Assert.IsFalse(state.rightAttackHeld);
            Assert.AreEqual(0f, state.turnAxis);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    [Test]
    public void KinectGestureMapper_TurnsInTPoseWithHysteresis()
    {
        KinectInputTuningProfile profile = CreateKinectProfile();
        profile.turnEnterLeanThreshold = 0.25f;
        profile.turnExitLeanThreshold = 0.15f;
        profile.turnSmoothing = 0f;
        profile.tPoseHandHeightTolerance = 0.10f;
        profile.tPoseSideExtensionThreshold = 0.85f;

        KinectGestureMapper mapper = new KinectGestureMapper();
        try
        {
            KinectGestureDebugState debugState;

            PlayerInputState centered = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0f, 0f, 1.05f, 1.05f, 170f, 170f, 0.02f, 0.02f, 0f), profile), profile, 0.06f, out debugState);
            PlayerInputState left = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0f, 0f, 1.05f, 1.05f, 170f, 170f, 0.02f, 0.02f, -0.12f), profile), profile, 0.06f, out debugState);
            PlayerInputState returnedCenter = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0f, 0f, 1.05f, 1.05f, 170f, 170f, 0.02f, 0.02f, 0f), profile), profile, 0.06f, out debugState);
            PlayerInputState right = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0f, 0f, 1.05f, 1.05f, 170f, 170f, 0.02f, 0.02f, 0.12f), profile), profile, 0.06f, out debugState);

            Assert.AreEqual(0f, centered.turnAxis);
            Assert.AreEqual(-1f, left.turnAxis);
            Assert.AreEqual(0f, returnedCenter.turnAxis);
            Assert.AreEqual(1f, right.turnAxis);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    [Test]
    public void KinectGestureMapper_TriggersSingleAttackPulseFromForwardStrike()
    {
        KinectInputTuningProfile profile = CreateKinectProfile();
        profile.attackSmoothing = 0.35f;
        profile.attackPulseSeconds = 0.12f;
        profile.attackCooldownSeconds = 0.06f;
        profile.attackArmedForwardMaxMeters = 0.12f;
        profile.attackArmedHandHeightMinOffset = -0.20f;
        profile.attackArmedHandHeightMaxOffset = 0.20f;
        profile.attackArmedExtensionThreshold = 1.05f;
        profile.attackForwardDeltaThresholdMeters = 0.20f;
        profile.attackExtensionThreshold = 1.00f;
        profile.attackElbowAngleThresholdDegrees = 145f;
        profile.attackSpeedThreshold = 0.45f;

        KinectGestureMapper mapper = new KinectGestureMapper();
        try
        {
            KinectGestureDebugState armedDebug;
            KinectGestureDebugState strikeDebug;
            KinectGestureDebugState repeatedStrikeDebug;
            KinectGestureDebugState rearmDebug;
            KinectGestureDebugState secondStrikeDebug;
            PlayerInputState armed = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0.00f, 0.00f, 0.80f, 0.78f, 165f, 150f, 0.10f, 0.12f, 0f, 2.05f, 2.05f, 2.05f, 2.05f), profile), profile, 0.06f, out armedDebug);
            PlayerInputState strike = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0.00f, 0.00f, 1.20f, 1.10f, 165f, 150f, 0.30f, 0.32f, 0f, 2.05f, 2.05f, 1.75f, 1.78f), profile), profile, 0.06f, out strikeDebug);
            PlayerInputState repeatedStrike = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0.00f, 0.00f, 1.20f, 1.10f, 165f, 150f, 0.30f, 0.32f, 0f, 2.05f, 2.05f, 1.75f, 1.78f), profile), profile, 0.06f, out repeatedStrikeDebug);
            PlayerInputState rearmed = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0.00f, 0.00f, 0.80f, 0.78f, 165f, 150f, 0.10f, 0.12f, 0f, 2.05f, 2.05f, 2.05f, 2.05f), profile), profile, 0.06f, out rearmDebug);
            PlayerInputState secondStrike = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0.00f, 0.00f, 1.20f, 1.10f, 165f, 150f, 0.30f, 0.32f, 0f, 2.05f, 2.05f, 1.75f, 1.78f), profile), profile, 0.06f, out secondStrikeDebug);

            Assert.IsFalse(armed.leftAttackHeld);
            Assert.IsTrue(strike.leftAttackHeld);
            Assert.IsFalse(strike.rightAttackHeld);
            Assert.IsFalse(repeatedStrikeDebug.leftAttackTriggered);
            Assert.IsFalse(repeatedStrikeDebug.rightAttackTriggered);
            Assert.IsFalse(rearmed.leftAttackHeld);
            Assert.IsTrue(secondStrikeDebug.leftAttackTriggered);
            Assert.IsFalse(secondStrikeDebug.rightAttackTriggered);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    [Test]
    public void KinectGestureMapper_PreservesChargeDuringInvalidFrameWithoutIntentionalRelease()
    {
        KinectInputTuningProfile profile = CreateKinectProfile();
        profile.chargeEnterHoldSeconds = 0.05f;
        profile.chargeReleaseHoldSeconds = 0.05f;
        profile.chargeEnterExtensionThreshold = 0.95f;
        profile.chargeReleaseExtensionThreshold = 1.10f;
        profile.chargeElbowAngleThreshold = 125f;
        profile.chargeReleaseElbowAngleThreshold = 135f;
        profile.chargeHandMinY = 0.15f;
        profile.chargeHandMaxY = 0.75f;
        profile.releaseHandAboveHeadThreshold = 0.10f;

        KinectGestureMapper mapper = new KinectGestureMapper();
        try
        {
            KinectGestureDebugState debugState;
            PlayerInputState charged = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0f, 0f, 0.82f, 0.84f, 104f, 102f, 0.25f, 0.27f, 0f, 2.05f, 2.05f, 1.90f, 1.90f), profile), profile, 0.06f, out debugState);
            PlayerInputState invalid = mapper.EvaluateInvalidFrame(out debugState);
            PlayerInputState release = mapper.Evaluate(mapper.Analyze(CreateTrackedFrame(0f, 0f, 1.20f, 1.18f, 145f, 142f, 0.22f, 0.24f, 0f, 2.05f, 2.05f, 1.75f, 1.78f), profile), profile, 0.06f, out debugState);

            Assert.IsTrue(charged.chargeHeld);
            Assert.IsTrue(invalid.chargeHeld);
            Assert.IsFalse(invalid.leftAttackHeld);
            Assert.IsFalse(invalid.rightAttackHeld);
            Assert.IsFalse(release.chargeHeld);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    private static KinectInputTuningProfile CreateKinectProfile()
    {
        KinectInputTuningProfile profile = ScriptableObject.CreateInstance<KinectInputTuningProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        return profile;
    }

    private static KinectTrackingFrame CreateTrackedFrame(float leftHandTowardKinectSpeed, float rightHandTowardKinectSpeed, float leftArmExtension, float rightArmExtension, float leftElbowAngle, float rightElbowAngle, float leftHandY, float rightHandY, float shoulderCenterOffsetX = 0f, float leftShoulderZ = 2.1f, float rightShoulderZ = 2.1f, float leftHandZ = 1.8f, float rightHandZ = 1.8f, float leftHandSpeedMetersPerSecond = -1f, float rightHandSpeedMetersPerSecond = -1f)
    {
        return new KinectTrackingFrame
        {
            SessionId = Guid.NewGuid().ToString("D"),
            FrameId = 1,
            TimestampMs = 1000,
            Tracked = true,
            TrackingId = 123,
            IsNormalizedValid = true,
            ShoulderWidthMeters = 0.4f,
            ShoulderCenter = Joint(shoulderCenterOffsetX, 0.0f, 0.0f),
            NormalizedJoints = CreateNormalizedJoints(leftHandY, rightHandY, shoulderCenterOffsetX),
            RawJoints = CreateRawJoints(shoulderCenterOffsetX, leftShoulderZ, rightShoulderZ, leftHandZ, rightHandZ),
            Metrics = new KinectTrackingMetrics
            {
                leftHandSpeedMetersPerSecond = leftHandSpeedMetersPerSecond >= 0f ? leftHandSpeedMetersPerSecond : leftHandTowardKinectSpeed,
                rightHandSpeedMetersPerSecond = rightHandSpeedMetersPerSecond >= 0f ? rightHandSpeedMetersPerSecond : rightHandTowardKinectSpeed,
                leftHandTowardKinectSpeedMetersPerSecond = leftHandTowardKinectSpeed,
                rightHandTowardKinectSpeedMetersPerSecond = rightHandTowardKinectSpeed,
                leftArmExtensionNormalized = leftArmExtension,
                rightArmExtensionNormalized = rightArmExtension,
                leftElbowAngleDegrees = leftElbowAngle,
                rightElbowAngleDegrees = rightElbowAngle,
                leftHandForwardFromShoulderMeters = leftShoulderZ - leftHandZ,
                rightHandForwardFromShoulderMeters = rightShoulderZ - rightHandZ
            }
        };
    }

    private static KinectTrackingJointCollection CreateNormalizedJoints(float leftHandY, float rightHandY, float shoulderCenterOffsetX)
    {
        return new KinectTrackingJointCollection
        {
            shoulderCenter = Joint(shoulderCenterOffsetX, 0.0f, 0.0f),
            spine = Joint(shoulderCenterOffsetX, 0.0f, 0.0f),
            hipCenter = Joint(0.0f, 0.0f, 0.0f),
            shoulderLeft = Joint(-0.2f, 0.0f, 0.0f),
            elbowLeft = Joint(-0.15f, 0.05f, 0.02f),
            wristLeft = Joint(-0.12f, leftHandY * 0.75f, 0.03f),
            handLeft = Joint(-0.10f, leftHandY, 0.04f),
            shoulderRight = Joint(0.2f, 0.0f, 0.0f),
            elbowRight = Joint(0.15f, 0.05f, 0.02f),
            wristRight = Joint(0.12f, rightHandY * 0.75f, 0.03f),
            handRight = Joint(0.10f, rightHandY, 0.04f)
        };
    }

    private static KinectTrackingJointCollection CreateRawJoints(float shoulderCenterOffsetX, float leftShoulderZ, float rightShoulderZ, float leftHandZ, float rightHandZ)
    {
        return new KinectTrackingJointCollection
        {
            shoulderCenter = Joint(shoulderCenterOffsetX, 1.5f, 2.1f),
            spine = Joint(shoulderCenterOffsetX, 1.3f, 2.1f),
            hipCenter = Joint(0.0f, 1.0f, 2.1f),
            shoulderLeft = Joint(-0.2f, 1.4f, leftShoulderZ),
            elbowLeft = Joint(-0.15f, 1.3f, leftShoulderZ - 0.10f),
            wristLeft = Joint(-0.12f, 1.2f, leftHandZ + 0.10f),
            handLeft = Joint(-0.10f, 1.1f, leftHandZ),
            shoulderRight = Joint(0.2f, 1.4f, rightShoulderZ),
            elbowRight = Joint(0.15f, 1.3f, rightShoulderZ - 0.10f),
            wristRight = Joint(0.12f, 1.2f, rightHandZ + 0.10f),
            handRight = Joint(0.10f, 1.1f, rightHandZ)
        };
    }

    private static KinectTrackingJointCollection CreateInvalidNormalizationJoints()
    {
        return new KinectTrackingJointCollection
        {
            shoulderLeft = Joint(0.0f, 1.4f, 2.1f),
            elbowLeft = Joint(0.01f, 1.3f, 2.0f),
            wristLeft = Joint(0.02f, 1.2f, 1.9f),
            handLeft = Joint(0.03f, 1.1f, 1.8f),
            shoulderRight = Joint(0.01f, 1.4f, 2.1f),
            elbowRight = Joint(0.02f, 1.3f, 2.0f),
            wristRight = Joint(0.03f, 1.2f, 1.9f),
            handRight = Joint(0.04f, 1.1f, 1.8f)
        };
    }

    private static void SeedConnectedTrackedFrame(KinectBridgeReceiver receiver)
    {
        Type nestedType = typeof(KinectBridgeReceiver).GetNestedType("KinectTrackingStateMachine", BindingFlags.NonPublic);
        Assert.IsNotNull(nestedType, "State machine type not found.");

        object stateMachine = FormatterServices.GetUninitializedObject(nestedType);
        FieldInfo watchdogField = nestedType.GetField("connectionWatchdog", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(watchdogField, "connectionWatchdog field not found.");

        KinectConnectionWatchdog watchdog = new KinectConnectionWatchdog(TimeSpan.FromSeconds(5));
        watchdog.MarkAccepted(DateTime.UtcNow);
        watchdogField.SetValue(stateMachine, watchdog);

        FieldInfo stateMachineField = typeof(KinectBridgeReceiver).GetField("stateMachine", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(stateMachineField, "stateMachine field not found.");
        stateMachineField.SetValue(receiver, stateMachine);

        FieldInfo latestFrameField = typeof(KinectBridgeReceiver).GetField("latestFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(latestFrameField, "latestFrame field not found.");
        latestFrameField.SetValue(receiver, new KinectTrackingFrame
        {
            SessionId = Guid.NewGuid().ToString("D"),
            FrameId = 1,
            TimestampMs = 1000,
            Tracked = true,
            TrackingId = 1,
            IsNormalizedValid = false,
            RawJoints = CreateInvalidNormalizationJoints()
        });
    }

    private static KinectTrackingJointSample Joint(float x, float y, float z)
    {
        return new KinectTrackingJointSample
        {
            x = x,
            y = y,
            z = z,
            state = (int)KinectTrackingJointState.Tracked
        };
    }
}
