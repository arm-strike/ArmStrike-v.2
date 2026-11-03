using UnityEngine;
using UnityEngine.UI;

public class SpeedometerUI : MonoBehaviour
{
    [Header("--- 参照オブジェクト ---")]
    [Tooltip("回転させる針のImage（RectTransform）")]
    public RectTransform needleTransform;

    [Tooltip("プレイヤーのスピードを取得するための参照")]
    public PlayerController playerController;

    [Header("--- 速度設定 ---")]
    [Tooltip("メーターの最小速度 (km/h)")]
    public float minSpeed = 0f;

    [Tooltip("メーターの最大速度 (km/h)")]
    public float maxSpeed = 100f;

    [Header("--- 針の角度調整 (Z軸回転) ---")]
    [Tooltip("速度0 km/h の時の針の角度（例: 135）")]
    public float minAngle = 135f;

    [Tooltip("速度100 km/h の時の針の角度（例: -135）")]
    public float maxAngle = -135f;

    [Header("--- 演出・動きのなめらかさ ---")]
    [Tooltip("針の動く速さ（大きいほど素早く反応）")]
    public float smoothSpeed = 8f;

    private float currentDisplayedSpeed = 0f;

    void Update()
    {
        // 1. プレイヤーの現在の速度を取得
        float targetSpeed = 0f;
        if (playerController != null)
        {
            // PlayerController の CurrentSpeed を利用
            targetSpeed = playerController.CurrentSpeed;
        }

        // 2. 速度を 0 ～ maxSpeed の範囲に制限
        targetSpeed = Mathf.Clamp(targetSpeed, minSpeed, maxSpeed);

        // 3. 針の動きをスムーズにする（SmoothDamp や Lerp）
        currentDisplayedSpeed = Mathf.Lerp(currentDisplayedSpeed, targetSpeed, Time.deltaTime * smoothSpeed);

        // 4. 速度(0 ~ 100) を 0.0 ～ 1.0 の割合（t）に変換
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, currentDisplayedSpeed);

        // 5. 割合（t）に基づいて、minAngle ～ maxAngle の角度を補間
        float currentAngle = Mathf.Lerp(minAngle, maxAngle, t);

        // 6. 針のZ軸回転を適用
        if (needleTransform != null)
        {
            needleTransform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
    }
}