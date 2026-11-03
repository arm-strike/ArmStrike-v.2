using UnityEngine;
using System.Collections;

public class PinController : MonoBehaviour
{
    private PinSpawner spawner;
    private Vector3 originalPosition;
    private float destroyDelay;
    private Collider currentFloor;
    private bool isDead = false;

    // 最後にこのピンに触れたプレイヤー
    private PlayerController lastHitPlayer;

    [Header("--- 倒れた判定の閾値 ---")]
    public float fallAngleThreshold = 45f;

    [Header("--- スコア設定 ---")]
    public int pinScoreValue = 100; // ピン1本あたりのスコア

    public void Initialize(PinSpawner spawnerRef, Vector3 pos, float delay, Collider floorCol)
    {
        spawner = spawnerRef;
        originalPosition = pos;
        destroyDelay = delay;
        currentFloor = floorCol;
    }

    // 腕が当たったときにプレイヤー情報を記録するメソッド
    public void SetLastHitPlayer(PlayerController player)
    {
        if (player != null)
        {
            lastHitPlayer = player;
        }
    }

    void Update()
    {
        if (isDead) return;

        float angle = Vector3.Angle(transform.up, Vector3.up);

        if (angle > fallAngleThreshold || transform.position.y < originalPosition.y - 2f)
        {
            StartCoroutine(PinDeathRoutine());
        }
    }

    IEnumerator PinDeathRoutine()
    {
        isDead = true;

        // 最後に倒したプレイヤーがいればスコアを加算
        if (lastHitPlayer != null && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(lastHitPlayer.playerID, pinScoreValue);
        }

        yield return new WaitForSeconds(1.0f);

        if (spawner != null)
        {
            spawner.OnPinDestroyed(originalPosition, currentFloor);
        }

        Destroy(gameObject);
    }
}