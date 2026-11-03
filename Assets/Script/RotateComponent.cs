using UnityEngine;

public class RotateComponent : MonoBehaviour
{
    /// <summary>
    /// 呼び出すと、現在の回転からX軸に90度回転を加える
    /// </summary>
    public void RotateX90()
    {
        // 現在の角度にX軸+90度を加算
        transform.Rotate(0f, 0f, 90f, Space.Self);
    }
}