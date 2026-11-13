using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 章バナーとページ背景。エディタでは Assets/Images/story を優先し、端末では StreamingAssets/story からコピーしたものを読む。
public static class StoryImages
{
    static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

    public static Sprite SpriteOf(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;
        Sprite cached;
        if (Sprites.TryGetValue(fileName, out cached) && cached != null)
            return cached;
        Texture2D texture = Read(fileName);
        if (texture == null)
            return null;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        Sprites[fileName] = sprite;
        return sprite;
    }

    public static string[] FileNames()
    {
        List<string> names = new List<string>();
        StoryChapterDef[] chapters = StoryCatalog.All;
        if (chapters == null)
            return new string[0];
        for (int c = 0; c < chapters.Length; c++)
        {
            StoryChapterDef chapter = chapters[c];
            if (chapter == null)
                continue;
            Add(names, chapter.BannerFile);
            if (chapter.Pages == null)
                continue;
            for (int i = 0; i < chapter.Pages.Length; i++)
            {
                StoryPageDef page = chapter.Pages[i];
                if (page != null)
                    Add(names, page.BackgroundFile);
            }
        }
        return names.ToArray();
    }

    public static void CopyFromFolder(string sourceDirectory)
    {
        if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return;
        string destDir = Path.Combine(Application.persistentDataPath, "story");
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);
        string[] files = Directory.GetFiles(sourceDirectory, "*.png", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
        {
            string dest = Path.Combine(destDir, Path.GetFileName(files[i]));
            File.Copy(files[i], dest, true);
        }
    }

    static void Add(List<string> names, string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || names.Contains(fileName))
            return;
        names.Add(fileName);
    }

    static Texture2D Read(string fileName)
    {
#if UNITY_EDITOR
        string images = Path.Combine(Application.dataPath, "Images", "story", fileName);
        if (File.Exists(images))
            return Load(images);
#endif
        string persistent = Path.Combine(Application.persistentDataPath, "story", fileName);
        if (File.Exists(persistent))
            return Load(persistent);
        string streaming = Path.Combine(Application.streamingAssetsPath, "story", fileName);
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
            Debug.LogWarning("ストーリー画像を読めませんでした。 " + path + " " + exception.Message);
        }
        return null;
    }
}
