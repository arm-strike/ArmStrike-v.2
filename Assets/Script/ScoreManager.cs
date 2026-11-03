using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("--- 各プレイヤーのスコア ---")]
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();

    [Header("--- UI表示設定 ---")]
    [Tooltip("各プレイヤーのスコアを表示するUIテキスト（1P, 2Pの順に登録）")]
    public List<TMP_Text> scoreTextTMPs = new List<TMP_Text>();
    public List<Text> scoreTexts = new List<Text>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddScore(int playerID, int amount)
    {
        if (!playerScores.ContainsKey(playerID))
        {
            playerScores[playerID] = 0;
        }

        playerScores[playerID] += amount;
        Debug.Log($"<color=yellow>【スコア加算】 Player {playerID} に +{amount}点！ 現在のスコア: {playerScores[playerID]}</color>");

        UpdateScoreUI(playerID);
    }

    public int GetScore(int playerID)
    {
        return playerScores.ContainsKey(playerID) ? playerScores[playerID] : 0;
    }

    private void UpdateScoreUI(int playerID)
    {
        int score = GetScore(playerID);
        int index = playerID - 1; // 1Pならインデックス0

        if (index >= 0 && index < scoreTextTMPs.Count && scoreTextTMPs[index] != null)
        {
            scoreTextTMPs[index].text = $"P{playerID} Score: {score}";
        }

        if (index >= 0 && index < scoreTexts.Count && scoreTexts[index] != null)
        {
            scoreTexts[index].text = $"P{playerID} Score: {score}";
        }
    }
}