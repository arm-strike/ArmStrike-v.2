using UnityEngine;

public class ArmColliderProxy : MonoBehaviour
{
    private ControllerTracker tracker;
    private PlayerController ownerPlayer;

    [Header("--- ピンを弾き飛ばす力 ---")]
    public float pinHitForce = 10f;

    void Start()
    {
        tracker = GetComponentInParent<ControllerTracker>();
        ownerPlayer = GetComponentInParent<PlayerController>();

        Debug.Log($"[ArmColliderProxy] 初期化完了 ({gameObject.name}) -> 持ち主PlayerID: {(ownerPlayer != null ? ownerPlayer.playerID.ToString() : "null")}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (tracker == null || ownerPlayer == null) return;

        // 1. ピンに当たった場合の処理
        Pin pin = other.GetComponent<Pin>() ?? other.transform.root.GetComponent<Pin>();
        if (pin != null)
        {
            pin.SetLastHitPlayer(ownerPlayer);
            Rigidbody pinRb = pin.GetComponent<Rigidbody>();
            if (pinRb != null)
            {
                Vector3 pushDir = (other.transform.position - transform.position).normalized + Vector3.up * 0.5f;
                pinRb.AddForce(pushDir * pinHitForce, ForceMode.Impulse);
            }
            return;
        }

        // 2. 他のプレイヤーに当たった場合
        PlayerController hitPlayer = other.GetComponentInParent<PlayerController>();
        if (hitPlayer != null)
        {
            Debug.Log($"[ArmColliderProxy] プレイヤーに接触: 自分のID={ownerPlayer.playerID}, 相手のID={hitPlayer.playerID}, IsAttacking={tracker.IsAttacking}");

            if (hitPlayer != ownerPlayer)
            {
                if (tracker.IsAttacking)
                {
                    hitPlayer.TakeDamage(ownerPlayer.attackDamage, other.transform.position);
                    Debug.Log($"<color=orange>【ヒット成功】 Player {ownerPlayer.playerID} のパンチが Player {hitPlayer.playerID} に命中！</color>");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>【不成立】 相手には触れましたが、IsAttacking が false のためダメージが入りません。</color>");
                }
            }
            else
            {
                Debug.Log("[ArmColliderProxy] 自分自身の体に触れました（無視）。");
            }
        }
    }
}