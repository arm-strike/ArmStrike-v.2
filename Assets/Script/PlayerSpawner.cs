using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("--- プレイヤーのプレハブ ---")]
    public GameObject player1Prefab;
    public GameObject player2Prefab;

    [Header("--- スポーンエリアの設定（範囲） ---")]
    public float minX = -6f;
    public float maxX = 6f;
    public float minZ = -6f;
    public float maxZ = 6f;
    public float spawnY = 0f; // 地面の高さ

    [Header("--- プレイヤー同士の最低距離 ---")]
    public float minDistanceBetweenPlayers = 4.0f;

    void Start()
    {
        SpawnPlayers();
    }

    void SpawnPlayers()
    {
        // 1Pのランダムな位置と向きを決定
        Vector3 p1Position = new Vector3(Random.Range(minX, maxX), spawnY, Random.Range(minZ, maxZ));
        Quaternion p1Rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // 2Pの位置を決定（1Pから一定以上離れるようにループで安全に離す）
        Vector3 p2Position;
        int safetyCounter = 0;
        do
        {
            p2Position = new Vector3(Random.Range(minX, maxX), spawnY, Random.Range(minZ, maxZ));
            safetyCounter++;
            if (safetyCounter > 100) break;
        }
        while (Vector3.Distance(p1Position, p2Position) < minDistanceBetweenPlayers);

        Quaternion p2Rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // 1Pを生成
        if (player1Prefab != null)
        {
            // ★重要：プレハブ自体のローカル座標のズレを防ぐため、インスタンス化後に明示的に位置を代入
            GameObject p1 = Instantiate(player1Prefab, p1Position, p1Rotation);
            p1.transform.position = p1Position;

            PlayerController p1Controller = p1.GetComponent<PlayerController>();
            if (p1Controller != null) p1Controller.playerID = 1;
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] Player1 Prefabがアサインされていません！");
        }

        // 2Pを生成
        if (player2Prefab != null)
        {
            GameObject p2 = Instantiate(player2Prefab, p2Position, p2Rotation);
            p2.transform.position = p2Position;

            PlayerController p2Controller = p2.GetComponent<PlayerController>();
            if (p2Controller != null) p2Controller.playerID = 2;
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] Player2 Prefabがアサインされていません！");
        }
    }
}