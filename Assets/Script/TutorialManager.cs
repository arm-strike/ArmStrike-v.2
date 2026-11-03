using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    // 他のスクリプトから「今チュートリアル中か？」を判定するためのフラグ
    public static bool IsTutorialActive { get; private set; } = false;

    [Header("--- 参照 ---")]
    public PlayerController playerController;
    public TMP_Text tutorialTextUI; // 画面上の指示テキスト用UI

    private int currentStep = 1;

    // ステップ1用（腕振り5回）
    private int armSwingCount = 0;

    // ステップ2用（ピン30本）
    private int knockedPinCount = 0;

    // ステップ3用（チャージダッシュ3回）
    private int chargeDashCount = 0;

    [Header("--- 各ステップごとの移動先座標 (X, Y, Z) ---")]
    [Tooltip("ステップ2開始時のプレイヤーの位置")]
    public Vector3 step2Position = new Vector3(15f, 0f, 0f);
    [Tooltip("ステップ3開始時のプレイヤーの位置")]
    public Vector3 step3Position = new Vector3(30f, 0f, 0f);
    [Tooltip("ステップ4開始時のプレイヤーの位置")]
    public Vector3 step4Position = new Vector3(45f, 0f, 0f);
    [Tooltip("ステップ5開始時のプレイヤーの位置")]
    public Vector3 step5Position = new Vector3(60f, 0f, 0f);

    [Header("--- 遷移先 ---")]
    public string nextSceneName = "SampleScene"; // すべてクリアした後のシーン名

    // --- スライドショー用の設定（GameObjectの配列） ---
    [System.Serializable]
    public struct StepObjects
    {
        [Header("このステップの解説用オブジェクト（複数設定可能）")]
        public GameObject[] explanationObjects;
    }

    [Header("--- 各ステップの解説オブジェクト設定 (インデックス0=ステップ1 … インデックス4=ステップ5) ---")]
    public StepObjects[] stepExplanationObjects = new StepObjects[5];

    [Header("--- スライドショー用UI参照 ---")]
    public GameObject imagePanelObject;   // 画像を表示するパネル全体のオブジェクト（非表示切り替え用）

    private bool isReadingExplanation = true; // 現在、解説画像を読んでいる最中か
    private int currentImageIndex = 0;        // 現在表示しているオブジェクトのインデックス

    void OnEnable()
    {
        IsTutorialActive = true;
    }

    void OnDisable()
    {
        IsTutorialActive = false;
    }

    void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        // 初期状態：ステップ1なので移動を禁止して解説からスタート
        if (playerController != null)
        {
            playerController.enableMovement = false;
        }

        // 最初のステップの解説状態をセット
        SetupStepStart();
    }

    void Update()
    {
        if (playerController == null) return;

        // 【解説画像フェーズ】のとき、SPACEキーでもスライドをめくれるようにする
        if (isReadingExplanation)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NotifyArmSwing();
            }
            return;
        }

        // 【通常の操作フェーズ】の処理
        switch (currentStep)
        {
            case 1:
                break;
            case 2:
                {
                    Pin[] remainingPins = FindObjectsOfType<Pin>();
                    if (remainingPins.Length == 0 && knockedPinCount < 30)
                    {
                        Debug.Log("ピンが足りなくなりました！ステージをやり直します。");
                        RetryStage();
                    }
                }
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                break;
        }
    }

    // 各ステップが始まったときの解説フェーズ初期化
    private void SetupStepStart()
    {
        isReadingExplanation = true;
        currentImageIndex = 0;

        if (playerController != null)
        {
            playerController.enableMovement = false;
        }

        UpdateInstructionUI();
    }

    // --- 外部（プレイヤーの腕振りセンサーやキー入力等）から呼び出す共通メソッド ---
    public void NotifyArmSwing()
    {
        // 1. 解説画像フェーズ中の場合：腕を振る（またはスペースキー）ことでスライドをめくる
        if (isReadingExplanation)
        {
            OnExplanationArmSwung();
            return;
        }

        // 2. 実操作フェーズ中の場合：ステップ1なら腕振りカウンターを回す
        if (currentStep == 1)
        {
            AddArmSwing();
        }
    }

    // 解説画像フェーズ中に腕が振られた / スペースキーが押されたときの処理（次のオブジェクトへ）
    private void OnExplanationArmSwung()
    {
        int arrayIdx = currentStep - 1;
        if (arrayIdx >= 0 && arrayIdx < stepExplanationObjects.Length)
        {
            GameObject[] objs = stepExplanationObjects[arrayIdx].explanationObjects;
            if (objs != null && objs.Length > 0)
            {
                // まだ最後のオブジェクトでなければ次のオブジェクトへ
                if (currentImageIndex < objs.Length - 1)
                {
                    currentImageIndex++;
                    UpdateInstructionUI();
                    return;
                }
            }
        }

        // 最後のオブジェクトまで見終わった状態でさらにアクションがあったら、実操作フェーズへ移行！
        isReadingExplanation = false;

        // 画像パネル全体を非表示にする
        if (imagePanelObject != null)
        {
            imagePanelObject.SetActive(false);
        }
        else
        {
            // パネルがない場合は個別のオブジェクトをすべて非表示にする
            HideAllExplanationObjects();
        }

        // ステップ1のみ、操作フェーズでも移動は禁止（腕振りカウンターのため）、それ以外は移動許可
        if (playerController != null)
        {
            playerController.enableMovement = (currentStep != 1);
        }

        UpdateInstructionUI();
    }

    // --- 各種外部から呼べる完了用パブリックメソッド ---

    public void AddArmSwing()
    {
        if (isReadingExplanation || currentStep != 1) return;

        armSwingCount++;
        UpdateInstructionUI();

        if (armSwingCount >= 5)
        {
            if (playerController != null)
            {
                playerController.enableMovement = true;
            }
            AdvanceStep(step2Position);
        }
    }

    public void AddKnockedPin()
    {
        if (isReadingExplanation || currentStep != 2) return;

        knockedPinCount++;
        UpdateInstructionUI();

        if (knockedPinCount >= 30)
        {
            AdvanceStep(step3Position);
        }
    }

    public void AddDashCount()
    {
        if (isReadingExplanation || currentStep != 3) return;

        chargeDashCount++;
        UpdateInstructionUI();

        if (chargeDashCount >= 3)
        {
            AdvanceStep(step4Position);
        }
    }

    public void OnRockDestroyed()
    {
        if (isReadingExplanation || currentStep != 4) return;
        AdvanceStep(step5Position);
    }

    public void OnEnemyDefeated()
    {
        if (isReadingExplanation || currentStep != 5) return;
        StartCoroutine(WaitAndFinishTutorial(2.0f));
    }

    private IEnumerator WaitAndFinishTutorial(float waitTime)
    {
        currentStep++;
        yield return new WaitForSeconds(waitTime);

        IsTutorialActive = false;
        SceneManager.LoadScene(nextSceneName);
    }

    public void RetryStage()
    {
        IsTutorialActive = false;
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    private void AdvanceStep(Vector3 nextPos)
    {
        currentStep++;

        if (playerController != null)
        {
            playerController.transform.position = nextPos;
        }

        SetupStepStart();
    }

    public void OnEnemyHit()
    {
    }

    // すべてのステップの解説オブジェクトを強制非表示にするヘルパー
    private void HideAllExplanationObjects()
    {
        foreach (var stepObj in stepExplanationObjects)
        {
            if (stepObj.explanationObjects != null)
            {
                foreach (var obj in stepObj.explanationObjects)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }
        }
    }

    private void UpdateInstructionUI()
    {
        int arrayIdx = currentStep - 1;
        if (arrayIdx >= 0 && arrayIdx < stepExplanationObjects.Length)
        {
            GameObject[] objs = stepExplanationObjects[arrayIdx].explanationObjects;

            if (isReadingExplanation && objs != null && objs.Length > 0)
            {
                if (imagePanelObject != null) imagePanelObject.SetActive(true);

                // 配列内のオブジェクトを順番にオン/オフ切り替え
                for (int i = 0; i < objs.Length; i++)
                {
                    if (objs[i] != null)
                    {
                        objs[i].SetActive(i == currentImageIndex);
                    }
                }
            }
            else
            {
                if (imagePanelObject != null) imagePanelObject.SetActive(false);
                HideAllExplanationObjects();
            }
        }

        if (tutorialTextUI == null) return;

        if (isReadingExplanation)
        {
            int totalObjs = 1;
            if (arrayIdx >= 0 && arrayIdx < stepExplanationObjects.Length && stepExplanationObjects[arrayIdx].explanationObjects != null)
            {
                totalObjs = Mathf.Max(1, stepExplanationObjects[arrayIdx].explanationObjects.Length);
            }
            int currentObjNum = currentImageIndex + 1;

            tutorialTextUI.text = string.Format("【ステップ {0} / 5 : 解説】\n腕を振る または [SPACE] キーでスライドを進めよう！\n({1} / {2}枚目)", currentStep, currentObjNum, totalObjs);
        }
        else
        {
            switch (currentStep)
            {
                case 1:
                    tutorialTextUI.text = string.Format("腕を5回振ってみよう！\n({0} / 5)", armSwingCount);
                    break;
                case 2:
                    tutorialTextUI.text = string.Format("ピンを倒そう！({0} / 30 本)", knockedPinCount);
                    break;
                case 3:
                    tutorialTextUI.text = string.Format("チャージダッシュを決めろ！\n({0} / 3 回)", chargeDashCount);
                    break;
                case 4:
                    tutorialTextUI.text = "目の前の岩をぶっとばそう！";
                    break;
                case 5:
                    tutorialTextUI.text = "敵をぶっとばして次のステージへ進こう！";
                    break;
            }
        }
    }
}