using UnityEngine;

// Android のゲーム中だけ、画面の自動消灯を止める。端末のシステム設定は変えない。
// バックグラウンドと終了時は、画面の点灯を OS の設定に戻す。
public sealed class ScreenAwake : MonoBehaviour
{
    const string PrefKey = "ScreenAwake";

    static ScreenAwake _instance;
    static bool _foreground = true;

    public static bool Enabled
    {
        get { return PlayerPrefs.GetInt(PrefKey, 1) != 0; }
        set
        {
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (_instance != null)
            return;
        GameObject obj = new GameObject("ScreenAwake");
        obj.AddComponent<ScreenAwake>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Apply();
    }

    void OnApplicationPause(bool paused)
    {
        _foreground = !paused;
        Apply();
    }

    void OnApplicationFocus(bool focused)
    {
        _foreground = focused;
        Apply();
    }

    void OnApplicationQuit()
    {
        _foreground = false;
        Apply();
    }

    void OnDestroy()
    {
        if (_instance != this)
            return;
        _instance = null;
#if UNITY_ANDROID && !UNITY_EDITOR
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
#endif
    }

    public static void Apply()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bool keep = _foreground && Enabled;
        Screen.sleepTimeout = keep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
#endif
    }
}
