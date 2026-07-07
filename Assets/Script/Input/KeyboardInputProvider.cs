using UnityEngine;

public sealed class KeyboardInputProvider : IPlayerInputProvider
{
    private readonly IPlayerKeyQuery keyQuery;

    public KeyboardInputProvider()
        : this(UnityPlayerKeyQuery.Instance)
    {
    }

    public KeyboardInputProvider(IPlayerKeyQuery keyQuery)
    {
        this.keyQuery = keyQuery == null ? UnityPlayerKeyQuery.Instance : keyQuery;
    }

    public string ProviderName
    {
        get { return "Keyboard"; }
    }

    public bool IsAvailable
    {
        get { return true; }
    }

    public PlayerInputState ReadState()
    {
        bool leftHeld = keyQuery.GetKey(KeyCode.A) || keyQuery.GetKey(KeyCode.LeftArrow);
        bool rightHeld = keyQuery.GetKey(KeyCode.D) || keyQuery.GetKey(KeyCode.RightArrow);
        bool chargeHeld = keyQuery.GetKey(KeyCode.S) || keyQuery.GetKey(KeyCode.Space);
        bool leftAttackHeld = keyQuery.GetKey(KeyCode.J);
        bool rightAttackHeld = keyQuery.GetKey(KeyCode.K);

        return new PlayerInputState(
            PlayerInputMapping.ResolveTurnAxis(leftHeld, rightHeld),
            chargeHeld,
            leftAttackHeld,
            rightAttackHeld
        );
    }
}
