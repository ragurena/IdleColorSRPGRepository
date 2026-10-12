using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 人の顔。Assets/StreamingAssets/human の正方形 PNG。
// 起動時に persistentDataPath/human へコピーし、そちらを優先して読む。
public static class PersonPortraits
{
    static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

    public static string FileName(int id)
    {
        if (id == PersonIds.Protagonist)
            return "syujinnkou_kaoaikon.png";
        if (id == PersonIds.TeacherBlack)
            return "gensirou_kaoaikon.png";
        if (id == PersonIds.ClassmateGreen)
            return "chigusa_kaoaikon.png";
        if (id == PersonIds.ClassmateRed)
            return "moa_kaoaikon.png";
        if (id == PersonIds.ClassmateBlue)
            return "souga_kaoaikon.png";
        return null;
    }

    public static Sprite SpriteOf(int id)
    {
        Sprite cached;
        if (Cache.TryGetValue(id, out cached) && cached != null)
            return cached;

        string fileName = FileName(id);
        if (string.IsNullOrEmpty(fileName))
            return null;

        Texture2D texture = Read(fileName);
        if (texture == null)
            return null;

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        Cache[id] = sprite;
        return sprite;
    }

    public static void CopyFromFolder(string sourceDirectory)
    {
        if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return;

        string destDir = Path.Combine(Application.persistentDataPath, "human");
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
        string persistent = Path.Combine(Application.persistentDataPath, "human", fileName);
        if (File.Exists(persistent))
            return Load(persistent);

        string streaming = Path.Combine(Application.streamingAssetsPath, "human", fileName);
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
        catch (System.Exception e)
        {
            Debug.LogWarning("[PersonPortraits] 読めませんでした: " + path + " " + e.Message);
        }
        return null;
    }
}
