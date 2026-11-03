using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;

public class PlayerController : MonoBehaviour
{
    [Header("--- プレイヤー識別設定 ---")]
    [Tooltip("1Pなら 1、2Pなら 2 を設定してください")]
    public int playerID = 1;

    [Header("--- ステータス・HP設定 ---")]
    public int maxHP = 100;
    public int currentHP;
    [Tooltip("別プレイヤーの攻撃が当たったときに受けるダメージ量")]
    public int attackDamage = 20;

    [Header("--- HP表示UIの設定 ---")]
    public Text hpText;
    public TMP_Text hpTextTMP;

    [Header("--- コントローラーのトラッカー参照 ---")]
    public ControllerTracker leftTracker;
    public ControllerTracker rightTracker;

    [Header("--- 通常時の移動・自動前進設定 ---")]
    public float autoForwardSpeed = 5f;
    public float maxForwardSpeed = 20f;
    public float normalTurnSpeed = 90f;
    public float acceleration = 10f;

    [Header("--- プッシュ（チャージ）設定 ---")]
    public float pushBrakeFactor = 0.95f;
    public float pushTurnSpeed = 180f;

    [Header("--- チャージダッシュ設定 ---")]
    public float maxCharge = 100f;
    public float chargeRate = 150f;
    public float dashForceMultiplier = 0.4f;

    [Header("--- チャージ振動設定 ---")]
    public float shakeAmount = 0.05f;
    public Transform visualModel;

    [Header("--- エフェクトの設定 ---")]
    public ParticleSystem chargeEffect;
    public ParticleSystem dashTrailEffect;

    [Header("--- UIゲージの設定 ---")]
    public Slider chargeSlider;

    [Header("--- 速度表示UIの設定 ---")]
    public Text speedText;
    public TMP_Text speedTextTMP;

    [HideInInspector] public bool isGigaDash = false;

    [Header("Hit Effect Settings")]
    public GameObject hitEffectPrefab;
    public Transform effectSpawnPoint;

    [Header("--- ゲーム終了の設定 ---")]
    public float backFlyForce = 35f;
    public GameObject gameClearImageUI;
    public GameObject gameOverImageUI;
    public float displayDelaySeconds = 2.0f;

    [Header("--- 攻撃入力の猶予設定 ---")]
    public float bothAttackWindow = 0.15f;

    [Header("--- シーン遷移設定 ---")]
    [Tooltip("ステージ1クリア時の遷移先シーン名")]
    public string stage2SceneName = "Stage2";
    [Tooltip("ゲームクリア時またはゲームオーバー時の共通タイトルシーン名")]
    public string titleSceneName = "TitleScene";
    [HideInInspector] public bool canReturnToTitle = false;

    [HideInInspector] public bool enableMovement = true;

    public float CurrentSpeed => currentVelocity.magnitude;

    private Renderer ballRenderer;
    private Color originalColor;
    private bool isColorFlashing = false;

    private Image sliderFillImage;
    private Color sliderOriginalColor;

    private Rigidbody rb;
    private float currentForwardSpeed = 0f;
    private float currentCharge = 0f;
    private bool isPushing = false;
    private Vector3 currentVelocity;

    private Vector3 visualModelDefaultPos;
    private Camera mainCamera;
    private float defaultFOV;

    private Animator animator;

    private bool isGameOver = false;
    private bool isGameClear = false;

    private float leftAttackBufferTimer = 0f;
    private float rightAttackBufferTimer = 0f;

    void Start()
    {
        currentHP = maxHP;
        UpdateHPUI();

        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            defaultFOV = mainCamera.fieldOfView;
        }

        animator = GetComponentInChildren<Animator>();

        if (visualModel != null)
        {
            visualModelDefaultPos = visualModel.localPosition;
            ballRenderer = visualModel.GetComponent<Renderer>();
            if (ballRenderer != null)
            {
                originalColor = ballRenderer.material.color;
            }
        }
        else
        {
            visualModel = null;
        }

        currentForwardSpeed = autoForwardSpeed;

        if (leftTracker == null || rightTracker == null)
        {
            ControllerTracker[] trackers = GetComponentsInChildren<ControllerTracker>();
            foreach (var t in trackers)
            {
                if (t.transform.IsChildOf(transform))
                {
                    if (t.nodeRole == XRNode.LeftHand && leftTracker == null) leftTracker = t;
                    if (t.nodeRole == XRNode.RightHand && rightTracker == null) rightTracker = t;
                }
            }
        }

        if (chargeSlider != null)
        {
            chargeSlider.value = 0f;
            if (chargeSlider.fillRect != null)
            {
                sliderFillImage = chargeSlider.fillRect.GetComponent<Image>();
                if (sliderFillImage != null)
                {
                    sliderOriginalColor = sliderFillImage.color;
                }
            }
        }

        if (GetComponent<Collider>() != null)
        {
            PhysicMaterial PM = new PhysicMaterial
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum
            };
            GetComponent<Collider>().material = PM;
        }

        if (chargeEffect != null) chargeEffect.Stop();
        if (dashTrailEffect != null) dashTrailEffect.Stop();
    }

    void Update()
    {
        bool chargeHeld = false;
        float turnAxis = 0f;

        if (playerID == 1)
        {
            chargeHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Joystick1Button0);
        }
        else if (playerID == 2)
        {
            chargeHeld = Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.Joystick2Button0);
        }

        // --- 腕を下げたほうだけに直感的に回転する処理 ---
        if (leftTracker != null && rightTracker != null)
        {
            float leftDrop = leftTracker.DropAmount;   // 左手が下がった量
            float rightDrop = rightTracker.DropAmount; // 右手が下がった量

            float turnInput = 0f;
            float dropThreshold = 0.02f; // 反応する下がり幅の閾値

            // 右手を下げたら右回転（プラス）
            if (rightDrop > dropThreshold && rightDrop > leftDrop)
            {
                turnInput = Mathf.Clamp(rightDrop * 5.0f, 0.2f, 1f);
            }
            // 左手を下げたら左回転（マイナス）
            else if (leftDrop > dropThreshold && leftDrop > rightDrop)
            {
                turnInput = -Mathf.Clamp(leftDrop * 5.0f, 0.2f, 1f);
            }

            if (Mathf.Abs(turnInput) > 0.01f)
            {
                turnAxis = turnInput;
            }
        }

        bool leftAttackDown = (leftTracker != null && leftTracker.attackDown);
        bool rightAttackDown = (rightTracker != null && rightTracker.attackDown);

        if (isGameOver || isGameClear)
        {
            if (canReturnToTitle)
            {
                if (leftAttackDown || rightAttackDown || Input.anyKeyDown)
                {
                    if (isGameOver)
                    {
                        SceneManager.LoadScene(titleSceneName);
                    }
                    else if (isGameClear)
                    {
                        string currentSceneName = SceneManager.GetActiveScene().name;

                        if (currentSceneName == "Stage2")
                        {
                            SceneManager.LoadScene(titleSceneName);
                        }
                        else
                        {
                            SceneManager.LoadScene(stage2SceneName);
                        }
                    }
                }
            }
            return;
        }

        if (!enableMovement)
        {
            currentForwardSpeed = 0f;
            currentVelocity = Vector3.zero;
            if (rb != null) rb.velocity = Vector3.zero;
            return;
        }

        float currentTurnSpeed = chargeHeld ? pushTurnSpeed : normalTurnSpeed;

        // プレイヤー本体を回転させる
        if (turnAxis != 0f)
        {
            transform.Rotate(Vector3.up, turnAxis * currentTurnSpeed * Time.deltaTime, Space.Self);
        }

        if (chargeHeld)
        {
            isPushing = true;
            currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, 0f, pushBrakeFactor * Time.deltaTime * 5f);
            currentCharge = Mathf.MoveTowards(currentCharge, maxCharge, chargeRate * Time.deltaTime);

            float chargeRatio = currentCharge / maxCharge;
            float currentShake = shakeAmount * chargeRatio;
            if (visualModel != null)
            {
                visualModel.localPosition = visualModelDefaultPos + new Vector3(
                    Random.Range(-currentShake, currentShake),
                    Random.Range(-currentShake, currentShake),
                    Random.Range(-currentShake, currentShake)
                );
            }

            if (currentCharge >= maxCharge)
            {
                isColorFlashing = true;
                float flash = Mathf.PingPong(Time.time * 30f, 1f);
                Color flashColor = Color.Lerp(Color.white, new Color(0f, 0.8f, 1f), flash);

                if (ballRenderer != null) ballRenderer.material.color = flashColor;
                if (sliderFillImage != null) sliderFillImage.color = flashColor;
            }

            if (chargeEffect != null && !chargeEffect.isPlaying) chargeEffect.Play();
        }
        else
        {
            if (chargeEffect != null && chargeEffect.isPlaying)
            {
                chargeEffect.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            if (isPushing && currentCharge > 10f)
            {
                currentForwardSpeed = maxForwardSpeed + (currentCharge * dashForceMultiplier);

                TutorialManager tutorialMgr = FindObjectOfType<TutorialManager>();
                if (tutorialMgr != null && playerID == 1)
                {
                    tutorialMgr.AddDashCount();
                }

                if (mainCamera != null)
                {
                    float extraFOV = (currentCharge >= maxCharge) ? 35f : 0f;
                    mainCamera.fieldOfView = defaultFOV + (currentCharge * 0.3f) + extraFOV;
                }

                if (currentCharge >= maxCharge)
                {
                    isGigaDash = true;
                    if (dashTrailEffect != null && !dashTrailEffect.isPlaying) dashTrailEffect.Play();
                }

                isPushing = false;
            }

            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, autoForwardSpeed, acceleration * Time.deltaTime);
            currentCharge = Mathf.MoveTowards(currentCharge, 0f, chargeRate * Time.deltaTime * 2f);
            isPushing = false;

            if (currentForwardSpeed <= maxForwardSpeed + 2f)
            {
                isGigaDash = false;

                if (dashTrailEffect != null && dashTrailEffect.isPlaying)
                {
                    dashTrailEffect.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }

                if (isColorFlashing)
                {
                    if (ballRenderer != null) ballRenderer.material.color = originalColor;
                    if (sliderFillImage != null) sliderFillImage.color = sliderOriginalColor;

                    isColorFlashing = false;
                }
            }

            if (isGigaDash)
            {
                float flash = Mathf.PingPong(Time.time * 20f, 1f);
                Color dashFlashColor = Color.Lerp(originalColor, Color.yellow, flash);

                if (ballRenderer != null) ballRenderer.material.color = dashFlashColor;
                if (sliderFillImage != null) sliderFillImage.color = dashFlashColor;
            }
        }

        if (chargeSlider != null)
        {
            chargeSlider.value = currentCharge;
        }

        if (mainCamera != null && playerID == 1)
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, defaultFOV, Time.deltaTime * 4f);
        }

        Vector3 targetVelocity = transform.forward * currentForwardSpeed;
        float driftSmoothness = chargeHeld ? 8f : 3f;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * driftSmoothness);

        if (leftAttackDown) leftAttackBufferTimer = bothAttackWindow;
        if (rightAttackDown) rightAttackBufferTimer = bothAttackWindow;

        if (leftAttackBufferTimer > 0f) leftAttackBufferTimer -= Time.deltaTime;
        if (rightAttackBufferTimer > 0f) rightAttackBufferTimer -= Time.deltaTime;

        if (animator != null)
        {
            animator.SetBool("IsCharging", chargeHeld);
            animator.SetBool("IsGiga", isGigaDash);

            if (leftAttackBufferTimer > 0f && rightAttackBufferTimer > 0f)
            {
                animator.SetTrigger("AttackBoth");
                leftAttackBufferTimer = 0f;
                rightAttackBufferTimer = 0f;
            }
            else if (leftAttackDown)
            {
                animator.SetTrigger("AttackLeft");
            }
            else if (rightAttackDown)
            {
                animator.SetTrigger("AttackRight");
            }
        }

        if (rb != null)
        {
            rb.velocity = new Vector3(currentVelocity.x, rb.velocity.y, currentVelocity.z);
        }

        UpdateSpeedUI();
    }

    // --- トリガーによる腕の衝突・ダメージ判定（攻撃中の腕のみ有効） ---
    void OnTriggerEnter(Collider other)
    {
        if (isGameOver || isGameClear) return;

        // 1. 当たったオブジェクトが "PlayerArm" タグを持っているか確認
        if (other.CompareTag("PlayerArm"))
        {
            // 2. その腕の ControllerTracker と、持ち主（PlayerController）を取得
            ControllerTracker armTracker = other.GetComponentInParent<ControllerTracker>();
            PlayerController armOwner = other.GetComponentInParent<PlayerController>();

            // 3. 持ち主が存在し、自分以外であり、かつ「その腕が現在攻撃中（パンチを突き出している状態）である」場合のみダメージ
            if (armOwner != null && armOwner != this && armTracker != null && armTracker.IsAttacking)
            {
                TakeDamage(armOwner.attackDamage, transform.position);
                Debug.Log($"<color=orange>【被弾（攻撃中のみ）】 Player {playerID} が Player {armOwner.playerID} のパンチを受けた！ 残りHP: {currentHP}</color>");
            }
        }

        // --- 従来のピンや敵（Enemy）との判定 ---
        if (other.GetComponent<Pin>() != null || other.transform.root.GetComponent<Pin>() != null)
        {
            return;
        }

        if (TutorialManager.IsTutorialActive) return;

        GameObject enemyObj = (other.CompareTag("Enemy")) ? other.gameObject :
                            (other.transform.root != null && other.transform.root.CompareTag("Enemy")) ? other.transform.root.gameObject : null;

        if (enemyObj != null)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector3 hitPoint = other.transform.position;
                bool wasDownedBeforeHit = enemy.IsDowned;

                enemy.TakeDamage(Mathf.RoundToInt(Mathf.Max(currentVelocity.magnitude * 5f, 1f)), hitPoint, currentVelocity);
                PlayHitEffect(hitPoint);

                if (wasDownedBeforeHit || enemy.IsDowned || enemy.IsDefeated)
                {
                    currentForwardSpeed *= 0.6f;
                    TriggerGameClear();
                }
                else
                {
                    currentForwardSpeed *= 0.3f;
                    TriggerGameOver();
                }
            }
        }
    }

    public void TakeDamage(int damage, Vector3 hitPosition)
    {
        if (isGameOver || isGameClear) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);
        UpdateHPUI();

        PlayHitEffect(hitPosition);

        if (currentHP <= 0)
        {
            TriggerGameOver();
        }
    }

    private void UpdateHPUI()
    {
        string hpString = string.Format("HP: {0} / {1}", currentHP, maxHP);

        if (hpTextTMP != null)
        {
            hpTextTMP.text = hpString;
        }
        else if (hpText != null)
        {
            hpText.text = hpString;
        }
    }

    private void PlayHitEffect(Vector3 position)
    {
        if (animator != null) animator.SetTrigger("Hit");

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, position, Quaternion.identity);
        }
    }

    private void TriggerGameClear()
    {
        isGameClear = true;

        if (playerID == 1)
        {
            var camController = Camera.main.GetComponent<CameraController>();
            if (camController != null)
            {
                camController.SetGameClearFOV();
            }
        }

        StartCoroutine(GameClearSequenceRoutine());
    }

    private IEnumerator GameClearSequenceRoutine()
    {
        float timer = 0f;
        while (timer < 1.0f)
        {
            timer += Time.deltaTime;
            currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, 0f, Time.deltaTime * 2f);
            yield return null;
        }

        currentForwardSpeed = 0f;
        currentVelocity = Vector3.zero;
        if (rb != null) rb.velocity = Vector3.zero;

        yield return new WaitForSeconds(displayDelaySeconds);
        if (gameClearImageUI != null && playerID == 1) gameClearImageUI.SetActive(true);

        canReturnToTitle = true;
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        currentForwardSpeed = 0f;
        currentVelocity = Vector3.zero;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;

            Vector3 flyDirection = (Vector3.back + Vector3.up * 0.5f).normalized;
            rb.AddForce(flyDirection * backFlyForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 500f, ForceMode.Impulse);
        }

        StartCoroutine(GameOverSequenceRoutine());
    }

    private IEnumerator GameOverSequenceRoutine()
    {
        yield return new WaitForSeconds(displayDelaySeconds);
        if (gameOverImageUI != null && playerID == 1)
        {
            gameOverImageUI.SetActive(true);
        }

        canReturnToTitle = true;
    }

    private void UpdateSpeedUI()
    {
        float speed = currentVelocity.magnitude;
        string speedString = string.Format("SPEED: {0:F1}", speed);

        if (speedTextTMP != null)
        {
            speedTextTMP.text = speedString;
        }
        else if (speedText != null)
        {
            speedText.text = speedString;
        }
    }
}