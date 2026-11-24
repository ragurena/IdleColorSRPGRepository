using System;
using System.IO;
using UnityEngine;

// .piqua の選択と、X などへの共有。Android は ShareStagePlugin、エディタはファイルダイアログ。
public class ShareStageFiles : MonoBehaviour
{
    public const string ReceiverName = "ShareStageFilesReceiver";

    static ShareStageFiles _instance;
    Action<string> _onPicked;
    bool _waiting;

    public static void Ensure()
    {
        if (_instance != null)
            return;
        GameObject receiver = new GameObject(ReceiverName);
        _instance = receiver.AddComponent<ShareStageFiles>();
        DontDestroyOnLoad(receiver);
    }

    public static void Pick(Action<string> onPicked)
    {
        Ensure();
        _instance._onPicked = onPicked;
        _instance._waiting = true;
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("共有ステージを選ぶ", "", "piqua");
        _instance.Finish(path);
#elif UNITY_ANDROID
        try
        {
            using (AndroidJavaClass plugin = new AndroidJavaClass("com.idlecolorsrpg.sharestage.ShareStagePlugin"))
                plugin.CallStatic("pick", ReceiverName);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("共有ステージのファイル選択を開けませんでした。 " + exception.Message);
            _instance.Finish(null);
        }
#else
        _instance.Finish(null);
#endif
    }

    public static void SaveFile(byte[] body, string displayName, Action<bool> onDone)
    {
        if (body == null)
        {
            if (onDone != null)
                onDone(false);
            return;
        }
        Ensure();
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanel("共有ステージを保存", "", displayName, "piqua");
        if (string.IsNullOrEmpty(path))
        {
            if (onDone != null)
                onDone(false);
            return;
        }
        try
        {
            File.WriteAllBytes(path, body);
            if (onDone != null)
                onDone(true);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("共有ステージを保存できませんでした。 " + exception.Message);
            if (onDone != null)
                onDone(false);
        }
#elif UNITY_ANDROID
        try
        {
            string cache = CacheFile("export.piqua");
            File.WriteAllBytes(cache, body);
            _instance._onPicked = path =>
            {
                if (onDone != null)
                    onDone(!string.IsNullOrEmpty(path));
            };
            _instance._waiting = true;
            using (AndroidJavaClass plugin = new AndroidJavaClass("com.idlecolorsrpg.sharestage.ShareStagePlugin"))
                plugin.CallStatic("save", ReceiverName, cache, displayName);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("共有ステージを保存できませんでした。 " + exception.Message);
            if (onDone != null)
                onDone(false);
        }
#else
        if (onDone != null)
            onDone(false);
#endif
    }

    public static void Share(string text, string imagePath)
    {
#if UNITY_EDITOR
        GUIUtility.systemCopyBuffer = text ?? "";
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            UnityEditor.EditorUtility.RevealInFinder(imagePath);
        Debug.Log("共有文をコピーしました。");
#elif UNITY_ANDROID
        try
        {
            using (AndroidJavaClass plugin = new AndroidJavaClass("com.idlecolorsrpg.sharestage.ShareStagePlugin"))
                plugin.CallStatic("share", text ?? "", imagePath ?? "");
        }
        catch (Exception exception)
        {
            Debug.LogWarning("共有シートを開けませんでした。 " + exception.Message);
        }
#endif
    }

    public static string CacheFile(string name)
    {
        string directory = Application.temporaryCachePath + "/sharestage";
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        return directory + "/" + name;
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
