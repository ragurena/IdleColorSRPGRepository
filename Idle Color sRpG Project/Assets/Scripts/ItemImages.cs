using System.IO;
using UnityEngine;

// アイテムの絵。エディタでは Assets/Images を優先し、端末では StreamingAssets/item からコピーしたものを読む。
public static class ItemImages
{
    public const string EnmusubiFile = "enmusubinoomamori.png";

    static Sprite _enmusubi;

    public static Sprite Enmusubi()
    {
        if (_enmusubi != null)
            return _enmusubi;
        Texture2D texture = Read(EnmusubiFile);
        if (texture == null)
            return null;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        _enmusubi = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        return _enmusubi;
    }

    public static void CopyFromFolder(string sourceDirectory)
    {
        if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return;
        string destDir = Path.Combine(Application.persistentDataPath, "item");
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);
        string[] files = Directory.GetFiles(sourceDirectory, "*.png", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
        {
            string dest = Path.Combine(destDir, Path.GetFileName(files[i]));
            File.Copy(files[i], dest, true);
        }
    }

    static Texture2D Read(string fileName)
    {
#if UNITY_EDITOR
        string images = Path.Combine(Application.dataPath, "Images", fileName);
        if (File.Exists(images))
            return Load(images);
#endif
        string persistent = Path.Combine(Application.persistentDataPath, "item", fileName);
        if (File.Exists(persistent))
            return Load(persistent);
        string streaming = Path.Combine(Application.streamingAssetsPath, "item", fileName);
        if (File.Exists(streaming))
            return Load(streaming);
        return null;
    }

    static Texture2D Load(string path)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
                return texture;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("アイテム画像を読めませんでした。 " + path + " " + exception.Message);
        }
        return null;
    }
}
