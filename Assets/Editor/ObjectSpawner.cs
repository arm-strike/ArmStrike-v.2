using UnityEngine;
using UnityEditor;

public class ObjectSpawner : EditorWindow
{
    public GameObject targetPrefab;
    public int count = 10;
    public float spacing = 2.0f;

    [MenuItem("Tools/オブジェクトを横に並べるツール")]
    public static void ShowWindow()
    {
        GetWindow<ObjectSpawner>("横並びツール");
    }

    private void OnGUI()
    {
        GUILayout.Label("オブジェクトを横一列に簡単配置", EditorStyles.boldLabel);

        targetPrefab = (GameObject)EditorGUILayout.ObjectField("並べたいプレハブ", targetPrefab, typeof(GameObject), false);
        count = EditorGUILayout.IntField("個数", count);
        spacing = EditorGUILayout.FloatField("間隔 (Spacing)", spacing);

        if (GUILayout.Button("一気に生成する！"))
        {
            SpawnObjects();
        }
    }

    private void SpawnObjects()
    {
        if (targetPrefab == null)
        {
            Debug.LogWarning("プレハブが設定されていません！");
            return;
        }

        // 既存の親オブジェクトがあればまとめる
        GameObject parentObj = new GameObject("SpawnedObjects_Parent");

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = new Vector3(i * spacing, 0f, 0f);
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
            obj.transform.position = spawnPos;
            obj.transform.SetParent(parentObj.transform);
        }

        Debug.Log($"{count}個のオブジェクトを横に並べました！");
    }
}