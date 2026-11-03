using UnityEngine;

public class BGMManager : MonoBehaviour
{
    // シーンを跨いでBGMマネージャーを1つだけ保つためのインスタンス
    public static BGMManager instance;

    private AudioSource audioSource;

    void Awake()
    {
        // すでに別のBGMManagerが存在しているかチェック
        if (instance == null)
        {
            instance = this;
            // シーンが切り替わってもこのオブジェクトを破壊しない
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 2つ目以降の重複したBGMManagerが生成された場合は、新しい方を消去して二重再生を防ぐ
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // 再生中でなければ再生を開始
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    // BGMを停止させたい時用のメソッド
    public void StopBGM()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}