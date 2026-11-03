using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleManager : MonoBehaviour
{
    [Header("--- 遷移先シーン名 ---")]
    public string tutorialSceneName = "Tutorial";
    [Header("--- チュートリアルへ進むためのピンのノルマ本数 ---")]
    public int requiredPinCount = 3;

    [Header("--- フェードアウトの設定 ---")]
    public Image fadePanel;             // 画面を覆う黒いImage（UI）
    public float waitBeforeFade = 1.5f; // ピンを倒し終わってからフェードを開始するまでの待ち時間（秒）
    public float fadeDuration = 1.0f;   // フェードアウトにかかる時間（秒）

    private int knockedPinCount = 0;
    private bool isTransitioning = false;

    void Start()
    {
        // ゲーム開始時にフェードパネルが透明になっているか確認・設定
        if (fadePanel != null)
        {
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
            fadePanel.gameObject.SetActive(false);
        }
    }

    // --- スペースキー判定の Update() は完全に削除しました ---

    // タイトルシーンにあるピンが1本倒れるごとに呼ぶメソッド
    public void AddKnockedPin()
    {
        if (isTransitioning) return;

        knockedPinCount++;
        Debug.Log("タイトルピン撃破: " + knockedPinCount + " / " + requiredPinCount);

        if (knockedPinCount >= requiredPinCount)
        {
            // ノルマ達成した瞬間、自動でフェード付きシーン遷移コルーチンを開始
            StartCoroutine(TransitionRoutine());
        }
    }

    // 一定時間待ってからフェードアウトし、シーンを切り替えるコルーチン
    private IEnumerator TransitionRoutine()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        // 1. ピンが倒れたあとの余韻（数秒待つ）
        yield return new WaitForSeconds(waitBeforeFade);

        // 2. フェードアウト処理
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            float timer = 0f;
            Color startColor = fadePanel.color;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Clamp01(timer / fadeDuration);
                fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        // 3. シーン切り替え
        SceneManager.LoadScene(tutorialSceneName);
    }
}