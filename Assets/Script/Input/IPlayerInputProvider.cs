public interface IPlayerInputProvider
{
    string ProviderName { get; }

    bool IsAvailable { get; }

    PlayerInputState ReadState();
}
