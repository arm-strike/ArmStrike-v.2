using UnityEngine;
using TMPro;

public class ScorePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float moveSpeed = 2f;     // 上に浮かび上がるスピード
    private float alphaSpeed = 1.5f;   // 消えていくスピード
    private Color textColor;

    void Awake()
    {
        // 3D空間用のTextMeshProを取得
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textColor = textMesh.color;
        }
    }

    // 表示する点数をセットする関数
    public void Setup(int scoreAmount)
    {
        if (textMesh != null)
        {
            textMesh.text = "+" + scoreAmount.ToString();
        }
    }

    void Update()
    {
        // 1. フワフワと上に移動させる
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // 2. カメラの方向を常に正面にむける（ビルボード処理）
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }

        // 3. 徐々に透明にして、完全に消えたらオブジェクトを削除する
        textColor.a -= alphaSpeed * Time.deltaTime;
        if (textMesh != null)
        {
            textMesh.color = textColor;
        }

        if (textColor.a <= 0)
        {
            Destroy(gameObject);
        }
    }
}