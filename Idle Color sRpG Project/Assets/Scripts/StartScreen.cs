using UnityEngine;
using UnityEngine.UI;

// 起動中の画面。読み込み中は Loading の点が増え、終わると START になり、押されるまで残る。
public class StartScreen : MonoBehaviour
{
    const int DotMax = 6;
    const float DotStepSeconds = 0.3f;

    GameObject _root;
    RectTransform _artRect;
    Sprite _art;
    Text _label;
    StartLabelGradient _gradient;
    int _dots;
    float _dotTime;
    bool _artFitted;
    bool _waitingClick;
    bool _pressed;

    public bool Pressed
    {
        get { return _pressed; }
    }

    public static StartScreen Open(Sprite background)
    {
        GameObject root = new GameObject(
            "StartScreen",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        StartScreen screen = root.AddComponent<StartScreen>();
        screen._root = root;
        screen._art = background;
        screen.Build();
        return screen;
    }

    public void ShowStart()
    {
        _waitingClick = true;
        if (_label != null)
            _label.text = "START!";
        if (_gradient != null)
        {
            _gradient.UseGradient = true;
            _label.SetVerticesDirty();
        }
    }

    public void Close()
    {
        if (_root != null)
            Destroy(_root);
        _root = null;
    }

    void OnPress()
    {
        if (!_waitingClick)
            return;
        _pressed = true;
    }

    void Build()
    {
        Canvas canvas = _root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;

        CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        Image backing = CreateImage("Backing", _root.transform);
        Stretch(backing.rectTransform);
        backing.sprite = White();
        backing.color = new Color(0.04f, 0.05f, 0.08f, 1f);
        backing.raycastTarget = true;
        Button button = backing.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OnPress);

        if (_art != null)
        {
            Image art = CreateImage("Background", _root.transform);
            _artRect = art.rectTransform;
            Stretch(_artRect);
            art.sprite = _art;
            art.color = Color.white;
            art.preserveAspect = true;
            art.raycastTarget = false;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject textObject = new GameObject("TextLoading", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(_root.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.12f);
        textRect.anchorMax = new Vector2(0.5f, 0.12f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(1000f, 200f);
        textRect.anchoredPosition = Vector2.zero;

        _label = textObject.GetComponent<Text>();
        _label.font = font;
        _label.fontSize = 88;
        _label.fontStyle = FontStyle.Bold;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.color = Color.white;
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        _label.verticalOverflow = VerticalWrapMode.Overflow;
        _label.raycastTarget = false;
        _label.text = "Loading";

        _gradient = textObject.AddComponent<StartLabelGradient>();
        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        Canvas.ForceUpdateCanvases();
        _artFitted = FitArt();
    }

    void Update()
    {
        if (!_artFitted)
            _artFitted = FitArt();
        if (_waitingClick || _label == null)
            return;

        _dotTime += Time.unscaledDeltaTime;
        if (_dotTime < DotStepSeconds)
            return;
        _dotTime = 0f;
        _dots++;
        if (_dots > DotMax)
            _dots = 0;
        _label.text = "Loading" + new string('.', _dots);
    }

    // 縦でも横でも、画面いっぱいに広げて、はみ出した分は見切る。
    bool FitArt()
    {
        if (_artRect == null || _art == null)
            return true;
        RectTransform view = _root.GetComponent<RectTransform>();
        float viewW = view.rect.width;
        float viewH = view.rect.height;
        if (viewW < 8f || viewH < 8f)
            return false;

        float spriteW = _art.rect.width;
        float spriteH = _art.rect.height;
        if (spriteW < 1f)
            spriteW = 1f;
        if (spriteH < 1f)
            spriteH = 1f;
        float spriteAspect = spriteW / spriteH;
        float viewAspect = viewW / viewH;
        float width;
        float height;
        if (spriteAspect > viewAspect)
        {
            height = viewH;
            width = height * spriteAspect;
        }
        else
        {
            width = viewW;
            height = width / spriteAspect;
        }

        _artRect.anchorMin = new Vector2(0.5f, 0.5f);
        _artRect.anchorMax = new Vector2(0.5f, 0.5f);
        _artRect.pivot = new Vector2(0.5f, 0.5f);
        _artRect.anchoredPosition = Vector2.zero;
        _artRect.sizeDelta = new Vector2(width, height);
        return true;
    }

    static Image CreateImage(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<Image>();
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static Sprite _white;

    static Sprite White()
    {
        if (_white != null)
            return _white;
        Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
        pixel.hideFlags = HideFlags.HideAndDontSave;
        _white = Sprite.Create(pixel, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        _white.hideFlags = HideFlags.HideAndDontSave;
        return _white;
    }
}

// START の文字。左上のパステル赤から、緑、青、灰色へ。
public class StartLabelGradient : BaseMeshEffect
{
    static readonly Color PastelRed = new Color(1f, 0.64f, 0.66f, 1f);
    static readonly Color PastelGreen = new Color(0.66f, 0.9f, 0.7f, 1f);
    static readonly Color PastelBlue = new Color(0.66f, 0.76f, 1f, 1f);
    static readonly Color PastelGray = new Color(0.78f, 0.78f, 0.82f, 1f);

    public bool UseGradient;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!UseGradient || vh.currentVertCount == 0)
            return;

        UIVertex vert = new UIVertex();
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vert, i);
            Vector3 position = vert.position;
            if (position.x < minX)
                minX = position.x;
            if (position.y < minY)
                minY = position.y;
            if (position.x > maxX)
                maxX = position.x;
            if (position.y > maxY)
                maxY = position.y;
        }

        float width = maxX - minX;
        float height = maxY - minY;
        if (width < 1f)
            width = 1f;
        if (height < 1f)
            height = 1f;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vert, i);
            float across = (vert.position.x - minX) / width;
            float down = (maxY - vert.position.y) / height;
            vert.color = ColorAt((across + down) * 0.5f);
            vh.SetUIVertex(vert, i);
        }
    }

    static Color ColorAt(float t)
    {
        if (t < 0f)
            t = 0f;
        if (t > 1f)
            t = 1f;
        if (t < 1f / 3f)
            return Color.Lerp(PastelRed, PastelGreen, t / (1f / 3f));
        if (t < 2f / 3f)
            return Color.Lerp(PastelGreen, PastelBlue, (t - 1f / 3f) / (1f / 3f));
        return Color.Lerp(PastelBlue, PastelGray, (t - 2f / 3f) / (1f / 3f));
    }
}
