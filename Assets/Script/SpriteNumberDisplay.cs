using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SpriteNumberDisplay : MonoBehaviour
{
    [Header("--- 数字スプライト素材 (0～9の順でアサイン) ---")]
    [Tooltip("0, 1, 2, 3, 4, 5, 6, 7, 8, 9 の順番で画像をセットしてください")]
    public Sprite[] numberSprites = new Sprite[10];

    [Header("--- UI表示・レイアウト設定 ---")]
    [Tooltip("数字1文字あたりの表示サイズ（幅, 高さ）")]
    public Vector2 digitSize = new Vector2(40f, 50f);

    [Tooltip("数字同士の間隔（ピクセル）")]
    public float digitSpacing = 35f;

    [Tooltip("中央揃えにするか（OFFの場合は左揃え）")]
    public bool centerAlign = true;

    // 動的に生成した桁Imageオブジェクトの保持リスト
    private List<GameObject> activeDigitObjects = new List<GameObject>();

    /// <summary>
    /// HPなどの数値を受け取り、数字画像UIを更新する
    /// </summary>
    public void SetNumber(int number)
    {
        ClearDigits();

        if (number < 0) number = 0;

        string numberString = number.ToString();
        int digitCount = numberString.Length;

        // 中央揃え用のX座標オフセットを計算
        float totalWidth = (digitCount - 1) * digitSpacing;
        float startX = centerAlign ? -totalWidth / 2f : 0f;

        for (int i = 0; i < digitCount; i++)
        {
            int digitValue = numberString[i] - '0'; // チャラクターを数値(0~9)に変換

            if (digitValue < 0 || digitValue >= numberSprites.Length || numberSprites[digitValue] == null)
            {
                continue;
            }

            // 1桁ごとのUI Image用GameObjectを作成
            GameObject digitObj = new GameObject($"Digit_{i}", typeof(RectTransform), typeof(Image));
            digitObj.transform.SetParent(transform, false);

            // RectTransformの設定
            RectTransform rect = digitObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + (i * digitSpacing), 0f);
            rect.sizeDelta = digitSize;

            // Imageの設定
            Image img = digitObj.GetComponent<Image>();
            img.sprite = numberSprites[digitValue];
            img.raycastTarget = false; // UIのクリック判定を無効化（軽量化）

            activeDigitObjects.Add(digitObj);
        }
    }

    /// <summary>
    /// 既存の数字UIを削除
    /// </summary>
    private void ClearDigits()
    {
        foreach (GameObject digitObj in activeDigitObjects)
        {
            if (digitObj != null)
            {
                Destroy(digitObj);
            }
        }
        activeDigitObjects.Clear();
    }
}