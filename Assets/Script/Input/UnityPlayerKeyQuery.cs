using UnityEngine;

public sealed class UnityPlayerKeyQuery : IPlayerKeyQuery
{
    public static readonly UnityPlayerKeyQuery Instance = new UnityPlayerKeyQuery();

    private UnityPlayerKeyQuery()
    {
    }

    public bool GetKey(KeyCode key)
    {
        return Input.GetKey(key);
    }
}
