using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class BigStone : MonoBehaviour
{
    /*
    [Header("--- ギミック設定 ---")]
    [Tooltip("オラオラ殴りを受け付ける制限時間（秒）")]
    public float rushDuration = 3.0f;

    [Tooltip("1ヒットあたりの威力加算値")]
    public float powerPerHit = 25f;

    [Tooltip("敵追尾スピード")]
    public float homingSpeed = 90f;

    [Header("--- そびえ立ち（上昇）演出設定 ---")]
    [Tooltip("プレイヤー接近時に持ち上がる高さ（Y座標の加算分）")]
    public float riseHeight = 3.0f;

    [Tooltip("そびえ立ちにかかる時間（秒）")]
    public float riseDuration = 0.5f;

    [Tooltip("そびえ立ち時のY軸拡大倍率（縦に伸びる演出）")]
    public float scaleYMultiplier = 1.3f;

    [Header("--- タグ設定 ---")]
    public string enemyTag = "Enemy";
    public string playerTag = "Player";

    [Header("--- UI表示 (画像＆数字スプライト) ---")]
    [Tooltip("「ORA!」などの文字・アイコン画像（Canvas UIのRawImage）")]
    public RawImage displayImageUI;

    [Tooltip("数字（0～9）の画像スプライト素材 (順に0～9をアセット)")]
    public Sprite[] numberSprites = new Sprite[10];

    [Tooltip("数字の画像UIを生成・配置するための親となるUIオブジェクト（RectTransform）")]
    public RectTransform numberContainerUI;

    [Tooltip("数字1文字あたりのUI表示サイズ（幅, 高さ）")]
    public Vector2 digitSize = new Vector2(30f, 40f);

    [Tooltip("数字同士の間隔（ピクセル）")]
    public float digitSpacing = 25f;

    [Header("--- SE（効果音）の設定 ---")]
    [Tooltip("石を殴られている最中（オラオラ中）に毎ヒット鳴るSE")]
    public AudioClip hitSE;
    [Tooltip("制限時間が終了して敵へぶっとばす瞬間に鳴るSE")]
    public AudioClip launchSE;
    [Range(0f, 1f)] public float seVolume = 1.0f;

    [Header("--- エフェクト ---")]
    public ParticleSystem hitEffect;
    public ParticleSystem launchExplosionEffect;

    private Rigidbody rb;
    private bool isRushActive = false;
    private bool isLaunched = false;
    private int hitCount = 0;
    private float totalPower = 0f;

    private Vector3 initialPosition;
    private Vector3 risenPosition;
    private Vector3 initialScale;

    // 動的に生成した数字画像の保持リスト
    private List<GameObject> activeDigitObjects = new List<GameObject>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialScale = transform.localScale;
        initialPosition = transform.position;

        // そびえ立った後の目標位置
        risenPosition = initialPosition + Vector3.up * riseHeight;

        // 初期状態ではUI・画像を非表示
        SetUIVisibility(false);
    }

    // --- 1. プレイヤーが接近（トリガー進入）した時の処理 ---
    private void OnTriggerEnter(Collider other)
    {
        if (isRushActive || isLaunched) return;

        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            StartRushMode();
        }
    }

    // --- 2. プレイヤーが物理で衝突した時の処理 ---
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(playerTag) || collision.transform.root.CompareTag(playerTag))
        {
            if (!isRushActive && !isLaunched)
            {
                StartRushMode();
            }

            if (isRushActive && !isLaunched)
            {
                OnHit();
            }
        }
    }

    /// <summary>
    /// そびえ立ち動作 ＆ オラオラモード開始
    /// </summary>
    private void StartRushMode()
    {
        isRushActive = true;
        hitCount = 0;
        totalPower = 0f;

        // UIと画像を表示
        SetUIVisibility(true);
        UpdateHitCountUI(hitCount);

        // 1. そびえ立つアニメーションを開始
        StartCoroutine(RiseUpRoutine());

        // 2. 制限時間のカウントダウン開始
        StartCoroutine(RushTimerRoutine());
    }

    /// <summary>
    /// 石が上に持ち上がる（そびえ立つ）処理
    /// </summary>
    private IEnumerator RiseUpRoutine()
    {
        float timer = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetScale = new Vector3(initialScale.x, initialScale.y * scaleYMultiplier, initialScale.z);

        while (timer < riseDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / riseDuration);

            transform.position = Vector3.Lerp(startPos, risenPosition, t);
            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);

            yield return null;
        }

        transform.position = risenPosition;
    }

    /// <summary>
    /// 殴られた時の処理
    /// </summary>
    public void OnHit()
    {
        if (!isRushActive || isLaunched) return;

        hitCount++;
        totalPower += powerPerHit;

        if (hitEffect != null) hitEffect.Play();

        // 殴られている最中のSEを再生
        if (hitSE != null)
        {
            AudioSource.PlayClipAtPoint(hitSE, transform.position, seVolume);
        }

        // ポップアップアニメーション
        StopCoroutine("PunchScaleRoutine");
        StartCoroutine("PunchScaleRoutine");

        // 数字画像UIを更新
        UpdateHitCountUI(hitCount);

        if (HitStopManager.instance != null)
        {
            HitStopManager.instance.TriggerHitStop(0.04f);
        }
    }

    private IEnumerator PunchScaleRoutine()
    {
        Vector3 baseRisenScale = new Vector3(initialScale.x, initialScale.y * scaleYMultiplier, initialScale.z);
        transform.localScale = baseRisenScale * 1.15f;
        yield return new WaitForSeconds(0.05f);
        transform.localScale = baseRisenScale;
    }

    /// <summary>
    /// パンチ回数の数字画像UIを動的に生成・更新する
    /// </summary>
    private void UpdateHitCountUI(int count)
    {
        ClearDigits();

        if (numberContainerUI == null) return;

        string countString = count.ToString();
        int digitCount = countString.Length;

        float totalWidth = (digitCount - 1) * digitSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < digitCount; i++)
        {
            int digitValue = countString[i] - '0';

            if (digitValue < 0 || digitValue >= numberSprites.Length || numberSprites[digitValue] == null)
            {
                continue;
            }

            GameObject digitObj = new GameObject($"Digit_{i}", typeof(RectTransform), typeof(Image));
            digitObj.transform.SetParent(numberContainerUI, false);

            RectTransform rect = digitObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + (i * digitSpacing), 0f);
            rect.sizeDelta = digitSize;

            Image img = digitObj.GetComponent<Image>();
            img.sprite = numberSprites[digitValue];
            img.raycastTarget = false;

            activeDigitObjects.Add(digitObj);
        }
    }

    private void ClearDigits()
    {
        foreach (GameObject obj in activeDigitObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        activeDigitObjects.Clear();
    }

    private void SetUIVisibility(bool visible)
    {
        if (displayImageUI != null) displayImageUI.gameObject.SetActive(visible);

        if (numberContainerUI != null)
        {
            numberContainerUI.gameObject.SetActive(visible);
            if (!visible)
            {
                ClearDigits();
            }
        }
    }

    /// <summary>
    /// 制限時間終了後にぶっ飛ばす
    /// </summary>
    private IEnumerator RushTimerRoutine()
    {
        yield return new WaitForSeconds(rushDuration);

        isRushActive = false;
        isLaunched = true;

        // ▼▼▼ ステップ4（岩をぶっとばす）のチュートリアル完了を通知 ▼▼▼
        TutorialManager tutorialMgr = FindObjectOfType<TutorialManager>();
        if (tutorialMgr != null)
        {
            tutorialMgr.OnRockDestroyed();
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        // UIと画像を非表示
        SetUIVisibility(false);

        if (launchExplosionEffect != null) launchExplosionEffect.Play();

        // ぶっとばす瞬間のSEを再生
        if (launchSE != null)
        {
            AudioSource.PlayClipAtPoint(launchSE, transform.position, seVolume);
        }

        Transform targetEnemy = GetNearestEnemyTransform();

        if (targetEnemy != null)
        {
            StartCoroutine(LaunchAndHomingRoutine(targetEnemy));
        }
        else
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce((transform.forward + Vector3.up * 0.5f).normalized * (totalPower * 2f), ForceMode.Impulse);
            }
            Destroy(gameObject, 3.0f);
        }
    }

    /// <summary>
    /// 敵への追尾と大ダメージ
    /// </summary>
    private IEnumerator LaunchAndHomingRoutine(Transform target)
    {
        if (rb != null) rb.isKinematic = true;

        Vector3 startPos = transform.position;
        Vector3 peakPos = startPos + Vector3.up * 5.0f;
        float launchTimer = 0f;

        while (launchTimer < 0.25f)
        {
            launchTimer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, peakPos, launchTimer / 0.25f);
            yield return null;
        }

        float finalSpeed = homingSpeed + (hitCount * 3f);

        while (target != null)
        {
            Vector3 targetPos = target.position + Vector3.up * 0.5f;
            Vector3 dir = (targetPos - transform.position).normalized;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, finalSpeed * Time.deltaTime);
            if (dir != Vector3.zero) transform.forward = dir;

            if (Vector3.Distance(transform.position, targetPos) < 1.5f)
            {
                int finalDamage = Mathf.RoundToInt(totalPower);

                Enemy enemy = target.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(finalDamage);
                }

                if (ScoreManager.instance != null)
                {
                    ScoreManager.instance.AddScore(finalDamage * 2);
                }
                break;
            }
            yield return null;
        }

        if (launchExplosionEffect != null)
        {
            Instantiate(launchExplosionEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private Transform GetNearestEnemyTransform()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (enemies == null || enemies.Length == 0) return null;

        GameObject nearestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy != null ? nearestEnemy.transform : null;
    }*/
}