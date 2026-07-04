using UnityEngine;
using TMPro; // ★ TextMeshProを使うために必要

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("スコアを表示するTextMeshProテキスト")]
    public TextMeshProUGUI scoreText;

    [Header("スコアの前に表示する文字")]
    public string scoreLabel = "SCORE: ";

    private int score = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        // ゲーム開始時にテキストを「SCORE: 0」に初期化
        UpdateScoreText();
    }

    // ピンが倒れたときに呼び出される関数
    public void AddScore(int points)
    {
        score += points;
        UpdateScoreText(); // スコアが増えるたびに表示を更新
    }

    // テキストの表示を更新する処理
    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = scoreLabel + score.ToString();
        }
    }
}