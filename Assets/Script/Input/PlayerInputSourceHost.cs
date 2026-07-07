using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInputSourceHost : MonoBehaviour
{
    public PlayerInputSourceKind inputSource = PlayerInputSourceKind.Keyboard;
    public PlayerInputDebugConfig dummyInput = new PlayerInputDebugConfig();
    public MonoBehaviour kinectBridgeReceiverProvider;
    public IPlayerInputProvider keyboardFallbackOverride;
    public bool logFallbackWarnings = true;

    private KeyboardInputProvider keyboardInputProvider;
    private DummyInputProvider dummyInputProvider;
    private PlayerInputDebugConfig cachedDummyInputConfig;
    private bool hasLoggedFallbackWarning;

    public PlayerInputState ReadState()
    {
        return ResolveProvider().ReadState();
    }

    private IPlayerInputProvider ResolveProvider()
    {
        EnsureProviders();

        if (inputSource == PlayerInputSourceKind.Keyboard)
        {
            return GetKeyboardProvider();
        }

        if (inputSource == PlayerInputSourceKind.Dummy)
        {
            return dummyInputProvider;
        }

        IPlayerInputProvider externalProvider = kinectBridgeReceiverProvider as IPlayerInputProvider;
        if (externalProvider != null && externalProvider.IsAvailable)
        {
            return externalProvider;
        }

        WarnFallbackOnce();
        return GetKeyboardProvider();
    }

    private void EnsureProviders()
    {
        if (keyboardInputProvider == null)
        {
            keyboardInputProvider = new KeyboardInputProvider();
        }

        if (dummyInput == null)
        {
            dummyInput = new PlayerInputDebugConfig();
        }

        if (dummyInputProvider == null || !ReferenceEquals(cachedDummyInputConfig, dummyInput))
        {
            cachedDummyInputConfig = dummyInput;
            dummyInputProvider = new DummyInputProvider(dummyInput);
        }
    }

    private IPlayerInputProvider GetKeyboardProvider()
    {
        return keyboardFallbackOverride ?? keyboardInputProvider;
    }

    private void WarnFallbackOnce()
    {
        if (!logFallbackWarnings || hasLoggedFallbackWarning)
        {
            return;
        }

        hasLoggedFallbackWarning = true;
        Debug.LogWarning("[PlayerInput] Selected input source is unavailable. Falling back to Keyboard.");
    }
}
