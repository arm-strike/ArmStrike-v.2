using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("--- 参照 ---")]
    public PlayerController playerController;

    [Header("--- 表示用UI ---")]
    [Tooltip("外枠などの背景画像（Wait Image・Start Image共通）を表示するImage用UI")]
    public Image waitFrameImageUI;

    [Tooltip("「腕を振ってスタート」の文字を表示するTMP_Text")]
    public TMP_Text waitTextUI;

    [Tooltip("「3」「2」「1」の数字を表示するImage用UI")]
    public Image countdownImageUI;

    [Header("--- イラスト（Sprite）設定 ---")]
    [Tooltip("待機時の外枠画像（Wait Image）")]
    public Sprite waitImage;
    [Tooltip("「3」の数字画像")]
    public Sprite image3;
    [Tooltip("「2」の数字画像")]
    public Sprite image2;
    [Tooltip("「1」の数字画像")]
    public Sprite image1;
    [Tooltip("「START!」の画像（Wait Imageと同じUIに表示）")]
    public Sprite startImage;

    private bool isGameStarted = false;
    private bool isWaitingForArmSwing = true; // 最初に腕を振るのを待っている状態

    void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        // ゲーム開始前はプレイヤーの移動を禁止しておく
        if (playerController != null)
        {
            playerController.enableMovement = false;
        }

        // 初期状態：外枠画像と「腕を振ってスタート」の文字を表示し、数字用画像は非表示にする
        if (waitFrameImageUI != null)
        {
            if (waitImage != null) waitFrameImageUI.sprite = waitImage;
            waitFrameImageUI.gameObject.SetActive(true);
        }

        if (waitTextUI != null)
        {
            waitTextUI.text = "腕を振ってスタート";
            waitTextUI.gameObject.SetActive(true);
        }

        if (countdownImageUI != null)
        {
            countdownImageUI.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // すでにゲームが始まっている、あるいはプレイヤーが見つからない場合はスルー
        if (isGameStarted || playerController == null) return;
    }

    /// <summary>
    /// **腕が振られたときに外部（PlayerControllerなど）から呼ぶメソッド**
    /// </summary>
    public void TriggerGameStart()
    {
        // まだ待機中でなければ何もしない
        if (!isWaitingForArmSwing || isGameStarted) return;

        isWaitingForArmSwing = false;

        // 3秒のカウントダウンコルーチンを開始
        StartCoroutine(StartCountdownRoutine());
    }

    // 3秒間のカウントダウンを行うコルーチン
    private IEnumerator StartCountdownRoutine()
    {
        // カウントダウン開始：外枠とテキストを非表示にして、数字用画像を表示する
        if (waitFrameImageUI != null)
        {
            waitFrameImageUI.gameObject.SetActive(false);
        }

        if (waitTextUI != null)
        {
            waitTextUI.gameObject.SetActive(false);
        }

        if (countdownImageUI != null)
        {
            countdownImageUI.gameObject.SetActive(true);
        }

        // --- 「3」の表示 ---
        if (countdownImageUI != null)
        {
            if (image3 != null) countdownImageUI.sprite = image3;
        }
        yield return new WaitForSeconds(1f);

        // --- 「2」の表示 ---
        if (countdownImageUI != null)
        {
            if (image2 != null) countdownImageUI.sprite = image2;
        }
        yield return new WaitForSeconds(1f);

        // --- 「1」の表示 ---
        if (countdownImageUI != null)
        {
            if (image1 != null) countdownImageUI.sprite = image1;
        }
        yield return new WaitForSeconds(1f);

        // --- 「START!」の表示（数字用を消し、外枠の画像を StartImage に切り替えて再表示） ---
        if (countdownImageUI != null)
        {
            countdownImageUI.gameObject.SetActive(false);
        }

        if (waitFrameImageUI != null)
        {
            if (startImage != null) waitFrameImageUI.sprite = startImage;
            waitFrameImageUI.gameObject.SetActive(true);
        }

        // プレイヤーの移動を許可！
        if (playerController != null)
        {
            playerController.enableMovement = true;
        }

        isGameStarted = true;

        // 「START!」を少し表示したあとに外枠ごと非表示にする（0.8秒後）
        yield return new WaitForSeconds(0.8f);
        if (waitFrameImageUI != null) // 正しい null チェック
        {
            waitFrameImageUI.gameObject.SetActive(false);
        }
    }
}