using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(KinectBridgeReceiver))]
[RequireComponent(typeof(KinectBridgeReceiverInputProvider))]
public sealed class KinectInputRig : MonoBehaviour
{
    [SerializeField] private KinectBridgeReceiver receiver;
    [SerializeField] private KinectBridgeReceiverInputProvider inputProvider;
    [SerializeField] private KinectInputTuningProfile tuningProfile;
    [SerializeField] private bool autoCreateRuntimeProfile = true;
    [SerializeField] private PlayerInputSourceHost playerInputSourceHost;
    [SerializeField] private bool autoWirePlayerInputSourceHost = true;

    public KinectBridgeReceiver Receiver
    {
        get { return receiver; }
    }

    public KinectBridgeReceiverInputProvider InputProvider
    {
        get { return inputProvider; }
    }

    public PlayerInputSourceHost PlayerInputSourceHost
    {
        get { return playerInputSourceHost; }
    }

    private void Reset()
    {
        CacheReferences();
        SyncBindings();
    }

    private void Awake()
    {
        CacheReferences();
        SyncBindings();
    }

    private void OnEnable()
    {
        CacheReferences();
        SyncBindings();
    }

    private void OnValidate()
    {
        CacheReferences();
        SyncBindings();
    }

    private void CacheReferences()
    {
        if (receiver == null)
        {
            receiver = GetComponent<KinectBridgeReceiver>();
        }

        if (inputProvider == null)
        {
            inputProvider = GetComponent<KinectBridgeReceiverInputProvider>();
        }

        if (autoWirePlayerInputSourceHost && playerInputSourceHost == null)
        {
            playerInputSourceHost = Object.FindFirstObjectByType<PlayerInputSourceHost>();
        }
    }

    private void SyncBindings()
    {
        if (inputProvider != null)
        {
            inputProvider.Configure(receiver, tuningProfile, autoCreateRuntimeProfile);
        }

        if (playerInputSourceHost != null && inputProvider != null)
        {
            playerInputSourceHost.kinectBridgeReceiverProvider = inputProvider;
        }
    }
}
