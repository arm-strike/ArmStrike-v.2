using UnityEngine;

public class DebugForwardMove : MonoBehaviour
{
    [Header("--- 前進設定 ---")]
    [Tooltip("毎秒進むスピード")]
    public float moveSpeed = 5.0f;

    [Tooltip("Rigidbodyを使って物理挙動を伴って動かすか")]
    public bool useRigidbody = true;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Rigidbodyを使用しない場合はTransformで直接動かす
        if (!useRigidbody)
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
        }
    }

    void FixedUpdate()
    {
        // Rigidbodyを使用する場合はvelocityで動かす（コリジョンや物理判定を維持するため）
        if (useRigidbody && rb != null)
        {
            // 現在のY軸の速度（落下など）は維持しつつ、Z軸方向に前進させる
            Vector3 forwardVelocity = transform.forward * moveSpeed;
            rb.velocity = new Vector3(forwardVelocity.x, rb.velocity.y, forwardVelocity.z);
        }
    }
}