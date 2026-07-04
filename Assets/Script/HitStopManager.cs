using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager instance;

    private bool isWaiting = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    // ヒットストップを発動させる関数（duration: 止める時間）
    public void TriggerHitStop(float duration)
    {
        // すでにヒットストップ中なら重ねて実行しない
        if (isWaiting) return;

        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        isWaiting = true;

        // ★Unity全体の時間の進み方を「完全にゼロ」にする
        Time.timeScale = 0f;

        // Time.timeScaleが0の時はTime.deltaTimeも0になってしまうため、
        // 現実世界の時間（WaitForSecondsRealtime）で指定秒数待つ
        yield return new WaitForSecondsRealtime(duration);

        // ★Unity全体の時間を「元の1倍速」に戻す
        Time.timeScale = 1f;

        isWaiting = false;
    }
}