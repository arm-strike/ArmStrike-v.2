public sealed class DummyInputProvider : IPlayerInputProvider
{
    private readonly PlayerInputDebugConfig config;

    public DummyInputProvider(PlayerInputDebugConfig config)
    {
        this.config = config == null ? new PlayerInputDebugConfig() : config;
    }

    public string ProviderName
    {
        get { return "Dummy"; }
    }

    public bool IsAvailable
    {
        get { return true; }
    }

    public PlayerInputState ReadState()
    {
        return config.ToState();
    }
}
