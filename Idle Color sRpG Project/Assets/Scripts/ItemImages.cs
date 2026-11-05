using System.Collections.Generic;
using System.IO;
using UnityEngine;

// アイテムの絵。エディタでは Assets/Images を優先し、端末では StreamingAssets/item からコピーしたものを読む。
public static class ItemImages
{
    public const string EnmusubiFile = "enmusubinoomamori.png";

    static readonly Dictionary<int, Sprite> Sprites = new Dictionary<int, Sprite>();

    public static Sprite Enmusubi()
    {
        return SpriteOf(ItemIds.EnmusubiOmamori);
    }

    public static Sprite SpriteOf(int itemId)
    {
        Sprite cached;
        if (Sprites.TryGetValue(itemId, out cached) && cached != null)
            return cached;
        string fileName = FileName(itemId);
        if (string.IsNullOrEmpty(fileName))
            return null;
        Texture2D texture = Read(fileName);
        if (texture == null)
            return null;
        bool pixel = itemId != ItemIds.EnmusubiOmamori;
        texture.filterMode = pixel ? FilterMode.Point : FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        Sprites[itemId] = sprite;
        return sprite;
    }

    public static string FileName(int itemId)
    {
        switch (itemId)
        {
            case ItemIds.ExpOrb8: return "keikentidama8.png";
            case ItemIds.ExpOrb16: return "keikentidama16.png";
            case ItemIds.ExpOrb32: return "keikentidama32.png";
            case ItemIds.ExpOrb64: return "keikentidama64.png";
            case ItemIds.ExpOrb128: return "keikentidama128.png";
            case ItemIds.EnmusubiOmamori: return EnmusubiFile;
            default: return null;
        }
    }

    public static string[] FileNames()
    {
        return new string[]
        {
            "keikentidama8.png",
            "keikentidama16.png",
            "keikentidama32.png",
            "keikentidama64.png",
            "keikentidama128.png",
            EnmusubiFile
        };
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
