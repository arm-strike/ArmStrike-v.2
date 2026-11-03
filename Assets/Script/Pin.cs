using UnityEngine;
using System.Collections;

public class Pin : MonoBehaviour
{
    [Header("倒れたと判定する傾き角度")]
    public float numberOfDegrees = 45f;

    [Header("消えるまでの時間（倒れた後の滞留秒数）")]
    public float destroyDelay = 2.0f;

    [Header("このピンの得点")]
    public int pinScore = 10;

    [Header("ピンのぶっ飛ぶパワー（通常時ベース）")]
    public float explosionForce = 30f;

    [Header("回転の勢い倍率")]
    public float spinMultiplier = 50f;

    [Header("--- ヒットストップ秒数の設定 ---")]
    public float gigaHitStopDuration = 0.15f;
    public float hardPinHitStopDuration = 0.3f;

    [Header("スコアポップアップのPrefab")]
    public GameObject scorePopupPrefab;

    [Header("--- エフェクトの設定 ---")]
    public GameObject hitByPlayerEffectPrefab;

    [Header("--- SE（効果音）の設定 ---")]
    public AudioClip normalPinHitSE;  // 通常ピンが弾かれた時のSE
    public AudioClip specialPinHitSE; // 特殊ピン（HardPinなど）が弾かれた時のSE
    [Range(0f, 1f)] public float seVolume = 1.0f; // 音量

    private Rigidbody rb;
    private bool isFallen = false;
    private PlayerController lastHitPlayer; // 最後に当てたプレイヤー

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // 腕（ArmColliderProxy）などから呼ばれてヒットしたプレイヤーを記録する
    public void SetLastHitPlayer(PlayerController player)
    {
        if (player != null)
        {
            lastHitPlayer = player;
        }
    }

    void Update()
    {
        if (isFallen) return;

        // 1. 角度による倒れた判定
        float angle = Vector3.Angle(transform.up, Vector3.up);
        if (angle > numberOfDegrees)
        {
            TriggerFallProcess();
        }
    }

    // OnCollisionEnter の代わりに OnTriggerEnter を使用（腕が Is Trigger のため）
    void OnTriggerEnter(Collider other)
    {
        if (isFallen) return;

        PlayerController player = other.transform.root.GetComponent<PlayerController>();
        bool isHitByPlayer = other.CompareTag("Player") || other.CompareTag("PlayerArm") || player != null;

        if (isHitByPlayer)
        {
            if (player != null)
            {
                lastHitPlayer = player;
            }

            // ArmColliderProxy からプレイヤーが設定されている場合も考慮
            ArmColliderProxy proxy = other.GetComponent<ArmColliderProxy>();
            // (ArmColliderProxy側でPin.SetLastHitPlayerが呼ばれているためここではそのまま進行)

            if (hitByPlayerEffectPrefab != null)
            {
                Instantiate(hitByPlayerEffectPrefab, transform.position, Quaternion.identity);
            }

            bool isHardPin = gameObject.CompareTag("HardPin");
            AudioClip clipToPlay = isHardPin ? specialPinHitSE : normalPinHitSE;
            if (clipToPlay != null)
            {
                AudioSource.PlayClipAtPoint(clipToPlay, transform.position, seVolume);
            }

            if (rb != null)
            {
                bool isGiga = (player != null && player.isGigaDash);
                float impactPlayerSpeed = (player != null) ? player.CurrentSpeed : 20f;

                if (isGiga)
                {
                    pinScore *= 2;
                    if (HitStopManager.instance != null)
                    {
                        HitStopManager.instance.TriggerHitStop(gigaHitStopDuration);
                    }
                }
                else if (isHardPin)
                {
                    if (HitStopManager.instance != null)
                    {
                        HitStopManager.instance.TriggerHitStop(hardPinHitStopDuration);
                    }
                }

                Vector3 hitDirection = (transform.position - other.transform.position).normalized;
                Vector3 launchDirection = (hitDirection + Vector3.up * 0.5f).normalized;

                float spinForce = impactPlayerSpeed * spinMultiplier;
                rb.AddTorque(transform.right * spinForce, ForceMode.Impulse);

                float baseForce = isGiga ? explosionForce * 2.5f : explosionForce;
                float finalForce = baseForce * (impactPlayerSpeed / 15f);

                rb.velocity = launchDirection * finalForce;
                TriggerFallProcess();
            }
        }
    }

    private void TriggerFallProcess()
    {
        if (isFallen) return;
        isFallen = true;

        // 4. スコア加算とポップアップ演出
        if (ScoreManager.Instance != null)
        {
            int targetPlayerID = (lastHitPlayer != null) ? lastHitPlayer.playerID : 1;
            ScoreManager.Instance.AddScore(targetPlayerID, pinScore);
        }

        if (scorePopupPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
            GameObject popupObj = Instantiate(scorePopupPrefab, spawnPos, Quaternion.identity);

            ScorePopup popup = popupObj.GetComponent<ScorePopup>();
            if (popup != null)
            {
                popup.Setup(pinScore);
            }
        }

        // 6. 自動消滅（リスポーン対応の基盤）
        StartCoroutine(DestroyRoutine(destroyDelay));
    }

    private IEnumerator DestroyRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}