using UnityEngine;
using System.Collections;

public class Pin : MonoBehaviour
{
    [Header("倒れたと判定する傾き角度")]
    public float numberOfDegrees = 45f;

    [Header("消えるまでの時間（秒）")]
    public float destroyDelay = 2f;

    [Header("このピンの得点")]
    public int pinScore = 10;

    [Header(" 【新規】ピンのぶっ飛ぶパワー")]
    public float explosionForce = 30f;

    [Header(" 【新規】スコアポップアップのPrefab")]
    public GameObject scorePopupPrefab;

    private Rigidbody rb;
    private bool isFallen = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (isFallen) return;

        float angle = Vector3.Angle(transform.up, Vector3.up);
        if (angle > numberOfDegrees)
        {
            StartCoroutine(FallRoutine());
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isFallen) return;

        // ぶつかった相手からPlayerControllerを取得
        PlayerController player = collision.transform.root.GetComponent<PlayerController>();
        bool isHitByPlayer = collision.gameObject.CompareTag("Player") || player != null;

        if (isHitByPlayer)
        {
            if (rb != null)
            {
                // 【超絶強化】プレイヤーが「ギガ・ボルト・ダッシュ」状態だった場合
                if (player != null && player.isGigaDash)
                {
                    // 1. スコアをいつもの「2倍」にする！
                    pinScore *= 2;

                    // 2. 特大ヒットストップを発生（一瞬の最大のタメ）
                    if (HitStopManager.instance != null)
                    {
                        HitStopManager.instance.TriggerHitStop(0.15f);
                    }

                    // 3. 上空へ音速ですっ飛ばす
                    rb.velocity = (Vector3.up * 2f + transform.forward).normalized * (explosionForce * 2.5f);
                    rb.angularVelocity = new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50));

                    // 4. 即座に消滅ルーティンへ（通常より早く消します）
                    destroyDelay = 0.4f;
                    StartCoroutine(FallRoutine());
                    return; // 通常の処理をスキップ
                }

                // --- 以下は以前作った通常＆HardPinの処理（そのまま維持） ---
                if (gameObject.CompareTag("HardPin"))
                {
                    if (HitStopManager.instance != null)
                    {
                        HitStopManager.instance.TriggerHitStop(0.3f);
                    }
                }

                Vector3 hitDirection = (transform.position - collision.transform.position).normalized;
                Vector3 launchVelocity = (hitDirection + Vector3.up * 0.5f).normalized * explosionForce;
                rb.velocity = launchVelocity;
                rb.angularVelocity = new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));
                StartCoroutine(FallRoutine());
            }
        }
    }

    private IEnumerator FallRoutine()
    {
        isFallen = true;

        // 1. スコア加算
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddScore(pinScore);
        }

        // 2. ★演出：ピンの頭上にスコアポップアップを生成
        if (scorePopupPrefab != null)
        {
            // ピンの少し上の位置
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
            GameObject popupObj = Instantiate(scorePopupPrefab, spawnPos, Quaternion.identity);

            // ポップアップに点数を教える
            ScorePopup popup = popupObj.GetComponent<ScorePopup>();
            if (popup != null)
            {
                popup.Setup(pinScore);
            }
        }

        // 3. 指定秒数待ってから消去
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}