using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 顔とスタンプ。message/icon と message/stamp を見て、顔が無ければ human も見る。
public static class MessageImages
{
    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    static Sprite _circle;
    static Sprite _round;

    public static Sprite Icon(string fileName)
    {
        return Load(fileName, true);
    }

    public static Sprite Portrait(string fileName, Color background)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;
        int r = Mathf.RoundToInt(background.r * 255f);
        int g = Mathf.RoundToInt(background.g * 255f);
        int b = Mathf.RoundToInt(background.b * 255f);
        string key = "p:" + fileName + ":" + r.ToString() + "," + g.ToString() + "," + b.ToString();
        Sprite cached;
        if (Cache.TryGetValue(key, out cached) && cached != null)
            return cached;
        Texture2D source = Read(fileName, true);
        if (source == null)
            return null;
        Texture2D cut = CutCircle(source, background);
        UnityEngine.Object.Destroy(source);
        if (cut == null)
            return null;
        Sprite sprite = Sprite.Create(cut, new Rect(0f, 0f, cut.width, cut.height), new Vector2(0.5f, 0.5f));
        Cache[key] = sprite;
        return sprite;
    }

    public static Sprite Stamp(string fileName)
    {
        return Load(fileName, false);
    }

    public static Sprite Circle()
    {
        if (_circle != null)
            return _circle;
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        float radius = (size - 1) * 0.5f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius;
                float dy = y - radius;
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.75f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        _circle = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return _circle;
    }

    public static Sprite Round()
    {
        if (_round != null)
            return _round;
        const int size = 64;
        const float radius = 22f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, size - radius);
                float cy = Mathf.Clamp(py, radius, size - radius);
                float dx = px - cx;
                float dy = py - cy;
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.75f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        _round = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        return _round;
    }

    static Sprite Load(string fileName, bool icon)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;
        string key = (icon ? "i:" : "s:") + fileName;
        Sprite cached;
        if (Cache.TryGetValue(key, out cached) && cached != null)
            return cached;
        Texture2D texture = Read(fileName, icon);
        if (texture == null)
            return null;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        Cache[key] = sprite;
        return sprite;
    }

    static Texture2D Read(string fileName, bool icon)
    {
        string folder = icon ? "icon" : "stamp";
        string persistent = Path.Combine(Application.persistentDataPath, "message", folder, fileName);
        if (File.Exists(persistent))
            return LoadFile(persistent);
        string streaming = Path.Combine(Application.streamingAssetsPath, "message", folder, fileName);
        if (File.Exists(streaming))
            return LoadFile(streaming);
        if (!icon)
            return null;
        string humanPersistent = Path.Combine(Application.persistentDataPath, "human", fileName);
        if (File.Exists(humanPersistent))
            return LoadFile(humanPersistent);
        string human = Path.Combine(Application.streamingAssetsPath, "human", fileName);
        if (File.Exists(human))
            return LoadFile(human);
        return null;
    }

    static Texture2D CutCircle(Texture2D source, Color background)
    {
        int size = Mathf.Min(source.width, source.height);
        if (size < 2)
            return null;
        Color[] sourcePixels = source.GetPixels();
        int ox = (source.width - size) / 2;
        int oy = (source.height - size) / 2;
        Color[] pixels = new Color[size * size];
        float radius = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color src = sourcePixels[(y + oy) * source.width + (x + ox)];
                float dx = x - radius;
                float dy = y - radius;
                float edge = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.75f);
                float cover = src.a;
                Color filled = new Color(
                    src.r * cover + background.r * (1f - cover),
                    src.g * cover + background.g * (1f - cover),
                    src.b * cover + background.b * (1f - cover),
                    edge);
                pixels[y * size + x] = filled;
            }
        }
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    static Texture2D LoadFile(string path)
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
            Debug.LogWarning("メッセージ画像を読めませんでした。 " + path + " " + exception.Message);
        }
        return null;
    }
}
