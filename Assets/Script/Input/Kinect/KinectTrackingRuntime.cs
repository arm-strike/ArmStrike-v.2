using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using UnityEngine;

public static class KinectProtocolConstants
{
    public const int CurrentVersion = 1;
}

public enum KinectTrackingJointState
{
    NotTracked = 0,
    Inferred = 1,
    Tracked = 2
}

public enum KinectTrackingSourceStatus
{
    Disconnected = 0,
    NoPerson = 1,
    Tracking = 2,
    TrackingButNormalizationInvalid = 3
}

[Serializable]
[DataContract]
public sealed class KinectWireJointSampleDto
{
    [DataMember(Name = "x")]
    public float? x;

    [DataMember(Name = "y")]
    public float? y;

    [DataMember(Name = "z")]
    public float? z;

    [DataMember(Name = "state")]
    public int? state;
}

[Serializable]
[DataContract]
public sealed class KinectWireJointCollectionDto
{
    [DataMember(Name = "shoulderCenter")]
    public KinectWireJointSampleDto shoulderCenter;

    [DataMember(Name = "spine")]
    public KinectWireJointSampleDto spine;

    [DataMember(Name = "hipCenter")]
    public KinectWireJointSampleDto hipCenter;

    [DataMember(Name = "shoulderLeft")]
    public KinectWireJointSampleDto shoulderLeft;

    [DataMember(Name = "elbowLeft")]
    public KinectWireJointSampleDto elbowLeft;

    [DataMember(Name = "wristLeft")]
    public KinectWireJointSampleDto wristLeft;

    [DataMember(Name = "handLeft")]
    public KinectWireJointSampleDto handLeft;

    [DataMember(Name = "shoulderRight")]
    public KinectWireJointSampleDto shoulderRight;

    [DataMember(Name = "elbowRight")]
    public KinectWireJointSampleDto elbowRight;

    [DataMember(Name = "wristRight")]
    public KinectWireJointSampleDto wristRight;

    [DataMember(Name = "handRight")]
    public KinectWireJointSampleDto handRight;
}

[Serializable]
[DataContract]
public sealed class KinectWirePacketDto
{
    [DataMember(Name = "version")]
    public int? version;

    [DataMember(Name = "sessionId")]
    public string sessionId;

    [DataMember(Name = "frameId")]
    public long? frameId;

    [DataMember(Name = "timestampMs")]
    public long? timestampMs;

    [DataMember(Name = "tracked")]
    public bool? tracked;

    [DataMember(Name = "trackingId", EmitDefaultValue = false)]
    public long? trackingId;

    [DataMember(Name = "joints", EmitDefaultValue = false)]
    public KinectWireJointCollectionDto joints;
}

[Serializable]
[DataContract]
public sealed class KinectTrackingJointSample
{
    [DataMember(Name = "x")]
    public float x;

    [DataMember(Name = "y")]
    public float y;

    [DataMember(Name = "z")]
    public float z;

    [DataMember(Name = "state")]
    public int state;

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public KinectTrackingJointSample Clone()
    {
        return new KinectTrackingJointSample
        {
            x = x,
            y = y,
            z = z,
            state = state
        };
    }
}

[Serializable]
[DataContract]
public sealed class KinectTrackingJointCollection
{
    [DataMember(Name = "shoulderCenter")]
    public KinectTrackingJointSample shoulderCenter;

    [DataMember(Name = "spine")]
    public KinectTrackingJointSample spine;

    [DataMember(Name = "hipCenter")]
    public KinectTrackingJointSample hipCenter;

    [DataMember(Name = "shoulderLeft")]
    public KinectTrackingJointSample shoulderLeft;

    [DataMember(Name = "elbowLeft")]
    public KinectTrackingJointSample elbowLeft;

    [DataMember(Name = "wristLeft")]
    public KinectTrackingJointSample wristLeft;

    [DataMember(Name = "handLeft")]
    public KinectTrackingJointSample handLeft;

    [DataMember(Name = "shoulderRight")]
    public KinectTrackingJointSample shoulderRight;

    [DataMember(Name = "elbowRight")]
    public KinectTrackingJointSample elbowRight;

    [DataMember(Name = "wristRight")]
    public KinectTrackingJointSample wristRight;

    [DataMember(Name = "handRight")]
    public KinectTrackingJointSample handRight;

    public KinectTrackingJointCollection Clone()
    {
        return new KinectTrackingJointCollection
        {
            shoulderCenter = CloneJoint(shoulderCenter),
            spine = CloneJoint(spine),
            hipCenter = CloneJoint(hipCenter),
            shoulderLeft = CloneJoint(shoulderLeft),
            elbowLeft = CloneJoint(elbowLeft),
            wristLeft = CloneJoint(wristLeft),
            handLeft = CloneJoint(handLeft),
            shoulderRight = CloneJoint(shoulderRight),
            elbowRight = CloneJoint(elbowRight),
            wristRight = CloneJoint(wristRight),
            handRight = CloneJoint(handRight)
        };
    }

    private static KinectTrackingJointSample CloneJoint(KinectTrackingJointSample joint)
    {
        return joint == null ? null : joint.Clone();
    }
}

[Serializable]
[DataContract]
public sealed class KinectTrackingMetrics
{
    [DataMember(Name = "leftElbowAngleDegrees")]
    public float leftElbowAngleDegrees;

    [DataMember(Name = "rightElbowAngleDegrees")]
    public float rightElbowAngleDegrees;

    [DataMember(Name = "leftHandSpeedMetersPerSecond")]
    public float leftHandSpeedMetersPerSecond;

    [DataMember(Name = "rightHandSpeedMetersPerSecond")]
    public float rightHandSpeedMetersPerSecond;

    [DataMember(Name = "leftHandTowardKinectSpeedMetersPerSecond")]
    public float leftHandTowardKinectSpeedMetersPerSecond;

    [DataMember(Name = "rightHandTowardKinectSpeedMetersPerSecond")]
    public float rightHandTowardKinectSpeedMetersPerSecond;

    [DataMember(Name = "leftShoulderToHandDistanceMeters")]
    public float leftShoulderToHandDistanceMeters;

    [DataMember(Name = "rightShoulderToHandDistanceMeters")]
    public float rightShoulderToHandDistanceMeters;

    [DataMember(Name = "leftArmExtensionNormalized")]
    public float leftArmExtensionNormalized;

    [DataMember(Name = "rightArmExtensionNormalized")]
    public float rightArmExtensionNormalized;

    [DataMember(Name = "leftHandForwardFromShoulderMeters")]
    public float leftHandForwardFromShoulderMeters;

    [DataMember(Name = "rightHandForwardFromShoulderMeters")]
    public float rightHandForwardFromShoulderMeters;
}

[Serializable]
[DataContract]
public sealed class KinectTrackingFrame
{
    [DataMember(Name = "sessionId")]
    public string SessionId { get; set; }

    [DataMember(Name = "frameId")]
    public long FrameId { get; set; }

    [DataMember(Name = "timestampMs")]
    public long TimestampMs { get; set; }

    [DataMember(Name = "tracked")]
    public bool Tracked { get; set; }

    [DataMember(Name = "trackingId")]
    public long? TrackingId { get; set; }

    [DataMember(Name = "rawJoints")]
    public KinectTrackingJointCollection RawJoints { get; set; }

    [DataMember(Name = "normalizedJoints")]
    public KinectTrackingJointCollection NormalizedJoints { get; set; }

    [DataMember(Name = "isNormalizedValid")]
    public bool IsNormalizedValid { get; set; }

    [DataMember(Name = "normalizationError")]
    public string NormalizationError { get; set; }

    [DataMember(Name = "shoulderWidthMeters")]
    public float ShoulderWidthMeters { get; set; }

    [DataMember(Name = "shoulderCenter")]
    public KinectTrackingJointSample ShoulderCenter { get; set; }

    [DataMember(Name = "normalizationOrigin")]
    public KinectTrackingJointSample NormalizationOrigin { get; set; }

    [DataMember(Name = "metrics")]
    public KinectTrackingMetrics Metrics { get; set; }
}

[Serializable]
[DataContract]
public sealed class KinectTrackingNormalizationOptions
{
    [DataMember(Name = "minimumShoulderWidthMeters")]
    public float MinimumShoulderWidthMeters = 0.05f;
}

[Serializable]
[DataContract]
public sealed class KinectTrackingNormalizationResult
{
    [DataMember(Name = "success")]
    public bool Success { get; set; }

    [DataMember(Name = "errorMessage")]
    public string ErrorMessage { get; set; }

    [DataMember(Name = "shoulderCenter")]
    public KinectTrackingJointSample ShoulderCenter { get; set; }

    [DataMember(Name = "normalizationOrigin")]
    public KinectTrackingJointSample NormalizationOrigin { get; set; }

    [DataMember(Name = "shoulderWidthMeters")]
    public float ShoulderWidthMeters { get; set; }

    [DataMember(Name = "normalizedJoints")]
    public KinectTrackingJointCollection NormalizedJoints { get; set; }
}

[Serializable]
[DataContract]
public sealed class KinectTrackingPacket
{
    [DataMember(Name = "version")]
    public int version = KinectProtocolConstants.CurrentVersion;

    [DataMember(Name = "sessionId")]
    public string sessionId;

    [DataMember(Name = "frameId")]
    public long frameId;

    [DataMember(Name = "timestampMs")]
    public long timestampMs;

    [DataMember(Name = "tracked")]
    public bool tracked;

    [DataMember(Name = "trackingId", EmitDefaultValue = false)]
    public long? trackingId;

    [DataMember(Name = "joints", EmitDefaultValue = false)]
    public KinectTrackingJointCollection joints;

    public KinectTrackingPacket Clone()
    {
        return new KinectTrackingPacket
        {
            version = version,
            sessionId = sessionId,
            frameId = frameId,
            timestampMs = timestampMs,
            tracked = tracked,
            trackingId = trackingId,
            joints = joints == null ? null : joints.Clone()
        };
    }
}

[Serializable]
[DataContract]
public sealed class KinectWirePacketParseResult
{
    [DataMember(Name = "success")]
    public bool Success { get; set; }

    [DataMember(Name = "error")]
    public KinectWirePacketValidationError Error { get; set; }

    [DataMember(Name = "errorMessage")]
    public string ErrorMessage { get; set; }

    [DataMember(Name = "wirePacket")]
    public KinectWirePacketDto WirePacket { get; set; }

    [DataMember(Name = "packet")]
    public KinectTrackingPacket Packet { get; set; }
}

public enum KinectWirePacketValidationError
{
    None = 0,
    InvalidUtf8,
    InvalidJson,
    MissingVersion,
    InvalidVersion,
    MissingSessionId,
    InvalidSessionId,
    MissingFrameId,
    InvalidFrameId,
    MissingTimestampMs,
    InvalidTimestampMs,
    MissingTracked,
    MissingTrackingId,
    InvalidTrackingId,
    MissingJoints,
    MissingJointSample,
    MissingJointCoordinate,
    InvalidJointCoordinate,
    MissingJointState,
    InvalidJointState,
    UnexpectedTrackedFalseFields
}

public static class KinectWirePacketDecoder
{
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly DataContractJsonSerializer Serializer = new DataContractJsonSerializer(typeof(KinectWirePacketDto));

    public static bool TryParseUtf8(byte[] payload, out KinectWirePacketParseResult result)
    {
        result = new KinectWirePacketParseResult();

        if (payload == null)
        {
            return Fail(result, KinectWirePacketValidationError.InvalidUtf8, "UTF-8 データがありません。");
        }

        string json;
        try
        {
            json = StrictUtf8.GetString(payload);
        }
        catch (Exception ex)
        {
            return Fail(result, KinectWirePacketValidationError.InvalidUtf8, "UTF-8 デコードに失敗しました: " + ex.Message);
        }

        return TryParseJson(json, out result);
    }

    public static bool TryParseJson(string json, out KinectWirePacketParseResult result)
    {
        result = new KinectWirePacketParseResult();

        if (json == null)
        {
            return Fail(result, KinectWirePacketValidationError.InvalidJson, "JSON が null です。");
        }

        KinectWirePacketDto wirePacket;
        try
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                wirePacket = Serializer.ReadObject(stream) as KinectWirePacketDto;
            }
        }
        catch (Exception ex)
        {
            return Fail(result, KinectWirePacketValidationError.InvalidJson, "JSON の解析に失敗しました: " + ex.Message);
        }

        if (wirePacket == null)
        {
            return Fail(result, KinectWirePacketValidationError.InvalidJson, "JSON の解析結果が空です。");
        }

        KinectTrackingPacket packet;
        KinectWirePacketValidationError error;
        string errorMessage;
        if (!TryValidateAndConvert(wirePacket, out packet, out error, out errorMessage))
        {
            return Fail(result, error, errorMessage, wirePacket);
        }

        result.Success = true;
        result.Error = KinectWirePacketValidationError.None;
        result.ErrorMessage = null;
        result.WirePacket = wirePacket;
        result.Packet = packet;
        return true;
    }

    private static bool TryValidateAndConvert(KinectWirePacketDto wirePacket, out KinectTrackingPacket packet, out KinectWirePacketValidationError error, out string errorMessage)
    {
        packet = null;
        error = KinectWirePacketValidationError.None;
        errorMessage = null;

        if (wirePacket.version == null)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingVersion, "version がありません。");
        }

        if (wirePacket.version.Value != KinectProtocolConstants.CurrentVersion)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.InvalidVersion, "version が不正です。 received=" + wirePacket.version.Value + " expected=" + KinectProtocolConstants.CurrentVersion);
        }

        if (string.IsNullOrWhiteSpace(wirePacket.sessionId))
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingSessionId, "sessionId がありません。");
        }

        if (!Guid.TryParseExact(wirePacket.sessionId.Trim(), "D", out _))
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.InvalidSessionId, "sessionId が GUID として不正です。");
        }

        if (wirePacket.frameId == null)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingFrameId, "frameId がありません。");
        }

        if (wirePacket.frameId.Value < 0)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.InvalidFrameId, "frameId が不正です。");
        }

        if (wirePacket.timestampMs == null)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingTimestampMs, "timestampMs がありません。");
        }

        if (wirePacket.timestampMs.Value < 0)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.InvalidTimestampMs, "timestampMs が不正です。");
        }

        if (wirePacket.tracked == null)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingTracked, "tracked がありません。");
        }

        if (!wirePacket.tracked.Value)
        {
            if (wirePacket.trackingId.HasValue || wirePacket.joints != null)
            {
                return Fail(out error, out errorMessage, KinectWirePacketValidationError.UnexpectedTrackedFalseFields, "tracked:false に trackingId または joints が含まれています。");
            }

            packet = new KinectTrackingPacket
            {
                version = wirePacket.version.Value,
                sessionId = wirePacket.sessionId,
                frameId = wirePacket.frameId.Value,
                timestampMs = wirePacket.timestampMs.Value,
                tracked = false,
                trackingId = null,
                joints = null
            };
            return true;
        }

        if (!wirePacket.trackingId.HasValue)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingTrackingId, "tracked:true ですが trackingId がありません。");
        }

        if (wirePacket.trackingId.Value <= 0)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.InvalidTrackingId, "trackingId が不正です。");
        }

        if (wirePacket.joints == null)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingJoints, "tracked:true ですが joints がありません。");
        }

        KinectTrackingJointCollection joints;
        if (!TryConvertJoints(wirePacket.joints, out joints, out error, out errorMessage))
        {
            return false;
        }

        packet = new KinectTrackingPacket
        {
            version = wirePacket.version.Value,
            sessionId = wirePacket.sessionId,
            frameId = wirePacket.frameId.Value,
            timestampMs = wirePacket.timestampMs.Value,
            tracked = true,
            trackingId = wirePacket.trackingId,
            joints = joints
        };
        return true;
    }

    private static bool TryConvertJoints(KinectWireJointCollectionDto wireJoints, out KinectTrackingJointCollection joints, out KinectWirePacketValidationError error, out string errorMessage)
    {
        joints = new KinectTrackingJointCollection();
        error = KinectWirePacketValidationError.None;
        errorMessage = null;

        if (!TryConvertJoint("shoulderCenter", wireJoints.shoulderCenter, out joints.shoulderCenter, out error, out errorMessage)) return false;
        if (!TryConvertOptionalJoint(wireJoints.spine, out joints.spine, out error, out errorMessage)) return false;
        if (!TryConvertOptionalJoint(wireJoints.hipCenter, out joints.hipCenter, out error, out errorMessage)) return false;
        if (!TryConvertJoint("shoulderLeft", wireJoints.shoulderLeft, out joints.shoulderLeft, out error, out errorMessage)) return false;
        if (!TryConvertJoint("elbowLeft", wireJoints.elbowLeft, out joints.elbowLeft, out error, out errorMessage)) return false;
        if (!TryConvertJoint("wristLeft", wireJoints.wristLeft, out joints.wristLeft, out error, out errorMessage)) return false;
        if (!TryConvertJoint("handLeft", wireJoints.handLeft, out joints.handLeft, out error, out errorMessage)) return false;
        if (!TryConvertJoint("shoulderRight", wireJoints.shoulderRight, out joints.shoulderRight, out error, out errorMessage)) return false;
        if (!TryConvertJoint("elbowRight", wireJoints.elbowRight, out joints.elbowRight, out error, out errorMessage)) return false;
        if (!TryConvertJoint("wristRight", wireJoints.wristRight, out joints.wristRight, out error, out errorMessage)) return false;
        if (!TryConvertJoint("handRight", wireJoints.handRight, out joints.handRight, out error, out errorMessage)) return false;

        return true;
    }

    private static bool TryConvertOptionalJoint(KinectWireJointSampleDto wireJoint, out KinectTrackingJointSample joint, out KinectWirePacketValidationError error, out string errorMessage)
    {
        joint = null;
        error = KinectWirePacketValidationError.None;
        errorMessage = null;

        if (wireJoint == null)
        {
            return true;
        }

        return TryConvertJoint("optional", wireJoint, out joint, out error, out errorMessage);
    }

    private static bool TryConvertJoint(string jointName, KinectWireJointSampleDto wireJoint, out KinectTrackingJointSample joint, out KinectWirePacketValidationError error, out string errorMessage)
    {
        joint = null;
        error = KinectWirePacketValidationError.None;
        errorMessage = null;

        if (wireJoint == null)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingJointSample, jointName + " がありません。");
        }

        if (!wireJoint.x.HasValue || !wireJoint.y.HasValue || !wireJoint.z.HasValue)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingJointCoordinate, jointName + " の座標がありません。");
        }

        if (float.IsNaN(wireJoint.x.Value) || float.IsNaN(wireJoint.y.Value) || float.IsNaN(wireJoint.z.Value) ||
            float.IsInfinity(wireJoint.x.Value) || float.IsInfinity(wireJoint.y.Value) || float.IsInfinity(wireJoint.z.Value))
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.InvalidJointCoordinate, jointName + " の座標が不正です。");
        }

        if (!wireJoint.state.HasValue)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.MissingJointState, jointName + " の state がありません。");
        }

        if (wireJoint.state.Value < (int)KinectTrackingJointState.NotTracked || wireJoint.state.Value > (int)KinectTrackingJointState.Tracked)
        {
            return Fail(out error, out errorMessage, KinectWirePacketValidationError.InvalidJointState, jointName + " の state が不正です。");
        }

        joint = new KinectTrackingJointSample
        {
            x = wireJoint.x.Value,
            y = wireJoint.y.Value,
            z = wireJoint.z.Value,
            state = wireJoint.state.Value
        };
        return true;
    }

    private static bool Fail(out KinectWirePacketValidationError error, out string errorMessage, KinectWirePacketValidationError reason, string message)
    {
        error = reason;
        errorMessage = message;
        return false;
    }

    private static bool Fail(KinectWirePacketParseResult result, KinectWirePacketValidationError error, string errorMessage, KinectWirePacketDto wirePacket = null)
    {
        result.Success = false;
        result.Error = error;
        result.ErrorMessage = errorMessage;
        result.WirePacket = wirePacket;
        result.Packet = null;
        return false;
    }
}

public sealed class KinectArmFrameNormalizer
{
    private readonly KinectTrackingNormalizationOptions options;

    public KinectArmFrameNormalizer()
        : this(new KinectTrackingNormalizationOptions())
    {
    }

    public KinectArmFrameNormalizer(KinectTrackingNormalizationOptions options)
    {
        this.options = options ?? new KinectTrackingNormalizationOptions();
    }

    public KinectTrackingNormalizationResult TryNormalize(KinectTrackingJointCollection raw)
    {
        KinectTrackingNormalizationResult result = new KinectTrackingNormalizationResult();

        if (raw == null)
        {
            result.ErrorMessage = "正規化対象の関節セットがありません。";
            return result;
        }

        if (!HasUsableShoulderJoints(raw))
        {
            result.ErrorMessage = "肩の関節が NotTracked です。";
            return result;
        }

        KinectTrackingJointSample leftShoulder = raw.shoulderLeft;
        KinectTrackingJointSample rightShoulder = raw.shoulderRight;

        Vector3 center = (leftShoulder.ToVector3() + rightShoulder.ToVector3()) * 0.5f;
        float width = Vector3.Distance(leftShoulder.ToVector3(), rightShoulder.ToVector3());

        if (width < options.MinimumShoulderWidthMeters)
        {
            result.ErrorMessage = "肩幅が極端に小さいため正規化できません。";
            return result;
        }

        int centerState = Math.Min(leftShoulder.state, rightShoulder.state);

        result.Success = true;
        result.NormalizationOrigin = CreateNormalizedOrigin(centerState);
        result.ShoulderCenter = CreateNormalizedOrigin(centerState);
        result.ShoulderWidthMeters = width;
        result.NormalizedJoints = NormalizeCollection(raw, center, width, centerState);
        return result;
    }

    private static bool HasUsableShoulderJoints(KinectTrackingJointCollection raw)
    {
        return raw.shoulderLeft != null &&
               raw.shoulderRight != null &&
               raw.shoulderLeft.state != (int)KinectTrackingJointState.NotTracked &&
               raw.shoulderRight.state != (int)KinectTrackingJointState.NotTracked;
    }

    private static KinectTrackingJointCollection NormalizeCollection(KinectTrackingJointCollection raw, Vector3 center, float width, int centerState)
    {
        return new KinectTrackingJointCollection
        {
            shoulderCenter = CreateNormalizedOrigin(centerState),
            spine = NormalizeJoint(raw.spine, center, width),
            hipCenter = NormalizeJoint(raw.hipCenter, center, width),
            shoulderLeft = NormalizeJoint(raw.shoulderLeft, center, width),
            elbowLeft = NormalizeJoint(raw.elbowLeft, center, width),
            wristLeft = NormalizeJoint(raw.wristLeft, center, width),
            handLeft = NormalizeJoint(raw.handLeft, center, width),
            shoulderRight = NormalizeJoint(raw.shoulderRight, center, width),
            elbowRight = NormalizeJoint(raw.elbowRight, center, width),
            wristRight = NormalizeJoint(raw.wristRight, center, width),
            handRight = NormalizeJoint(raw.handRight, center, width)
        };
    }

    private static KinectTrackingJointSample CreateNormalizedOrigin(int state)
    {
        return new KinectTrackingJointSample
        {
            x = 0f,
            y = 0f,
            z = 0f,
            state = state
        };
    }

    private static KinectTrackingJointSample NormalizeJoint(KinectTrackingJointSample joint, Vector3 center, float width)
    {
        if (joint == null)
        {
            return null;
        }

        return new KinectTrackingJointSample
        {
            x = (joint.x - center.x) / width,
            y = (joint.y - center.y) / width,
            z = (joint.z - center.z) / width,
            state = joint.state
        };
    }
}

public sealed class KinectArmMotionMetricsCalculator
{
    public bool TryCalculate(KinectTrackingFrame previousFrame, KinectTrackingFrame currentFrame, out KinectTrackingMetrics metrics, out string errorMessage)
    {
        metrics = null;
        errorMessage = null;

        if (previousFrame == null)
        {
            errorMessage = "前フレームがありません。";
            return false;
        }

        if (currentFrame == null)
        {
            errorMessage = "現在フレームがありません。";
            return false;
        }

        if (currentFrame.RawJoints == null || previousFrame.RawJoints == null)
        {
            errorMessage = "関節データが不足しています。";
            return false;
        }

        if (currentFrame.ShoulderWidthMeters <= 0f)
        {
            errorMessage = "肩幅が不正です。";
            return false;
        }

        float deltaSeconds = (currentFrame.TimestampMs - previousFrame.TimestampMs) / 1000f;
        if (deltaSeconds <= 0f)
        {
            errorMessage = "前フレームとの差分時間が不正です。";
            return false;
        }

        KinectTrackingJointCollection prev = previousFrame.RawJoints;
        KinectTrackingJointCollection curr = currentFrame.RawJoints;

        if (!HasUsableArmJoints(prev, curr))
        {
            errorMessage = "腕の関節が NotTracked です。";
            return false;
        }

        metrics = new KinectTrackingMetrics
        {
            leftElbowAngleDegrees = ComputeElbowAngle(curr.shoulderLeft, curr.elbowLeft, curr.handLeft),
            rightElbowAngleDegrees = ComputeElbowAngle(curr.shoulderRight, curr.elbowRight, curr.handRight),
            leftHandSpeedMetersPerSecond = ComputeSpeed(prev.handLeft, curr.handLeft, deltaSeconds),
            rightHandSpeedMetersPerSecond = ComputeSpeed(prev.handRight, curr.handRight, deltaSeconds),
            leftHandTowardKinectSpeedMetersPerSecond = ComputeTowardKinectSpeed(prev.handLeft, curr.handLeft, deltaSeconds),
            rightHandTowardKinectSpeedMetersPerSecond = ComputeTowardKinectSpeed(prev.handRight, curr.handRight, deltaSeconds),
            leftShoulderToHandDistanceMeters = Distance(curr.shoulderLeft, curr.handLeft),
            rightShoulderToHandDistanceMeters = Distance(curr.shoulderRight, curr.handRight)
        };
        metrics.leftArmExtensionNormalized = metrics.leftShoulderToHandDistanceMeters / currentFrame.ShoulderWidthMeters;
        metrics.rightArmExtensionNormalized = metrics.rightShoulderToHandDistanceMeters / currentFrame.ShoulderWidthMeters;
        metrics.leftHandForwardFromShoulderMeters = curr.shoulderLeft.z - curr.handLeft.z;
        metrics.rightHandForwardFromShoulderMeters = curr.shoulderRight.z - curr.handRight.z;
        return true;
    }

    private static bool HasUsableArmJoints(KinectTrackingJointCollection previous, KinectTrackingJointCollection current)
    {
        return IsUsable(previous.handLeft) &&
               IsUsable(previous.handRight) &&
               IsUsable(current.shoulderLeft) &&
               IsUsable(current.elbowLeft) &&
               IsUsable(current.handLeft) &&
               IsUsable(current.shoulderRight) &&
               IsUsable(current.elbowRight) &&
               IsUsable(current.handRight);
    }

    private static bool IsUsable(KinectTrackingJointSample joint)
    {
        return joint != null && joint.state != (int)KinectTrackingJointState.NotTracked;
    }

    private static float ComputeSpeed(KinectTrackingJointSample previous, KinectTrackingJointSample current, float deltaSeconds)
    {
        return Distance(previous, current) / deltaSeconds;
    }

    private static float ComputeTowardKinectSpeed(KinectTrackingJointSample previous, KinectTrackingJointSample current, float deltaSeconds)
    {
        return (previous.z - current.z) / deltaSeconds;
    }

    private static float ComputeElbowAngle(KinectTrackingJointSample shoulder, KinectTrackingJointSample elbow, KinectTrackingJointSample hand)
    {
        if (shoulder == null || elbow == null || hand == null)
        {
            return float.NaN;
        }

        Vector3 first = shoulder.ToVector3() - elbow.ToVector3();
        Vector3 second = hand.ToVector3() - elbow.ToVector3();
        if (first.sqrMagnitude <= Mathf.Epsilon || second.sqrMagnitude <= Mathf.Epsilon)
        {
            return float.NaN;
        }

        return Vector3.Angle(first, second);
    }

    private static float Distance(KinectTrackingJointSample first, KinectTrackingJointSample second)
    {
        if (first == null || second == null)
        {
            return float.NaN;
        }

        return Vector3.Distance(first.ToVector3(), second.ToVector3());
    }
}

public sealed class KinectConnectionWatchdog
{
    private readonly TimeSpan timeout;

    public KinectConnectionWatchdog(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        this.timeout = timeout;
    }

    public TimeSpan Timeout
    {
        get { return timeout; }
    }

    public DateTime? LastAcceptedUtc { get; private set; }

    public void MarkAccepted(DateTime acceptedUtc)
    {
        LastAcceptedUtc = acceptedUtc;
    }

    public void Reset()
    {
        LastAcceptedUtc = null;
    }

    public bool IsConnected(DateTime nowUtc)
    {
        if (!LastAcceptedUtc.HasValue)
        {
            return false;
        }

        return nowUtc - LastAcceptedUtc.Value <= timeout;
    }
}

public sealed class KinectLogThrottle
{
    private sealed class Entry
    {
        public string Message;
        public DateTime LastLoggedUtc;
    }

    private readonly System.Collections.Generic.Dictionary<string, Entry> entries = new System.Collections.Generic.Dictionary<string, Entry>();
    private readonly object gate = new object();

    public bool ShouldLog(string key, string message, TimeSpan repeatInterval, DateTime nowUtc)
    {
        lock (gate)
        {
            Entry entry;
            if (!entries.TryGetValue(key, out entry))
            {
                entry = new Entry();
                entries[key] = entry;
            }

            if (!string.Equals(entry.Message, message, StringComparison.Ordinal))
            {
                entry.Message = message;
                entry.LastLoggedUtc = nowUtc;
                return true;
            }

            if (nowUtc - entry.LastLoggedUtc >= repeatInterval)
            {
                entry.LastLoggedUtc = nowUtc;
                return true;
            }

            return false;
        }
    }

    public void Reset(string key)
    {
        lock (gate)
        {
            entries.Remove(key);
        }
    }
}
