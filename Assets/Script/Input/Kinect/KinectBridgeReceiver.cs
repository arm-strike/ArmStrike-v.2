using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KinectBridgeReceiver : MonoBehaviour
{
    [SerializeField] private string listenAddress = "127.0.0.1";
    [SerializeField] private int listenPort = 5005;
    [SerializeField] private float connectionTimeoutSeconds = 1.0f;
    [SerializeField] private float trackingLockGraceSeconds = 0.5f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool logTrackingMessages = true;

    private readonly object packetGate = new object();
    private readonly KinectArmFrameNormalizer normalizer = new KinectArmFrameNormalizer();
    private readonly KinectArmMotionMetricsCalculator metricsCalculator = new KinectArmMotionMetricsCalculator();
    private readonly KinectLogThrottle logThrottle = new KinectLogThrottle();
    private Thread receiveThread;
    private UdpClient udpClient;
    private volatile bool running;
    private byte[] latestPayload;
    private bool hasPendingPayload;
    private KinectTrackingStateMachine stateMachine;
    private KinectTrackingFrame latestFrame;
    private string lastReceiveError;
    private string statusMessage;
    private string currentSessionId;
    private long? currentTrackingId;
    private float currentTrackingHoldSeconds;

    public KinectTrackingSourceStatus Status
    {
        get
        {
            if (stateMachine == null || !stateMachine.IsConnected(DateTime.UtcNow))
            {
                return KinectTrackingSourceStatus.Disconnected;
            }

            if (latestFrame == null || !latestFrame.Tracked)
            {
                return KinectTrackingSourceStatus.NoPerson;
            }

            return latestFrame.IsNormalizedValid ? KinectTrackingSourceStatus.Tracking : KinectTrackingSourceStatus.TrackingButNormalizationInvalid;
        }
    }

    public bool IsConnected
    {
        get { return stateMachine != null && stateMachine.IsConnected(DateTime.UtcNow); }
    }

    public bool HasValidTrackedFrame
    {
        get { return latestFrame != null && latestFrame.Tracked && latestFrame.IsNormalizedValid; }
    }

    public KinectTrackingFrame LatestFrame
    {
        get { return latestFrame; }
    }

    public long? LockedTrackingId
    {
        get { return currentTrackingId; }
    }

    public string LastReceiveError
    {
        get { return lastReceiveError; }
    }

    public string DebugStatusMessage
    {
        get { return statusMessage; }
    }

    public bool DebugRunning
    {
        get { return running; }
    }

    public string DebugCurrentSessionId
    {
        get { return currentSessionId; }
    }

    public void ResetReceiver()
    {
        Stop();
        StartReceiver();
    }

    private void Awake()
    {
        stateMachine = new KinectTrackingStateMachine(TimeSpan.FromSeconds(connectionTimeoutSeconds), TimeSpan.FromSeconds(Mathf.Max(0.1f, trackingLockGraceSeconds)));
    }

    private void OnEnable()
    {
        if (stateMachine == null)
        {
            stateMachine = new KinectTrackingStateMachine(TimeSpan.FromSeconds(connectionTimeoutSeconds), TimeSpan.FromSeconds(Mathf.Max(0.1f, trackingLockGraceSeconds)));
        }

        if (autoStart)
        {
            StartReceiver();
        }
    }

    private void Update()
    {
        if (!running)
        {
            return;
        }

        byte[] payload = null;
        lock (packetGate)
        {
            if (hasPendingPayload)
            {
                payload = latestPayload;
                latestPayload = null;
                hasPendingPayload = false;
            }
        }

        if (payload == null)
        {
            return;
        }

        KinectWirePacketParseResult parseResult;
        if (!KinectWirePacketDecoder.TryParseUtf8(payload, out parseResult))
        {
            lastReceiveError = parseResult.ErrorMessage;
            statusMessage = parseResult.ErrorMessage;
            if (logTrackingMessages && logThrottle.ShouldLog("wire-parse-" + parseResult.Error, parseResult.ErrorMessage, TimeSpan.FromSeconds(5), DateTime.UtcNow))
            {
                Debug.LogWarning("[Kinect] " + parseResult.ErrorMessage);
            }

            return;
        }

        KinectTrackingFrame frame;
        string logMessage;
        bool accepted = stateMachine.TryApply(parseResult.Packet, DateTime.UtcNow, out frame, out logMessage);
        if (!accepted)
        {
            if (!string.IsNullOrEmpty(logMessage) && logTrackingMessages && logThrottle.ShouldLog("reject-" + logMessage, logMessage, TimeSpan.FromSeconds(5), DateTime.UtcNow))
            {
                Debug.LogWarning("[Kinect] " + logMessage);
            }

            statusMessage = logMessage;
            return;
        }

        latestFrame = frame;
        currentSessionId = frame.SessionId;
        currentTrackingId = frame.TrackingId;
        currentTrackingHoldSeconds = stateMachine.CurrentTrackingHoldSeconds;
        lastReceiveError = null;
        statusMessage = string.IsNullOrEmpty(logMessage) ? "OK" : logMessage;

        if (!string.IsNullOrEmpty(logMessage) && logTrackingMessages && logThrottle.ShouldLog("state-" + logMessage, logMessage, TimeSpan.FromSeconds(5), DateTime.UtcNow))
        {
            Debug.Log("[Kinect] " + logMessage);
        }
    }

    private void OnDisable()
    {
        Stop();
        latestFrame = null;
        lastReceiveError = null;
        statusMessage = null;
        currentSessionId = null;
        currentTrackingId = null;
        currentTrackingHoldSeconds = 0f;
    }

    private void StartReceiver()
    {
        if (running)
        {
            return;
        }

        if (stateMachine == null)
        {
            stateMachine = new KinectTrackingStateMachine(TimeSpan.FromSeconds(connectionTimeoutSeconds), TimeSpan.FromSeconds(Mathf.Max(0.1f, trackingLockGraceSeconds)));
        }

        udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(string.IsNullOrEmpty(listenAddress) ? "127.0.0.1" : listenAddress), listenPort));
        running = true;
        latestFrame = null;
        lastReceiveError = null;
        statusMessage = null;
        currentSessionId = null;
        currentTrackingId = null;
        currentTrackingHoldSeconds = 0f;
        stateMachine.Reset();
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("[Kinect] UDP 受信を開始しました: " + listenAddress + ":" + listenPort);
    }

    private void Stop()
    {
        if (!running)
        {
            return;
        }

        running = false;
        try
        {
            if (udpClient != null)
            {
                udpClient.Close();
            }
        }
        catch
        {
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000);
        }

        receiveThread = null;
        if (udpClient != null)
        {
            udpClient.Dispose();
            udpClient = null;
        }

        lock (packetGate)
        {
            latestPayload = null;
            hasPendingPayload = false;
        }
    }

    private void ReceiveLoop()
    {
        while (running)
        {
            try
            {
                IPEndPoint endPoint = null;
                byte[] payload = udpClient.Receive(ref endPoint);
                lock (packetGate)
                {
                    latestPayload = payload;
                    hasPendingPayload = true;
                }
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                if (!running)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                lastReceiveError = "UDP受信に失敗しました: " + ex.Message;
                statusMessage = lastReceiveError;
                if (logTrackingMessages && logThrottle.ShouldLog("udp-receive", lastReceiveError, TimeSpan.FromSeconds(5), DateTime.UtcNow))
                {
                    Debug.LogWarning("[Kinect] " + lastReceiveError);
                }
            }
        }
    }

    private sealed class KinectTrackingStateMachine
    {
        private readonly KinectArmFrameNormalizer normalizer;
        private readonly KinectArmMotionMetricsCalculator metricsCalculator;
        private readonly KinectConnectionWatchdog connectionWatchdog;
        private readonly TimeSpan trackingLockGrace;
        private string currentSessionId;
        private long lastAcceptedFrameId = long.MinValue;
        private long? currentTrackingId;
        private KinectTrackingFrame previousTrackedFrame;
        private DateTime? lastTrackedAcceptedUtc;

        public KinectTrackingStateMachine(TimeSpan connectionTimeout, TimeSpan trackingLockGrace)
            : this(new KinectArmFrameNormalizer(), new KinectArmMotionMetricsCalculator(), new KinectConnectionWatchdog(connectionTimeout), trackingLockGrace)
        {
        }

        public KinectTrackingStateMachine(KinectArmFrameNormalizer normalizer, KinectArmMotionMetricsCalculator metricsCalculator, KinectConnectionWatchdog connectionWatchdog, TimeSpan trackingLockGrace)
        {
            this.normalizer = normalizer ?? new KinectArmFrameNormalizer();
            this.metricsCalculator = metricsCalculator ?? new KinectArmMotionMetricsCalculator();
            this.connectionWatchdog = connectionWatchdog ?? new KinectConnectionWatchdog(TimeSpan.FromSeconds(1));
            this.trackingLockGrace = trackingLockGrace <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(500) : trackingLockGrace;
        }

        public KinectTrackingFrame LatestFrame { get; private set; }

        public long? CurrentTrackingId
        {
            get { return currentTrackingId; }
        }

        public float CurrentTrackingHoldSeconds { get; private set; }

        public ConnectionStateSnapshot ConnectionState
        {
            get
            {
                return new ConnectionStateSnapshot
                {
                    CurrentSessionId = currentSessionId,
                    LastAcceptedFrameId = lastAcceptedFrameId,
                    CurrentTrackingId = currentTrackingId,
                    CurrentTrackingHoldSeconds = CurrentTrackingHoldSeconds
                };
            }
        }

        public bool TryApply(KinectTrackingPacket packet, DateTime acceptedAtUtc, out KinectTrackingFrame frame, out string logMessage)
        {
            frame = null;
            logMessage = null;

            if (!IsPacketUsable(packet, out logMessage))
            {
                return false;
            }

            bool sessionChanged = !string.Equals(currentSessionId, packet.sessionId, StringComparison.Ordinal);
            if (sessionChanged)
            {
                ResetForSession(packet.sessionId);
                logMessage = "Bridge の sessionId が切り替わりました。";
            }

            if (packet.sessionId == currentSessionId && packet.frameId <= lastAcceptedFrameId)
            {
                logMessage = "古い frameId を受信したため破棄しました。";
                return false;
            }

            if (!packet.tracked)
            {
                if (lastTrackedAcceptedUtc.HasValue)
                {
                    CurrentTrackingHoldSeconds = (float)(acceptedAtUtc - lastTrackedAcceptedUtc.Value).TotalSeconds;
                    if (CurrentTrackingHoldSeconds >= (float)trackingLockGrace.TotalSeconds)
                    {
                        currentTrackingId = null;
                    }
                }
                else
                {
                    CurrentTrackingHoldSeconds = 0f;
                }

                frame = CreateFrameFromPacket(packet, tracked: false);
                LatestFrame = frame;
                lastAcceptedFrameId = packet.frameId;
                connectionWatchdog.MarkAccepted(acceptedAtUtc);
                return true;
            }

            if (!packet.trackingId.HasValue)
            {
                logMessage = "tracked:true ですが trackingId がありません。";
                return false;
            }

            if (currentTrackingId.HasValue && currentTrackingId.Value != packet.trackingId.Value)
            {
                TimeSpan elapsedSinceTracked = lastTrackedAcceptedUtc.HasValue ? acceptedAtUtc - lastTrackedAcceptedUtc.Value : TimeSpan.MaxValue;
                if (elapsedSinceTracked < trackingLockGrace)
                {
                    logMessage = "trackingId が切り替わりましたがロック保持中です。";
                    return false;
                }
            }

            if (!currentTrackingId.HasValue || currentTrackingId.Value != packet.trackingId.Value)
            {
                previousTrackedFrame = null;
                currentTrackingId = packet.trackingId.Value;
                CurrentTrackingHoldSeconds = 0f;
                logMessage = "trackingId が切り替わったため履歴をリセットしました。";
            }

            if (packet.joints == null)
            {
                logMessage = "tracked:true ですが joints がありません。";
                return false;
            }

            frame = CreateFrameFromPacket(packet, tracked: true);

            KinectTrackingNormalizationResult normalization = normalizer.TryNormalize(packet.joints);
            frame.IsNormalizedValid = normalization.Success;
            frame.NormalizationError = normalization.ErrorMessage;
            frame.ShoulderCenter = normalization.ShoulderCenter;
            frame.NormalizationOrigin = normalization.NormalizationOrigin;
            frame.ShoulderWidthMeters = normalization.ShoulderWidthMeters;
            frame.NormalizedJoints = normalization.NormalizedJoints;

            if (previousTrackedFrame != null && frame.IsNormalizedValid && previousTrackedFrame.IsNormalizedValid)
            {
                KinectTrackingMetrics metrics;
                string metricsError;
                if (metricsCalculator.TryCalculate(previousTrackedFrame, frame, out metrics, out metricsError))
                {
                    frame.Metrics = metrics;
                }
                else if (!string.IsNullOrEmpty(metricsError))
                {
                    logMessage = metricsError;
                }
            }

            previousTrackedFrame = frame;
            LatestFrame = frame;
            lastAcceptedFrameId = packet.frameId;
            connectionWatchdog.MarkAccepted(acceptedAtUtc);
            lastTrackedAcceptedUtc = acceptedAtUtc;
            CurrentTrackingHoldSeconds = 0f;
            return true;
        }

        public bool IsConnected(DateTime nowUtc)
        {
            return connectionWatchdog.IsConnected(nowUtc);
        }

        public void Reset()
        {
            currentSessionId = null;
            currentTrackingId = null;
            lastAcceptedFrameId = long.MinValue;
            previousTrackedFrame = null;
            lastTrackedAcceptedUtc = null;
            LatestFrame = null;
            CurrentTrackingHoldSeconds = 0f;
            connectionWatchdog.Reset();
        }

        private void ResetForSession(string sessionId)
        {
            currentSessionId = sessionId;
            currentTrackingId = null;
            lastAcceptedFrameId = long.MinValue;
            previousTrackedFrame = null;
            lastTrackedAcceptedUtc = null;
            LatestFrame = null;
            CurrentTrackingHoldSeconds = 0f;
        }

        private static bool IsPacketUsable(KinectTrackingPacket packet, out string logMessage)
        {
            logMessage = null;
            if (packet == null)
            {
                logMessage = "パケットが null です。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(packet.sessionId))
            {
                logMessage = "sessionId がありません。";
                return false;
            }

            if (packet.frameId < 0)
            {
                logMessage = "frameId が不正です。";
                return false;
            }

            if (packet.timestampMs < 0)
            {
                logMessage = "timestampMs が不正です。";
                return false;
            }

            return true;
        }

        private static KinectTrackingFrame CreateFrameFromPacket(KinectTrackingPacket packet, bool tracked)
        {
            return new KinectTrackingFrame
            {
                SessionId = packet.sessionId,
                FrameId = packet.frameId,
                TimestampMs = packet.timestampMs,
                Tracked = tracked,
                TrackingId = packet.trackingId,
                RawJoints = packet.joints == null ? null : packet.joints.Clone(),
                NormalizedJoints = null,
                IsNormalizedValid = false,
                NormalizationError = null,
                ShoulderCenter = null,
                NormalizationOrigin = null,
                ShoulderWidthMeters = 0f,
                Metrics = null
            };
        }

        public struct ConnectionStateSnapshot
        {
            public string CurrentSessionId;
            public long LastAcceptedFrameId;
            public long? CurrentTrackingId;
            public float CurrentTrackingHoldSeconds;
        }
    }
}
