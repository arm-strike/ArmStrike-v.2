using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Enemy : MonoBehaviour
{
    [Header("--- ステータス ---")]
    public int maxHP = 10000;
    private int currentHP;

    [Header("--- 状態管理 ---")]
    private bool isDowned = false;  // HP0で倒れている状態
    private bool isDefeated = false; // 完全に撃破された状態

    [Header("--- UI参照 ---")]
    public SpriteNumberDisplay[] hpNumberDisplayUIs;

    [Header("--- ぶっ飛び設定 ---")]
    public float flyForceMultiplier = 40f;
    public float torqueForce = 1000f;

    [Header("--- SEの設定 ---")]
    public AudioClip defeatSE;
    [Range(0f, 1f)] public float seVolume = 1.0f;

    private Rigidbody rb;
    private AudioSource audioSource;

    public bool IsDefeated => isDefeated;
    public bool IsDowned => isDowned;

    void Start()
    {
        currentHP = maxHP;
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (rb != null) rb.isKinematic = true;
        UpdateHPDisplay();
    }

    // ダメージ処理 (詳細版)
    public bool TakeDamage(int damage, Vector3 hitPoint, Vector3 hitVelocity)
    {
        if (isDowned || isDefeated) return false;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        UpdateHPDisplay();

        // ▼▼▼ プレイヤーやギミックからダメージを受けた（殴られた）タイミングでチュートリアルに通知 ▼▼▼
        TutorialManager tutorialMgr = FindObjectOfType<TutorialManager>();
        if (tutorialMgr != null)
        {
            tutorialMgr.OnEnemyHit();
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        if (currentHP <= 0)
        {
            isDowned = true;
            Debug.Log("敵がダウン状態になった！プレイヤーの追撃を待機！");
        }
        return true;
    }

    // ダメージ処理 (BigStoneなどの簡易版)
    public void TakeDamage(int damage)
    {
        if (isDowned || isDefeated) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        UpdateHPDisplay();

        // ▼▼▼ プレイヤーやギミックからダメージを受けた（殴られた）タイミングでチュートリアルに通知 ▼▼▼
        TutorialManager tutorialMgr = FindObjectOfType<TutorialManager>();
        if (tutorialMgr != null)
        {
            tutorialMgr.OnEnemyHit();
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        if (currentHP <= 0)
        {
            isDowned = true;
            Debug.Log("敵がダウン状態になった！プレイヤーの追撃を待機！");
        }
    }

    // プレイヤーとの接触判定（ここが撃破のトリガー）
    private void OnCollisionEnter(Collision collision)
    {
        // ダウン状態で、プレイヤーがぶつかってきたら撃破！
        if (isDowned && !isDefeated && collision.gameObject.CompareTag("Player"))
        {
            // 衝突した方向を利用してぶっ飛ばす
            Vector3 contactDir = (transform.position - collision.transform.position).normalized;
            DefeatAndFly(contactDir * 10f); // 適当な勢いを付与
        }
    }

    private void DefeatAndFly(Vector3 hitVelocity)
    {
        isDowned = false;
        isDefeated = true;

        // ▼▼▼ 敵を完全に撃破した瞬間の通知が必要な場合はここに置くこともできます ▼▼▼
        // TutorialManager tutorialMgr = FindObjectOfType<TutorialManager>();
        // if (tutorialMgr != null) { tutorialMgr.OnEnemyHit(); } 
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        TutorialManager tutorialMgr = FindObjectOfType<TutorialManager>();
        if (tutorialMgr != null)
        {
            tutorialMgr.OnEnemyDefeated();
        }

        if (defeatSE != null && audioSource != null)
        {
            audioSource.PlayOneShot(defeatSE, seVolume);
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            Vector3 flyDirection = (hitVelocity.normalized + Vector3.up * 0.8f).normalized;
            float finalForce = Mathf.Max(hitVelocity.magnitude * flyForceMultiplier, 800f);

            rb.AddForce(flyDirection * finalForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
        }
    }

    void UpdateHPDisplay()
    {
        if (hpNumberDisplayUIs != null)
        {
            foreach (var ui in hpNumberDisplayUIs)
            {
                if (ui != null) ui.SetNumber(currentHP);
            }
        }
    }
}