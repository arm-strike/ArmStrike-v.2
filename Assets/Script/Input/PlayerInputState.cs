using UnityEngine;

[System.Serializable]
public struct PlayerInputState
{
    public float turnAxis;
    public bool chargeHeld;
    public bool leftAttackHeld;
    public bool rightAttackHeld;

    public PlayerInputState(float turnAxis, bool chargeHeld, bool leftAttackHeld, bool rightAttackHeld)
    {
        this.turnAxis = Mathf.Clamp(turnAxis, -1f, 1f);
        this.chargeHeld = chargeHeld;
        this.leftAttackHeld = leftAttackHeld;
        this.rightAttackHeld = rightAttackHeld;
    }

    public static PlayerInputState Neutral
    {
        get { return new PlayerInputState(0f, false, false, false); }
    }
}
