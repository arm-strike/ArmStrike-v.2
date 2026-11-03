using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PinSpawner : MonoBehaviour
{
    [Header("--- ピンの設定 ---")]
    [Tooltip("ランダムにスポーンさせたい複数種類のピンのプレハブを登録してください")]
    public List<GameObject> pinPrefabs = new List<GameObject>();

    [Header("--- スポーン範囲の設定 ---")]
    [Tooltip("このオブジェクトのトリガー内にある、'Floor' タグを持つオブジェクトの範囲内でランダムに配置します")]
    public string floorTag = "Floor";

    [Tooltip("同時に存在させたいピンの最大数")]
    public int maxPins = 10;

    [Tooltip("床の上からどれくらい浮かせた位置にスポーンさせるか")]
    public float spawnHeightOffset = 0.5f;

    [Header("--- 復活（リスポーン）の設定 ---")]
    [Tooltip("ピンが倒れてから消滅・復活するまでの時間（秒）")]
    public float respawnDelay = 5.0f;

    private List<GameObject> activePins = new List<GameObject>();
    private List<Collider> validFloors = new List<Collider>();

    void Start()
    {
        // 1. このオブジェクトのコライダーが「Is Trigger」になっているか確認・自動設定
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null)
        {
            myCollider.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("[PinSpawner] このオブジェクトに Collider がありません。BoxCollider などをアタッチしてください。");
        }

        // 2. トリガー範囲内にある（またはシーン内にある）"Floor" タグを持つコライダーを収集
        FindFloorsInTrigger();

        if (validFloors.Count == 0)
        {
            Debug.LogError("[PinSpawner] トリガー範囲内、またはシーン内に 'Floor' タグがついたコライダーが見つかりません！");
            return;
        }

        // 3. 初期状態で最大数分ピンをスポーンさせる
        StartCoroutine(SpawnInitialPinsRoutine());
    }

    // トリガー内に侵入、または最初から含まれているFloorを検知する
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(floorTag))
        {
            Collider floorCol = other.GetComponent<Collider>();
            if (floorCol != null && !validFloors.Contains(floorCol))
            {
                validFloors.Add(floorCol);
                Debug.Log($"[PinSpawner] Floorタグのオブジェクト '{other.gameObject.name}' をスポーン範囲として登録しました。");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(floorTag))
        {
            Collider floorCol = other.GetComponent<Collider>();
            if (floorCol != null && validFloors.Contains(floorCol))
            {
                validFloors.Remove(floorCol);
            }
        }
    }

    void FindFloorsInTrigger()
    {
        validFloors.Clear();

        // シーン内のすべてのオブジェクトから "Floor" タグを持つものを探す
        GameObject[] floorObjs = GameObject.FindGameObjectsWithTag(floorTag);
        Collider myTrigger = GetComponent<Collider>();

        foreach (var obj in floorObjs)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                // トリガー範囲が設定されていない、またはスポナーのトリガーコライダーと重なっている場合に追加
                if (myTrigger == null || myTrigger.bounds.Intersects(col.bounds))
                {
                    validFloors.Add(col);
                }
            }
        }
    }

    IEnumerator SpawnInitialPinsRoutine()
    {
        // 確実にFloorが検知されるまで1フレーム待つ
        yield return null;

        for (int i = 0; i < maxPins; i++)
        {
            SpawnRandomPin();
            yield return null;
        }
    }

    void SpawnRandomPin()
    {
        if (pinPrefabs == null || pinPrefabs.Count == 0 || validFloors.Count == 0) return;

        // 1. 有効なFloorリストからランダムに1つ床を選ぶ（複数の床エリアがある場合にも対応）
        Collider selectedFloor = validFloors[Random.Range(0, validFloors.Count)];
        if (selectedFloor == null) return;

        // 2. プレハブリストからランダムに1つ選択
        GameObject selectedPrefab = pinPrefabs[Random.Range(0, pinPrefabs.Count)];

        // 3. 選ばれた床のBounds（範囲）からランダムな X, Z 座標を算出
        Bounds bounds = selectedFloor.bounds;
        float randomX = Random.Range(bounds.min.x + 1f, bounds.max.x - 1f);
        float randomZ = Random.Range(bounds.min.z + 1f, bounds.max.z - 1f);
        float spawnY = bounds.max.y + spawnHeightOffset;

        Vector3 spawnPos = new Vector3(randomX, spawnY, randomZ);

        // 4. ピンを生成
        GameObject newPin = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
        activePins.Add(newPin);

        // 5. ピン側に復活監視用コンポーネントをアタッチ
        PinController pinCtrl = newPin.GetComponent<PinController>();
        if (pinCtrl == null)
        {
            pinCtrl = newPin.AddComponent<PinController>();
        }
        pinCtrl.Initialize(this, spawnPos, respawnDelay, selectedFloor);
    }

    // ピンが消滅・リスポーン対象になったときに呼ばれるコールバック
    public void OnPinDestroyed(Vector3 originalPos, Collider targetFloor)
    {
        StartCoroutine(RespawnRoutine(targetFloor));
    }

    IEnumerator RespawnRoutine(Collider targetFloor)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (pinPrefabs.Count > 0)
        {
            GameObject selectedPrefab = pinPrefabs[Random.Range(0, pinPrefabs.Count)];
            Collider floorToUse = (targetFloor != null && validFloors.Contains(targetFloor)) ? targetFloor :
                                  (validFloors.Count > 0 ? validFloors[Random.Range(0, validFloors.Count)] : null);

            if (floorToUse != null)
            {
                Bounds bounds = floorToUse.bounds;
                float randomX = Random.Range(bounds.min.x + 1f, bounds.max.x - 1f);
                float randomZ = Random.Range(bounds.min.z + 1f, bounds.max.z - 1f);
                float spawnY = bounds.max.y + spawnHeightOffset;
                Vector3 newSpawnPos = new Vector3(randomX, spawnY, randomZ);

                GameObject respawnedPin = Instantiate(selectedPrefab, newSpawnPos, Quaternion.identity);

                PinController pinCtrl = respawnedPin.GetComponent<PinController>();
                if (pinCtrl == null)
                {
                    pinCtrl = respawnedPin.AddComponent<PinController>();
                }
                pinCtrl.Initialize(this, newSpawnPos, respawnDelay, floorToUse);
            }
        }
    }
}