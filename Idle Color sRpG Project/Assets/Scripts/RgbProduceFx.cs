using UnityEngine;
using UnityEngine.UI;

// RGB生産の1回分。キャラは短く跳ね、色の光が波紋になって、右上に生産量が浮かんで消える。
public class RgbProduceFx : MonoBehaviour
{
    const float HopDuration = 0.32f;
    const float RippleDuration = 0.4f;
    const float FloatDuration = 0.58f;
    const int SparkCount = 7;

    static Sprite _glow;
    static Sprite _line;
    static Sprite _disc;
    static Sprite _spark;
    static Font _font;

    Button _button;
    RectTransform _root;
    RectTransform _hop;
    Image _hopImage;
    Image _flash;
    RectTransform _flashRt;
    RingHalf _backA;
    RingHalf _frontA;
    RingHalf _backB;
    RingHalf _frontB;
    Transform _nameLabel;
    int _nameSibling;
    Image[] _sparks;
    Image[] _sparkCores;
    RectTransform[] _sparkRts;
    Text _label;
    Outline _outline;
    RectTransform _labelRt;

    bool _playing;
    bool _hopping;
    bool _showRing;
    bool _hidden;
    float _elapsed;
    float _fxStart;
    float _duration;
    float _size;
    float _footY;
    float _sprayWidth;
    float _sprayHeight;
    Color _color;
    Color _labelColor;
    Color _shown;
    Image _source;

    public static Color ChannelColor(int channel)
    {
        if (channel == 1)
            return new Color(0.05f, 0.78f, 0.08f, 1f);
        if (channel == 2)
            return new Color(0.08f, 0.32f, 1f, 1f);
        return new Color(0.95f, 0.08f, 0.05f, 1f);
    }

    public static void Play(Button button, bool hop, Color color, ulong amount)
    {
        if (button == null || amount == 0 || !button.gameObject.activeInHierarchy)
            return;
        RgbProduceFx fx = button.GetComponent<RgbProduceFx>();
        if (fx == null)
            fx = button.gameObject.AddComponent<RgbProduceFx>();
        fx.Begin(hop, color, amount);
    }

    void Awake()
    {
        _button = GetComponent<Button>();
        enabled = false;
    }

    void Begin(bool hop, Color color, ulong amount)
    {
        if (_button == null)
            _button = GetComponent<Button>();
        if (_button == null)
            return;

        enabled = true;
        RestoreName();
        RestoreIfHidden();
        EnsureBuilt();
        _color = color;
        _elapsed = 0f;
        _playing = true;
        _labelColor = Color.Lerp(color, Color.white, 0.22f);
        _labelColor.a = 1f;
        if (_label != null)
        {
            _label.text = "+" + UiNumber.Format(amount);
            _label.color = _labelColor;
        }
        if (_outline != null)
            _outline.effectColor = new Color(0f, 0f, 0f, 0.9f);

        Rect rect = _button.GetComponent<RectTransform>().rect;
        _size = Mathf.Min(rect.width, rect.height);
        if (_size < 8f)
            _size = 72f;
        _flashRt.sizeDelta = new Vector2(_size * 0.72f, _size * 0.72f);
        PlaceRing(_backA);
        PlaceRing(_frontA);
        PlaceRing(_backB);
        PlaceRing(_frontB);
        float sparkSize = Mathf.Max(10f, _size * 0.2f);
        for (int i = 0; i < SparkCount; i++)
            _sparkRts[i].sizeDelta = new Vector2(sparkSize, sparkSize);
        if (_label != null)
            _label.fontSize = Mathf.Clamp(Mathf.RoundToInt(_size * 0.38f), 24, 38);

        _showRing = hop;
        if (hop)
            ShowFigure();
        else if (_hop != null)
            _hop.gameObject.SetActive(false);
        _hopping = hop && _hidden;
        if (!_hopping && _hop != null)
        {
            _hop.anchoredPosition = Vector2.zero;
            _hop.localScale = Vector3.one;
        }
        if (!_showRing)
            HideRing();

        _footY = -rect.height * 0.4f;
        _sprayWidth = _size * 0.72f;
        _sprayHeight = rect.height * 1.45f;
        _fxStart = HopDuration * 0.7f;
        _duration = Mathf.Max(FloatDuration, _fxStart + 0.52f);

        _root.SetAsLastSibling();
        _root.gameObject.SetActive(true);
        LiftName();
        Apply();
    }

    void Update()
    {
        if (!_playing)
            return;
        _elapsed += Time.deltaTime;
        Apply();
        if (_elapsed >= _duration)
            Finish();
    }

    void Apply()
    {
        DrawHop();
        if (_showRing)
        {
            DrawRipple(_backA, _frontA, _fxStart, RippleDuration, 0.16f, 1.22f, 0.8f);
            DrawRipple(_backB, _frontB, _fxStart + 0.08f, RippleDuration - 0.06f, 0.1f, 0.92f, 0.8f);
            DrawFlash();
        }
        else
            HideRing();
        DrawSparks();
        DrawLabel();
    }

    void DrawHop()
    {
        if (!_hopping || _hop == null)
            return;
        if (_elapsed >= HopDuration)
        {
            _hop.anchoredPosition = Vector2.zero;
            _hop.localScale = Vector3.one;
            _hopping = false;
            return;
        }
        float wave = HopWave(_elapsed / HopDuration);
        float height = Mathf.Clamp(_size * 0.24f, 12f, 30f);
        _hop.anchoredPosition = new Vector2(0f, wave * height);
        float stretch = 1f + 0.14f * wave;
        float squeeze = 1f - 0.05f * wave;
        _hop.localScale = new Vector3(squeeze, stretch, 1f);
    }

    static float HopWave(float t)
    {
        if (t <= 0f || t >= 1f)
            return 0f;
        if (t < 0.7f)
            return Mathf.Sin(t / 0.7f * Mathf.PI);
        return Mathf.Sin((t - 0.7f) / 0.3f * Mathf.PI) * 0.22f;
    }

    void DrawRipple(RingHalf back, RingHalf front, float delay, float life, float from, float to, float alpha)
    {
        float t = _elapsed - delay;
        if (t < 0f || t > life)
        {
            SetRingVisible(back, false);
            SetRingVisible(front, false);
            return;
        }
        float u = life <= 0f ? 1f : t / life;
        float ease = 1f - (1f - u) * (1f - u);
        float scale = Mathf.Lerp(from, to, ease);
        Vector3 size = new Vector3(scale, scale * 0.36f, 1f);
        if (back != null && back.Root != null)
            back.Root.localScale = size;
        if (front != null && front.Root != null)
            front.Root.localScale = size;
        float a = alpha * (1f - u * u);
        Color c = _color;
        c.a = a;
        PaintRing(back, c, a);
        PaintRing(front, c, a);
    }

    static void SetRingVisible(RingHalf ring, bool visible)
    {
        if (ring == null)
            return;
        if (ring.Glow != null)
            ring.Glow.enabled = visible;
        if (ring.Line != null)
            ring.Line.enabled = visible;
    }

    static void PaintRing(RingHalf ring, Color glow, float lineAlpha)
    {
        if (ring == null)
            return;
        if (ring.Glow != null)
        {
            ring.Glow.enabled = true;
            ring.Glow.color = glow;
        }
        if (ring.Line != null)
        {
            ring.Line.enabled = true;
            ring.Line.color = new Color(1f, 1f, 1f, lineAlpha);
        }
    }

    void DrawFlash()
    {
        const float life = 0.16f;
        float t = _elapsed - _fxStart;
        if (_flash == null || t < 0f || t > life)
        {
            if (_flash != null)
                _flash.enabled = false;
            return;
        }
        _flash.enabled = true;
        float u = t / life;
        float scale = Mathf.Lerp(0.08f, 0.7f, 1f - (1f - u) * (1f - u));
        _flashRt.localScale = new Vector3(scale, scale, 1f);
        Color c = Color.Lerp(_color, Color.white, 0.28f);
        c.a = 1f - u * u;
        _flash.color = c;
    }

    void HideRing()
    {
        SetRingVisible(_backA, false);
        SetRingVisible(_frontA, false);
        SetRingVisible(_backB, false);
        SetRingVisible(_frontB, false);
        if (_flash != null)
            _flash.enabled = false;
    }

    void DrawSparks()
    {
        if (_sparks == null)
            return;
        for (int i = 0; i < SparkCount; i++)
        {
            float side = SparkCount <= 1 ? 0f : (i / (SparkCount - 1f)) * 2f - 1f;
            bool fast = (i % 2) == 0;
            float start = _fxStart + Mathf.Abs(side) * 0.02f;
            float life = fast ? 0.32f : 0.5f;
            float t = _elapsed - start;
            Image image = _sparks[i];
            if (t < 0f || t > life)
            {
                image.enabled = false;
                if (_sparkCores != null && _sparkCores[i] != null)
                    _sparkCores[i].enabled = false;
                continue;
            }
            image.enabled = true;
            if (_sparkCores != null && _sparkCores[i] != null)
                _sparkCores[i].enabled = true;
            float u = t / life;
            float open = 1f - (1f - u) * (1f - u);
            float angle = side * 76f * Mathf.Deg2Rad;
            float up = Mathf.Cos(angle);
            float outX = Mathf.Sin(angle);
            float step = fast ? 1f : 0.68f;
            float reach = open * _sprayHeight * 0.34f * step;
            _sparkRts[i].anchoredPosition = new Vector2(outX * open * _sprayWidth, _footY + up * reach);
            float fadeIn = Mathf.Clamp01(u / 0.1f);
            float fadeOut = 1f - Mathf.Clamp01((u - 0.62f) / 0.38f);
            float pop = fadeIn * fadeOut;
            float scale = Mathf.Lerp(0.35f, 1.05f, open) * Mathf.Lerp(1f, 0.7f, 1f - fadeOut);
            _sparkRts[i].localScale = new Vector3(scale, scale, 1f);
            _sparkRts[i].localEulerAngles = new Vector3(0f, 0f, -side * 76f * open);
            Color c = Color.Lerp(_color, Color.white, (1f - u) * 0.2f);
            c.a = pop;
            image.color = c;
            if (_sparkCores != null && _sparkCores[i] != null)
                _sparkCores[i].color = new Color(1f, 1f, 1f, pop);
        }
    }

    void DrawLabel()
    {
        if (_label == null || _labelRt == null)
            return;
        float t = FloatDuration <= 0f ? 1f : Mathf.Clamp01(_elapsed / FloatDuration);
        float rise = Mathf.Clamp(_size * 0.48f, 26f, 46f);
        float eased = 1f - (1f - t) * (1f - t);
        _labelRt.anchoredPosition = new Vector2(0f, -4f + rise * eased);
        float fade = t < 0.22f ? 1f : 1f - (t - 0.22f) / 0.78f;
        if (fade < 0f)
            fade = 0f;
        Color c = _labelColor;
        c.a = fade;
        _label.color = c;
        if (_outline != null)
            _outline.effectColor = new Color(0f, 0f, 0f, 0.88f * fade);
    }

    void Finish()
    {
        _playing = false;
        _hopping = false;
        RestoreName();
        RestoreIfHidden();
        if (_hop != null)
            _hop.gameObject.SetActive(false);
        if (_root != null)
            _root.gameObject.SetActive(false);
        enabled = false;
    }

    void ShowFigure()
    {
        _source = _button != null ? _button.image : null;
        if (_source == null || _hopImage == null)
        {
            if (_hop != null)
                _hop.gameObject.SetActive(false);
            return;
        }
        _shown = _source.color;
        if (_shown.a < 0.99f)
            _shown.a = 1f;
        _hopImage.sprite = _source.sprite;
        _hopImage.type = _source.type;
        _hopImage.preserveAspect = _source.preserveAspect;
        _hopImage.color = _shown;
        _hop.anchoredPosition = Vector2.zero;
        _hop.localScale = Vector3.one;
        _hop.gameObject.SetActive(true);
        _source.color = new Color(_shown.r, _shown.g, _shown.b, 0f);
        _hidden = true;
    }

    void RestoreIfHidden()
    {
        if (!_hidden || _source == null)
            return;
        _hidden = false;
        if (_source.color.a > 0.01f)
            return;
        _source.color = _shown;
    }

    void EnsureBuilt()
    {
        if (_root != null)
            return;
        Transform stale = transform.Find("RgbProduceFxRoot");
        if (stale != null)
            Destroy(stale.gameObject);

        GameObject rootObject = new GameObject("RgbProduceFxRoot", typeof(RectTransform));
        rootObject.transform.SetParent(transform, false);
        _root = rootObject.GetComponent<RectTransform>();
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;

        _backA = CreateHalfRing("RingABack", true);
        _backB = CreateHalfRing("RingBBack", true);

        _hopImage = CreateImage("Hop", _root, null);
        _hop = _hopImage.rectTransform;
        _hop.anchorMin = Vector2.zero;
        _hop.anchorMax = Vector2.one;
        _hop.offsetMin = Vector2.zero;
        _hop.offsetMax = Vector2.zero;
        _hop.gameObject.SetActive(false);

        _frontA = CreateHalfRing("RingAFront", false);
        _frontB = CreateHalfRing("RingBFront", false);
        _flash = CreateImage("Flash", _root, DiscSprite());
        _flashRt = _flash.rectTransform;
        AnchorCenter(_flashRt);

        _sparks = new Image[SparkCount];
        _sparkCores = new Image[SparkCount];
        _sparkRts = new RectTransform[SparkCount];
        Sprite spark = SparkSprite();
        Sprite core = DiscSprite();
        for (int i = 0; i < SparkCount; i++)
        {
            GameObject holder = new GameObject("Spark" + i.ToString(), typeof(RectTransform));
            holder.transform.SetParent(_root, false);
            _sparkRts[i] = holder.GetComponent<RectTransform>();
            AnchorCenter(_sparkRts[i]);
            _sparks[i] = CreateImage("Color", holder.transform, spark);
            FillRect(_sparks[i].rectTransform);
            _sparkCores[i] = CreateImage("Core", holder.transform, core);
            CenterPortion(_sparkCores[i].rectTransform, 0.42f);
        }
        CreateLabel();
    }

    void CreateLabel()
    {
        Font font = LabelFont();
        GameObject obj = new GameObject("Amount", typeof(RectTransform));
        obj.transform.SetParent(_root, false);
        _labelRt = obj.GetComponent<RectTransform>();
        _labelRt.anchorMin = new Vector2(1f, 1f);
        _labelRt.anchorMax = new Vector2(1f, 1f);
        _labelRt.pivot = new Vector2(1f, 0f);
        _labelRt.sizeDelta = new Vector2(260f, 52f);
        _labelRt.anchoredPosition = Vector2.zero;
        if (font == null)
        {
            obj.SetActive(false);
            _label = null;
            return;
        }
        _label = obj.AddComponent<Text>();
        _label.font = font;
        _label.fontSize = 28;
        _label.alignment = TextAnchor.LowerRight;
        _label.color = Color.white;
        _label.raycastTarget = false;
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        _label.verticalOverflow = VerticalWrapMode.Overflow;
        _label.supportRichText = false;
        _outline = obj.AddComponent<Outline>();
        _outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        _outline.effectDistance = new Vector2(1f, -1f);
    }

    void PlaceRing(RingHalf ring)
    {
        if (ring == null)
            return;
        ring.Root.sizeDelta = new Vector2(_size, _size);
        PlaceRingAtFeet(ring.Root);
    }

    void LiftName()
    {
        _nameLabel = null;
        if (_button == null || _hop == null)
            return;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == _root || child.GetComponent<Text>() == null)
                continue;
            _nameLabel = child;
            _nameSibling = i;
            break;
        }
        if (_nameLabel == null)
            return;
        _nameLabel.SetParent(_root, false);
        _nameLabel.SetSiblingIndex(_hop.GetSiblingIndex() + 1);
    }

    void RestoreName()
    {
        if (_nameLabel == null)
            return;
        _nameLabel.SetParent(transform, false);
        int index = _nameSibling;
        if (index < 0)
            index = 0;
        if (index > transform.childCount - 1)
            index = transform.childCount - 1;
        _nameLabel.SetSiblingIndex(index);
        _nameLabel = null;
    }

    void PlaceRingAtFeet(RectTransform rt)
    {
        if (rt == null || _button == null)
            return;
        Rect rect = _button.GetComponent<RectTransform>().rect;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, rect.height * 0.14f);
    }

    static void AnchorCenter(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    RingHalf CreateHalfRing(string name, bool upper)
    {
        RingHalf half = new RingHalf();
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(_root, false);
        half.Root = obj.GetComponent<RectTransform>();
        AnchorCenter(half.Root);

        GameObject maskObject = new GameObject("Mask", typeof(RectTransform));
        maskObject.transform.SetParent(obj.transform, false);
        RectTransform mask = maskObject.GetComponent<RectTransform>();
        mask.anchorMin = upper ? new Vector2(0f, 0.5f) : new Vector2(0f, 0f);
        mask.anchorMax = upper ? new Vector2(1f, 1f) : new Vector2(1f, 0.5f);
        mask.offsetMin = Vector2.zero;
        mask.offsetMax = Vector2.zero;
        mask.pivot = new Vector2(0.5f, 0.5f);
        maskObject.AddComponent<RectMask2D>();

        half.Glow = CreateImage("Glow", mask, GlowSprite());
        half.Line = CreateImage("Line", mask, LineSprite());
        PlaceFullRing(half.Glow.rectTransform, upper);
        PlaceFullRing(half.Line.rectTransform, upper);
        return half;
    }

    static void PlaceFullRing(RectTransform rt, bool upper)
    {
        if (upper)
        {
            rt.anchorMin = new Vector2(0f, -1f);
            rt.anchorMax = new Vector2(1f, 1f);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 2f);
        }
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
    }

    static void FillRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static void CenterPortion(RectTransform rt, float portion)
    {
        float edge = (1f - portion) * 0.5f;
        rt.anchorMin = new Vector2(edge, edge);
        rt.anchorMax = new Vector2(1f - edge, 1f - edge);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }

    static Font LabelFont()
    {
        if (_font != null)
            return _font;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return _font;
    }

    static Sprite GlowSprite()
    {
        if (_glow != null)
            return _glow;
        _glow = RingSprite(0.11f, 0.85f);
        return _glow;
    }

    static Sprite LineSprite()
    {
        if (_line != null)
            return _line;
        _line = RingSprite(0.02f, 1f);
        return _line;
    }

    // 同じ半径の輪。spread が広いほどぼける。
    static Sprite RingSprite(float spread, float peak)
    {
        const int n = 160;
        const float radius = 0.72f;
        Texture2D tex = NewTex(n);
        float c = (n - 1) * 0.5f;
        float denom = 2f * spread * spread;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float delta = d - radius;
                float a = Mathf.Exp(-(delta * delta) / denom) * peak;
                if (a < 0.004f)
                    a = 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        return FinishSprite(tex, n);
    }

    static Sprite DiscSprite()
    {
        if (_disc != null)
            return _disc;
        const int n = 64;
        Texture2D tex = NewTex(n);
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        _disc = FinishSprite(tex, n);
        return _disc;
    }

    static Sprite SparkSprite()
    {
        if (_spark != null)
            return _spark;
        const int n = 32;
        Texture2D tex = NewTex(n);
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs(x - c) / c;
                float dy = Mathf.Abs(y - c) / c;
                float diamond = Mathf.Clamp01(1f - (dx + dy) / 0.95f);
                float horiz = Mathf.Clamp01(1f - dy / 0.14f) * Mathf.Clamp01(1f - dx);
                float vert = Mathf.Clamp01(1f - dx / 0.14f) * Mathf.Clamp01(1f - dy);
                float a = Mathf.Max(diamond, Mathf.Max(horiz, vert));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        _spark = FinishSprite(tex, n);
        return _spark;
    }

    static Texture2D NewTex(int n)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    static Sprite FinishSprite(Texture2D tex, int n)
    {
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    class RingHalf
    {
        public RectTransform Root;
        public Image Glow;
        public Image Line;
    }
}
