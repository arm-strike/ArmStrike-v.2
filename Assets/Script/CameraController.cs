using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("追跡するプレイヤー（本体）")]
    public Transform target;

    [Header("プレイヤーとの基本距離（位置オフセット）")]
    public Vector3 offset = new Vector3(0f, 6f, -12f);

    [Header("カメラが追従する滑らかさ")]
    public float smoothSpeed = 5f;

    [Header("💥 スピード連動のカスタマイズ")]
    public float minFOV = 50f;       // チャージブレーキ（停止）時の視野角
    public float normalFOV = 60f;    // 通常走行時の視野角
    public float maxFOV = 80f;       // 最高速（ロケットダッシュ）時の視野角
    public float fovSmoothSpeed = 4f; // 視野角が変化する滑らかさ

    private Camera cam;
    private Rigidbody targetRb;
    private PlayerController playerController;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody>();
            playerController = target.GetComponent<PlayerController>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. プレイヤーの背後に回り込みながら追従する基本移動
        Vector3 targetPosition = target.transform.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // 2. 常にプレイヤーを視野の中心に捉える回転
        transform.LookAt(target.position + Vector3.up * 1f);

        // 3. 💥 スピードに応じた視野角（FOV）の変化
        if (cam != null && targetRb != null)
        {
            float currentSpeed = targetRb.velocity.magnitude;
            float targetFOV = normalFOV;

            // プレイヤーが現在最高速度（ダッシュ）に向かっているか、ブレーキ中かを判定
            // ※リジッドボディの速度を基準に計算します
            if (currentSpeed > 21f)
            {
                // 通常の最高速（20）を超えている＝ロケットダッシュ中
                // スピードが上がれば上がるほど、maxFOVに向けてカメラが引く
                float speedFactor = Mathf.InverseLerp(20f, 40f, currentSpeed);
                targetFOV = Mathf.Lerp(normalFOV, maxFOV, speedFactor);
            }
            else if (currentSpeed < 5f)
            {
                // 速度がほぼゼロ＝プッシュブレーキでチャージ中
                // 停止に近づくほど、minFOVに向けてカメラがグッと寄る
                float stopFactor = Mathf.InverseLerp(5f, 0f, currentSpeed);
                targetFOV = Mathf.Lerp(normalFOV, minFOV, stopFactor);
            }

            // 計算した視野角へスムーズに変形させる
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSmoothSpeed * Time.deltaTime);
        }
    }
}