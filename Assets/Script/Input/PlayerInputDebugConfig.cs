[System.Serializable]
public sealed class PlayerInputDebugConfig
{
    public float turnAxis;
    public bool chargeHeld;
    public bool leftAttackHeld;
    public bool rightAttackHeld;

    public PlayerInputState ToState()
    {
        return new PlayerInputState(turnAxis, chargeHeld, leftAttackHeld, rightAttackHeld);
    }
}
