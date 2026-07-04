using UnityEngine;
using UnityEngine.UI; // ★UIを操作するために必要です

public class PlayerController : MonoBehaviour
{
    [Header("--- 通常時の移動設定 ---")]
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

    [Header("--- 腕のオブジェクト設定 ---")]
    public Transform leftArm;
    public Transform rightArm;
    public float swingSpeed = 500f;

    [Header("--- 攻撃時の腕のカスタマイズ ---")]
    public float attackScaleMultiplier = 1.5f;
    public float attackOffsetAmount = 1.0f;

    [Header("💥 チャージ振動設定 ---")]
    public float shakeAmount = 0.05f;
    public Transform visualModel;

    [Header("💥 エフェクトの設定")]
    public ParticleSystem chargeEffect;
    public ParticleSystem dashTrailEffect;

    [Header("📊 【新規】UIゲージの設定")]
    public Slider chargeSlider; // ★ここに作ったSliderをセットします

    // ギガ・ボルト・ダッシュ用の変数
    [HideInInspector] public bool isGigaDash = false;
    private Renderer ballRenderer;
    private Color originalColor;
    private bool isColorFlashing = false;

    // ★ ゲージの元の色を記憶する用
    private Image sliderFillImage;
    private Color sliderOriginalColor;

    private Rigidbody rb;
    private float currentForwardSpeed = 0f;
    private float currentCharge = 0f;
    private bool isPushing = false;
    private Vector3 currentVelocity;

    private Quaternion leftDefaultRot;
    private Quaternion rightDefaultRot;
    private Vector3 leftDefaultScale;
    private Vector3 rightDefaultScale;
    private Vector3 leftDefaultPos;
    private Vector3 rightDefaultPos;
    private Vector3 visualModelDefaultPos;
    private Camera mainCamera;
    private float defaultFOV;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        if (mainCamera != null) { defaultFOV = mainCamera.fieldOfView; }

        leftDefaultRot = Quaternion.Euler(0, -90, 0);
        rightDefaultRot = Quaternion.Euler(0, -90, 0);
        leftDefaultScale = leftArm.localScale;
        rightDefaultScale = rightArm.localScale;
        leftDefaultPos = leftArm.localPosition;
        rightDefaultPos = rightArm.localPosition;

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
            visualModel = transform;
            visualModelDefaultPos = Vector3.zero;
        }

        // ★ ゲージの塗りつぶし画像とその色を取得
        if (chargeSlider != null)
        {
            chargeSlider.value = 0f; // 最初は0
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
        bool isTurningRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        bool isTurningLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool isAttackingLeft = Input.GetKey(KeyCode.J);
        bool isAttackingRight = Input.GetKey(KeyCode.K);
        bool pushInput = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Space);

        float currentTurnSpeed = pushInput ? pushTurnSpeed : normalTurnSpeed;
        if (isTurningRight) transform.Rotate(Vector3.up, currentTurnSpeed * Time.deltaTime);
        else if (isTurningLeft) transform.Rotate(Vector3.up, -currentTurnSpeed * Time.deltaTime);

        // --- 3. プッシュ（チャージ）の計算 ＆ ボルトオーラ演出 ---
        if (pushInput)
        {
            isPushing = true;
            currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, 0f, pushBrakeFactor * Time.deltaTime * 5f);
            currentCharge = Mathf.MoveTowards(currentCharge, maxCharge, chargeRate * Time.deltaTime);

            float chargeRatio = currentCharge / maxCharge;
            float currentShake = shakeAmount * chargeRatio;
            visualModel.localPosition = visualModelDefaultPos + new Vector3(
                Random.Range(-currentShake, currentShake),
                Random.Range(-currentShake, currentShake),
                Random.Range(-currentShake, currentShake)
            );

            // 演出：チャージがMAXになったら電気バチバチ点滅（プレイヤー ＆ ゲージ）
            if (currentCharge >= maxCharge)
            {
                isColorFlashing = true;
                float flash = Mathf.PingPong(Time.time * 30f, 1f);
                Color flashColor = Color.Lerp(Color.white, new Color(0f, 0.8f, 1f), flash);

                // プレイヤーの点滅
                if (ballRenderer != null) ballRenderer.material.color = flashColor;
                // ★ 新規：ゲージの点滅
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

            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, maxForwardSpeed, acceleration * Time.deltaTime);
            currentCharge = Mathf.MoveTowards(currentCharge, 0f, chargeRate * Time.deltaTime * 2f);
            isPushing = false;

            // 通常速度に近づいたらオーラモードとトレイル、点滅を解除
            if (currentForwardSpeed <= maxForwardSpeed + 2f)
            {
                isGigaDash = false;

                if (dashTrailEffect != null && dashTrailEffect.isPlaying)
                {
                    dashTrailEffect.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }

                if (isColorFlashing)
                {
                    // プレイヤーの色を元に戻す
                    if (ballRenderer != null) ballRenderer.material.color = originalColor;
                    // ★ 新規：ゲージの色を元に戻す
                    if (sliderFillImage != null) sliderFillImage.color = sliderOriginalColor;

                    isColorFlashing = false;
                }
            }

            // マックスダッシュ中は、移動中も弾丸っぽく輝かせ続ける
            if (isGigaDash)
            {
                float flash = Mathf.PingPong(Time.time * 20f, 1f);
                Color dashFlashColor = Color.Lerp(originalColor, Color.yellow, flash);

                if (ballRenderer != null) ballRenderer.material.color = dashFlashColor;
                if (sliderFillImage != null) sliderFillImage.color = dashFlashColor;
            }

            visualModel.localPosition = visualModelDefaultPos;
        }

        // ★ 新規：チャージの値をゲージのUIにリアルタイム反映
        if (chargeSlider != null)
        {
            chargeSlider.value = currentCharge;
        }

        if (mainCamera != null)
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, defaultFOV, Time.deltaTime * 4f);
        }

        Vector3 targetVelocity = transform.forward * currentForwardSpeed;
        float driftSmoothness = pushInput ? 8f : 3f;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * driftSmoothness);

        // --- 5. アーム連動 ---
        if (isAttackingLeft)
        {
            leftArm.localRotation = Quaternion.RotateTowards(leftArm.localRotation, Quaternion.Euler(0, -30, 0), swingSpeed * Time.deltaTime);
            leftArm.localScale = leftDefaultScale * attackScaleMultiplier;
            leftArm.localPosition = leftDefaultPos + new Vector3(-attackOffsetAmount, 0f, 0f);
        }
        else if (pushInput)
        {
            leftArm.localRotation = Quaternion.RotateTowards(leftArm.localRotation, Quaternion.Euler(45f, -90, 0), swingSpeed * Time.deltaTime);
            leftArm.localScale = leftDefaultScale; leftArm.localPosition = leftDefaultPos;
        }
        else if (isTurningLeft)
        {
            leftArm.localRotation = Quaternion.RotateTowards(leftArm.localRotation, Quaternion.Euler(25f, -90, 0), swingSpeed * Time.deltaTime);
            leftArm.localScale = leftDefaultScale; leftArm.localPosition = leftDefaultPos;
        }
        else
        {
            leftArm.localRotation = Quaternion.RotateTowards(leftArm.localRotation, leftDefaultRot, swingSpeed * Time.deltaTime);
            leftArm.localScale = leftDefaultScale; leftArm.localPosition = leftDefaultPos;
        }

        if (isAttackingRight)
        {
            rightArm.localRotation = Quaternion.RotateTowards(rightArm.localRotation, Quaternion.Euler(0, -150, 0), swingSpeed * Time.deltaTime);
            rightArm.localScale = rightDefaultScale * attackScaleMultiplier;
            rightArm.localPosition = rightDefaultPos + new Vector3(attackOffsetAmount, 0f, 0f);
        }
        else if (pushInput)
        {
            rightArm.localRotation = Quaternion.RotateTowards(rightArm.localRotation, Quaternion.Euler(-45f, -90, 0), swingSpeed * Time.deltaTime);
            rightArm.localScale = rightDefaultScale; rightArm.localPosition = rightDefaultPos;
        }
        else if (isTurningRight)
        {
            rightArm.localRotation = Quaternion.RotateTowards(rightArm.localRotation, Quaternion.Euler(-25f, -90, 0), swingSpeed * Time.deltaTime);
            rightArm.localScale = rightDefaultScale; rightArm.localPosition = rightDefaultPos;
        }
        else
        {
            rightArm.localRotation = Quaternion.RotateTowards(rightArm.localRotation, rightDefaultRot, swingSpeed * Time.deltaTime);
            rightArm.localScale = rightDefaultScale; rightArm.localPosition = rightDefaultPos;
        }

        rb.velocity = new Vector3(currentVelocity.x, rb.velocity.y, currentVelocity.z);
    }
}