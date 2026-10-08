using System;
using System.Runtime.InteropServices;
using UnityEngine;

// エディタではファイル選択ウィンドウ、スマホでは端末のファイル選択を開く。
// スマホ側は実機でまだ確認していない。
public class PngFilePicker : MonoBehaviour
{
    public const string ReceiverName = "PngFilePickerReceiver";

    static PngFilePicker _instance;
    Action<string> _onPicked;
    bool _waiting;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void PngPicker_Open(string receiver);
#endif

    public static void Ensure()
    {
        if (_instance != null)
            return;
        GameObject receiver = new GameObject(ReceiverName);
        _instance = receiver.AddComponent<PngFilePicker>();
        DontDestroyOnLoad(receiver);
    }

    // 選んだらパス。キャンセルは空文字。この環境で開けないときは null。
    public static void Pick(Action<string> onPicked)
    {
        Ensure();
        _instance._onPicked = onPicked;
        _instance._waiting = true;

#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("PNGを選ぶ", "", "png");
        _instance.Finish(path);
#elif UNITY_ANDROID
        try
        {
            using (AndroidJavaClass picker = new AndroidJavaClass("com.idlecolorsrpg.pngimport.PngPicker"))
                picker.CallStatic("open", ReceiverName);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PNGのファイル選択を開けませんでした。 " + exception.Message);
            _instance.Finish(null);
        }
#elif UNITY_IOS
        try
        {
            PngPicker_Open(ReceiverName);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PNGのファイル選択を開けませんでした。 " + exception.Message);
            _instance.Finish(null);
        }
#else
        _instance.Finish(null);
#endif
    }

    public void OnPickedPath(string path)
    {
        Finish(path);
    }

    void Finish(string path)
    {
        if (!_waiting)
            return;
        _waiting = false;
        Action<string> callback = _onPicked;
        _onPicked = null;
        if (callback != null)
            callback(path);
    }
}
