using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // 追いかける対象（プレイヤー）
    public Transform playerTransform;

    // プレイヤーの「真後ろ」にどれくらい離してカメラを置くか（距離の設定）
    // 以前の位置（Y=5, Z=-10くらい）をベースに調整します
    public Vector3 offset = new Vector3(0f, 5f, -10f);

    void LateUpdate()
    {
        if (playerTransform != null)
        {
            // 1. プレイヤーの現在の回転（向き）に合わせて、カメラの位置を計算する
            // プレイヤーが回転すると、この位置も自動的にプレイヤーの真後ろに回り込みます
            Vector3 targetPosition = playerTransform.position + (playerTransform.rotation * offset);
            transform.position = targetPosition;

            // 2. カメラの向きを、プレイヤーと同じ向きにする
            transform.rotation = playerTransform.rotation;

            // 3. カメラを少しだけ下（プレイヤーの方向）に傾ける
            // もし見下ろす角度を変えたい場合は、この「20」の数字を調整してください
            transform.Rotate(Vector3.right, 20f);
        }
    }
}