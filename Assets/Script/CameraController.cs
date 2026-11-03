using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("追跡するプレイヤー（本体）")]
    public Transform target;

    [Header("プレイヤーとの基本距離（位置オフセット）")]
    public Vector3 offset = new Vector3(0f, 6f, -12f);

    [Header("カメラが追従する滑らかさ")]
    public float smoothSpeed = 5f;

    [Header("【新規】Z軸のカメラ移動制限")]
    public bool enableZLimit = true;
    public float maxZLimit = 50f;

    [Header(" スピード連動のカスタマイズ")]
    public float minFOV = 50f;
    public float normalFOV = 60f;
    public float maxFOV = 80f;
    public float fovSmoothSpeed = 4f;

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

    // 【追加】ゲームクリア時にFOV設定を変更するメソッド
    public void SetGameClearFOV()
    {
        enableZLimit = false; // Z制限も解除
        minFOV = 60f;        // FOVを140に設定
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.transform.TransformPoint(offset);

        if (enableZLimit)
        {
            if (targetPosition.z > maxZLimit) targetPosition.z = maxZLimit;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1f);

        if (cam != null && targetRb != null)
        {
            float currentSpeed = targetRb.velocity.magnitude;
            float targetFOV = normalFOV;

            if (currentSpeed > 21f)
            {
                float speedFactor = Mathf.InverseLerp(20f, 40f, currentSpeed);
                targetFOV = Mathf.Lerp(normalFOV, maxFOV, speedFactor);
            }
            else if (currentSpeed < 5f)
            {
                float stopFactor = Mathf.InverseLerp(5f, 0f, currentSpeed);
                targetFOV = Mathf.Lerp(normalFOV, minFOV, stopFactor);
            }

            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSmoothSpeed * Time.deltaTime);
        }
    }
}