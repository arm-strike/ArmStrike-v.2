public static class PlayerInputMapping
{
    public static float ResolveTurnAxis(bool leftHeld, bool rightHeld)
    {
        if (leftHeld == rightHeld)
        {
            return 0f;
        }

        return leftHeld ? -1f : 1f;
    }
}
