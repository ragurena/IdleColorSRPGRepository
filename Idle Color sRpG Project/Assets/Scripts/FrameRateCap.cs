using UnityEngine;

// Android 実機だけ描画FPSを30に上限する。
// 生産・戦闘などの進行は Time.deltaTime / 独自タイマー基準のまま変えず、描画間隔だけ抑える。
public sealed class FrameRateCap : MonoBehaviour
{
    const int AndroidTargetFps = 30;

    static FrameRateCap _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_instance != null)
            return;
        GameObject obj = new GameObject("FrameRateCap");
        obj.AddComponent<FrameRateCap>();
#endif
    }

    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Apply();
#else
        Destroy(gameObject);
#endif
    }

    void OnApplicationFocus(bool focused)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (focused)
            Apply();
#endif
    }

    void OnApplicationPause(bool paused)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!paused)
            Apply();
#endif
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    static void Apply()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Medium 品質などでは vSyncCount=1 のため端末リフレッシュに同期する。
        // targetFrameRate を効かせるには垂直同期を切る。
        if (QualitySettings.vSyncCount != 0)
            QualitySettings.vSyncCount = 0;
        if (Application.targetFrameRate != AndroidTargetFps)
            Application.targetFrameRate = AndroidTargetFps;
#endif
    }
}
