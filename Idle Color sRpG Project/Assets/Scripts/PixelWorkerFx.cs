using UnityEngine;
using UnityEngine.UI;

// ピクセル生産の担当。オーラは表示中の絵の不透明部分を外側へふくらませた形で、シルエットに沿う。
// ドットキャラは進行中に、呼吸のようにゆっくり少し拡縮する。オーラも伸び縮みする。人は体を動かさない。
public class PixelWorkerFx : MonoBehaviour
{
    const float BurstDuration = 0.42f;
    const float HopDuration = 0.3f;
    const int SparkCount = 6;
    const float PortraitLift = 18f;

    static Sprite _disc;

    public bool IsMoving
    {
        get { return _shaking || _bursting || _pulse; }
    }

    Button _button;
    RectTransform _buttonRt;
    RectTransform _aura;
    Image _silhouette;
    Image[] _sparks;
    RectTransform[] _sparkRts;
    Sprite _outlineSprite;
    int _outlineKey;
    float _outlineScaleX = 1f;
    float _outlineScaleY = 1f;
    Vector2 _rest;
    Vector3 _restScale;
    bool _restReady;
    bool _shaking;
    bool _pulse;
    bool _bursting;
    bool _moveBody;
    float _burstTime;
    Color _auraColor;
    Color _burstColor;
    Color _afterBurst;

    public static PixelWorkerFx Attach(Button button)
    {
        if (button == null)
            return null;
        PixelWorkerFx fx = button.GetComponent<PixelWorkerFx>();
        if (fx == null)
            fx = button.gameObject.AddComponent<PixelWorkerFx>();
        return fx;
    }

    public void Hide()
    {
        _shaking = false;
        _pulse = false;
        _bursting = false;
        RestoreBody();
        if (_aura != null)
            _aura.gameObject.SetActive(false);
        Transform leftover = transform.Find("PixelWorkerAura");
        if (leftover != null && leftover != _aura)
            leftover.gameObject.SetActive(false);
        enabled = false;
    }

    // 行を複製したとき、元の担当と光の実体を壊さないよう参照だけ外す。
    public void DiscardSharedCopy()
    {
        _outlineSprite = null;
        _outlineKey = 0;
        _aura = null;
        _silhouette = null;
        _sparks = null;
        _sparkRts = null;
        _button = null;
        _buttonRt = null;
        _restReady = false;
        _shaking = false;
        _pulse = false;
        _bursting = false;
        enabled = false;
    }

    public void Show(Color aura, bool active, bool moveBody)
    {
        _auraColor = aura;
        _afterBurst = aura;
        _moveBody = moveBody;
        bool changed = FigureMismatch();
        if (_bursting)
        {
            if (changed)
                FitAura();
            return;
        }
        // 進行中でも、担当が替わったら古いシルエットを捨てて今の絵で作り直す。
        if ((_shaking || _pulse) && !active && !changed)
            return;
        ApplyAura(aura, active || changed && (_shaking || _pulse));
    }

    public void Burst(Color burstColor, Color auraAfter, bool moveBody)
    {
        _moveBody = moveBody;
        EnsureVisuals();
        FitAura();
        _burstColor = burstColor;
        _burstColor.a = 1f;
        _afterBurst = auraAfter;
        _bursting = true;
        _shaking = false;
        _pulse = false;
        _burstTime = 0f;
        RestoreBody();
        if (_aura != null)
        {
            _aura.gameObject.SetActive(_silhouette != null && _silhouette.enabled);
            _aura.localScale = Vector3.one;
        }
        PaintAura(_burstColor, 0.95f);
        if (_sparks != null)
        {
            for (int i = 0; i < SparkCount; i++)
                _sparks[i].gameObject.SetActive(true);
        }
        enabled = true;
    }

    void Awake()
    {
        _button = GetComponent<Button>();
        _buttonRt = transform as RectTransform;
        enabled = false;
    }

    void OnDestroy()
    {
        if (_aura != null)
            Destroy(_aura.gameObject);
        ReleaseOutline();
    }

    void Update()
    {
        if (_aura != null && _aura.sizeDelta.x < 1f && !_bursting)
            FitAura();
        if (_bursting)
        {
            _burstTime += Time.deltaTime;
            DrawBurst();
            if (_burstTime >= BurstDuration)
                EndBurst();
            return;
        }
        if (_shaking)
            DrawBreath();
        if (_pulse || _shaking)
            DrawPulse();
    }

    void ApplyAura(Color aura, bool active)
    {
        EnsureVisuals();
        FitAura();
        _shaking = active && _moveBody;
        _pulse = active && !_moveBody;
        _aura.localScale = Vector3.one;
        bool visible = _silhouette != null && _silhouette.enabled && _silhouette.sprite != null;
        _aura.gameObject.SetActive(visible);
        PaintAura(aura, 0.82f);
        for (int i = 0; i < SparkCount; i++)
            _sparks[i].gameObject.SetActive(false);
        if (!_shaking)
            RestoreBody();
        BringAuraAbovePicture();
        enabled = _shaking || _pulse;
    }

    void DrawBreath()
    {
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * 2f);
        float scale = Mathf.Lerp(0.98f, 1.02f, wave);
        _buttonRt.anchoredPosition = _rest;
        _buttonRt.localScale = new Vector3(_restScale.x * scale, _restScale.y * scale, _restScale.z);
    }

    void DrawPulse()
    {
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * 5.5f);
        PaintAura(_auraColor, Mathf.Lerp(0.5f, 0.95f, wave));
    }

    void DrawBurst()
    {
        float u = _burstTime / BurstDuration;
        if (u < 0f)
            u = 0f;
        if (u > 1f)
            u = 1f;
        if (_moveBody)
        {
            float hopT = HopDuration <= 0f ? 1f : _burstTime / HopDuration;
            float hop = HopWave(hopT);
            float height = Mathf.Clamp(_buttonRt.rect.height * 0.16f, 10f, 22f);
            _buttonRt.anchoredPosition = _rest + new Vector2(0f, hop * height);
            if (_aura != null && _aura.parent != transform)
                _aura.anchoredPosition = _buttonRt.anchoredPosition;
            float stretch = 1f + 0.16f * hop;
            float squeeze = 1f - 0.07f * hop;
            _buttonRt.localScale = new Vector3(_restScale.x * squeeze, _restScale.y * stretch, _restScale.z);
        }
        else
            RestoreBody();

        float spread = Mathf.Lerp(1f, 1.9f, u);
        _aura.localScale = new Vector3(spread, spread, 1f);
        float fade = 1f - u;
        PaintAura(_burstColor, 0.95f * fade);

        float reach = Mathf.Max(_aura.sizeDelta.x, 72f) * 0.42f;
        for (int i = 0; i < SparkCount; i++)
        {
            float angle = i * Mathf.PI * 2f / SparkCount;
            float dist = Mathf.Lerp(4f, reach, u);
            _sparkRts[i].anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            Color color = Color.Lerp(_burstColor, Color.white, 0.35f);
            color.a = fade;
            _sparks[i].color = color;
            float size = Mathf.Lerp(0.85f, 1.45f, u);
            _sparkRts[i].sizeDelta = new Vector2(22f, 22f);
            _sparkRts[i].localScale = new Vector3(size, size, 1f);
        }
    }

    void EndBurst()
    {
        _bursting = false;
        ApplyAura(_afterBurst, false);
    }

    void RestoreBody()
    {
        if (_buttonRt == null || !_restReady)
            return;
        _buttonRt.anchoredPosition = _rest;
        _buttonRt.localScale = _restScale;
    }

    void EnsureVisuals()
    {
        if (_button == null)
            _button = GetComponent<Button>();
        if (_buttonRt == null)
            _buttonRt = transform as RectTransform;
        if (!_restReady && _buttonRt != null)
        {
            _rest = _buttonRt.anchoredPosition;
            _restScale = _buttonRt.localScale;
            if (_restScale == Vector3.zero)
                _restScale = Vector3.one;
            _restReady = true;
        }
        if (_aura != null && _silhouette != null && _aura.parent == transform)
            return;
        if (_aura != null)
        {
            Destroy(_aura.gameObject);
            _aura = null;
            _silhouette = null;
        }
        if (transform.parent != null)
        {
            Transform stale = transform.parent.Find("PixelWorkerAura");
            if (stale != null)
                Destroy(stale.gameObject);
        }

        GameObject rootObject = new GameObject("PixelWorkerAura", typeof(RectTransform));
        rootObject.transform.SetParent(transform, false);
        _aura = rootObject.GetComponent<RectTransform>();
        _aura.anchorMin = new Vector2(0.5f, 0.5f);
        _aura.anchorMax = new Vector2(0.5f, 0.5f);
        _aura.pivot = new Vector2(0.5f, 0.5f);
        _aura.localScale = Vector3.one;
        _aura.SetAsFirstSibling();

        GameObject imageObject = new GameObject("Silhouette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(_aura, false);
        _silhouette = imageObject.GetComponent<Image>();
        _silhouette.raycastTarget = false;
        _silhouette.preserveAspect = false;
        _silhouette.type = Image.Type.Simple;
        RectTransform imageRect = _silhouette.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        _sparks = new Image[SparkCount];
        _sparkRts = new RectTransform[SparkCount];
        float spark = 22f;
        for (int i = 0; i < SparkCount; i++)
        {
            _sparks[i] = CreateDisc("Spark" + i.ToString(), spark);
            _sparkRts[i] = _sparks[i].rectTransform;
            _sparks[i].gameObject.SetActive(false);
        }
    }

    Image CreateDisc(string name, float size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(_aura, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = Disc();
        image.raycastTarget = false;
        image.color = Color.white;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;
        return image;
    }

    void FitAura()
    {
        if (_aura == null || _buttonRt == null)
            return;
        bool portrait;
        Sprite sprite = FigureSprite(out portrait);
        EnsureOutline(sprite, !portrait);
        if (_silhouette == null || _silhouette.sprite == null)
            return;
        float width = _buttonRt.rect.width;
        float height = _buttonRt.rect.height;
        if (portrait)
        {
            if (_aura.parent != transform)
                _aura.SetParent(transform, false);
            height = width;
            Rect portraitRect = _buttonRt.rect;
            Transform figure = transform.Find("PersonPortrait");
            float lift = PortraitLift;
            RectTransform figureRect = figure as RectTransform;
            if (figureRect != null)
                lift = figureRect.anchoredPosition.y;
            float bottom = portraitRect.yMin + lift;
            Vector2 center = new Vector2(0f, bottom + height * 0.5f - portraitRect.center.y);
            if (width < 1f || height < 1f)
                return;
            _aura.anchorMin = new Vector2(0.5f, 0.5f);
            _aura.anchorMax = new Vector2(0.5f, 0.5f);
            _aura.pivot = new Vector2(0.5f, 0.5f);
            _aura.anchoredPosition = center;
            _aura.sizeDelta = new Vector2(width * _outlineScaleX, height * _outlineScaleY);
            BringAuraAbovePicture();
            return;
        }

        if (_button != null && _button.image != null)
        {
            _button.image.type = Image.Type.Simple;
            _button.image.preserveAspect = false;
        }
        if (width < 1f || height < 1f)
            return;
        if (_aura.parent != transform)
            _aura.SetParent(transform, false);
        _aura.anchorMin = new Vector2(0.5f, 0.5f);
        _aura.anchorMax = new Vector2(0.5f, 0.5f);
        _aura.pivot = new Vector2(0.5f, 0.5f);
        _aura.anchoredPosition = Vector2.zero;
        _aura.sizeDelta = new Vector2(width * _outlineScaleX, height * _outlineScaleY);
        BringAuraAbovePicture();
    }

    // 肖像や属性の枠より手前。真ん中は抜けているので、見える光はRGBだけになる。
    void BringAuraAbovePicture()
    {
        if (_aura == null)
            return;
        if (_aura.parent != transform)
            _aura.SetParent(transform, false);
        _aura.SetAsLastSibling();
        Transform bar = transform.Find("PersonSkillBar");
        if (bar != null)
            bar.SetAsLastSibling();
    }

    Sprite FigureSprite(out bool portrait)
    {
        portrait = false;
        Transform figure = transform.Find("PersonPortrait");
        if (figure != null && figure.gameObject.activeInHierarchy)
        {
            Image image = figure.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                portrait = true;
                return image.sprite;
            }
        }
        if (_button != null && _button.image != null)
            return _button.image.sprite;
        return null;
    }

    bool FigureMismatch()
    {
        bool portrait;
        Sprite sprite = FigureSprite(out portrait);
        int key = OutlineKey(sprite, !portrait);
        return key == 0 || key != _outlineKey || _outlineSprite == null;
    }

    static int OutlineKey(Sprite sprite, bool character)
    {
        if (sprite == null || sprite.texture == null)
            return 0;
        return sprite.texture.GetInstanceID() ^ (character ? 0xE55 : 0x2C3);
    }

    void EnsureOutline(Sprite sprite, bool dropEdgeBlack)
    {
        if (_silhouette == null)
            return;
        if (sprite == null || sprite.texture == null || !sprite.texture.isReadable)
        {
            _silhouette.sprite = null;
            _silhouette.enabled = false;
            return;
        }
        int key = OutlineKey(sprite, dropEdgeBlack);
        if (key == _outlineKey && _outlineSprite != null)
        {
            _silhouette.sprite = _outlineSprite;
            _silhouette.enabled = true;
            return;
        }
        float scaleX;
        float scaleY;
        Sprite built = BuildOutline(sprite, dropEdgeBlack, out scaleX, out scaleY);
        if (built == null)
        {
            _silhouette.sprite = null;
            _silhouette.enabled = false;
            return;
        }
        ReleaseOutline();
        _outlineSprite = built;
        _outlineKey = key;
        _outlineScaleX = scaleX;
        _outlineScaleY = scaleY;
        _silhouette.sprite = built;
        _silhouette.enabled = true;
    }

    void ReleaseOutline()
    {
        if (_outlineSprite == null)
            return;
        if (_outlineSprite.texture != null)
            Destroy(_outlineSprite.texture);
        Destroy(_outlineSprite);
        _outlineSprite = null;
        _outlineKey = 0;
    }

    static bool[] InkMask(Color32[] pixels, int texW, int rx, int ry, int rw, int rh)
    {
        int count = rw * rh;
        bool[] ink = new bool[count];
        bool any = false;
        for (int y = 0; y < rh; y++)
        {
            int src = (ry + y) * texW + rx;
            int dst = y * rw;
            for (int x = 0; x < rw; x++)
            {
                // 黒もキャラの一部。抜くのは透明だけ。
                if (pixels[src + x].a > 16)
                {
                    ink[dst + x] = true;
                    any = true;
                }
            }
        }
        return any ? ink : null;
    }

    // リスト表示用。真ん中は抜けた白い光で、色は呼び出し側が乗せる。
    public static Sprite CreateOutline(Sprite sprite, out float scaleX, out float scaleY)
    {
        return BuildOutline(sprite, false, out scaleX, out scaleY);
    }

    static Sprite BuildOutline(Sprite sprite, bool dropEdgeBlack, out float scaleX, out float scaleY)
    {
        scaleX = 1f;
        scaleY = 1f;
        Texture2D source = sprite.texture;
        if (source == null || !source.isReadable)
            return null;
        // textureRect は透過を切り詰めた範囲。大きい絵は下に余白があることが多く、
        // そこだけを枠いっぱいに伸ばすとオーラが上へずれる。作ったときの範囲を使う。
        Rect texRect = sprite.rect;
        int rx = Mathf.RoundToInt(texRect.x);
        int ry = Mathf.RoundToInt(texRect.y);
        int rw = Mathf.RoundToInt(texRect.width);
        int rh = Mathf.RoundToInt(texRect.height);
        if (rw < 1 || rh < 1)
            return null;

        Color32[] pixels;
        try
        {
            pixels = source.GetPixels32();
        }
        catch
        {
            return null;
        }
        int texW = source.width;
        if (pixels == null || rx < 0 || ry < 0 || rx + rw > texW || ry + rh > source.height)
            return null;

        bool[] ink = InkMask(pixels, texW, rx, ry, rw, rh);
        if (ink == null)
            return null;

        // ドット絵は小さいまま光ると形が崩れる。人の肖像と同じ 256 に広げてから落とす。
        if (dropEdgeBlack)
        {
            const int silhouetteSize = 256;
            bool[] fitted = new bool[silhouetteSize * silhouetteSize];
            for (int y = 0; y < silhouetteSize; y++)
            {
                int sy = y * rh / silhouetteSize;
                if (sy >= rh)
                    sy = rh - 1;
                int srcRow = sy * rw;
                int dstRow = y * silhouetteSize;
                for (int x = 0; x < silhouetteSize; x++)
                {
                    int sx = x * rw / silhouetteSize;
                    if (sx >= rw)
                        sx = rw - 1;
                    fitted[dstRow + x] = ink[srcRow + sx];
                }
            }
            ink = fitted;
            rw = silhouetteSize;
            rh = silhouetteSize;
        }

        int sampleW = rw;
        int sampleH = rh;
        bool[] solid = ink;
        int maxSide = rw > rh ? rw : rh;
        if (maxSide > 80)
        {
            int step = Mathf.CeilToInt(maxSide / 80f);
            sampleW = (rw + step - 1) / step;
            sampleH = (rh + step - 1) / step;
            solid = new bool[sampleW * sampleH];
            for (int sy = 0; sy < sampleH; sy++)
            {
                int y0 = sy * step;
                int y1 = rh;
                int yStop = y0 + step;
                if (yStop < y1)
                    y1 = yStop;
                for (int sx = 0; sx < sampleW; sx++)
                {
                    int x0 = sx * step;
                    int x1 = rw;
                    int xStop = x0 + step;
                    if (xStop < x1)
                        x1 = xStop;
                    bool on = false;
                    for (int y = y0; y < y1 && !on; y++)
                    {
                        int row = y * rw;
                        for (int x = x0; x < x1; x++)
                        {
                            if (ink[row + x])
                            {
                                on = true;
                                break;
                            }
                        }
                    }
                    solid[sy * sampleW + sx] = on;
                }
            }
        }
        else if (maxSide < 64 && maxSide > 0)
        {
            int scale = 64 / maxSide;
            if (scale < 2)
                scale = 2;
            sampleW = rw * scale;
            sampleH = rh * scale;
            solid = new bool[sampleW * sampleH];
            for (int y = 0; y < sampleH; y++)
            {
                int sy = y / scale;
                int row = sy * rw;
                int dst = y * sampleW;
                for (int x = 0; x < sampleW; x++)
                    solid[dst + x] = ink[row + x / scale];
            }
        }

        // キャラの光は、細すぎず広すぎない幅にする。
        float reach = dropEdgeBlack ? 0.18f : 0.1f;
        int radiusMax = dropEdgeBlack ? 16 : 12;
        int radius = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(sampleW, sampleH) * reach), 4, radiusMax);
        int ow = sampleW + radius * 2;
        int oh = sampleH + radius * 2;
        Color32[] outline = new Color32[ow * oh];
        for (int y = 0; y < oh; y++)
        {
            int sy = y - radius;
            for (int x = 0; x < ow; x++)
            {
                int sx = x - radius;
                if (sx >= 0 && sy >= 0 && sx < sampleW && sy < sampleH && solid[sy * sampleW + sx])
                    continue;
                float best = radius + 1f;
                int x0 = sx - radius;
                if (x0 < 0)
                    x0 = 0;
                int x1 = sx + radius;
                if (x1 > sampleW - 1)
                    x1 = sampleW - 1;
                int y0 = sy - radius;
                if (y0 < 0)
                    y0 = 0;
                int y1 = sy + radius;
                if (y1 > sampleH - 1)
                    y1 = sampleH - 1;
                for (int yy = y0; yy <= y1; yy++)
                {
                    int row = yy * sampleW;
                    int dy = yy - sy;
                    if (dy < 0)
                        dy = -dy;
                    for (int xx = x0; xx <= x1; xx++)
                    {
                        if (!solid[row + xx])
                            continue;
                        int dx = xx - sx;
                        if (dx < 0)
                            dx = -dx;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist < best)
                            best = dist;
                    }
                }
                if (best > radius || best <= 0f)
                    continue;
                float t = (best - 0.65f) / radius;
                if (t < 0f)
                    t = 0f;
                if (t > 1f)
                    t = 1f;
                float fade = 1f - t;
                fade = fade * fade;
                byte alpha = (byte)Mathf.RoundToInt(fade * 255f);
                if (alpha == 0)
                    continue;
                outline[y * ow + x] = new Color32(255, 255, 255, alpha);
            }
        }

        Texture2D texture = new Texture2D(ow, oh, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels32(outline);
        texture.Apply(false, false);
        Sprite result = Sprite.Create(texture, new Rect(0f, 0f, ow, oh), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        result.hideFlags = HideFlags.HideAndDontSave;
        scaleX = ow / (float)sampleW;
        scaleY = oh / (float)sampleH;
        return result;
    }

    void PaintAura(Color color, float alpha)
    {
        if (_silhouette == null)
            return;
        Color ink = color;
        ink.a = color.a <= 0f ? 0f : alpha;
        _silhouette.color = ink;
    }

    static float HopWave(float t)
    {
        if (t <= 0f || t >= 1f)
            return 0f;
        if (t < 0.72f)
            return Mathf.Sin(t / 0.72f * Mathf.PI);
        return Mathf.Sin((t - 0.72f) / 0.28f * Mathf.PI) * 0.2f;
    }

    static Sprite Disc()
    {
        if (_disc != null)
            return _disc;
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float fade = 1f - Mathf.Clamp01(dist);
                fade = fade * fade;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, fade));
            }
        }
        texture.Apply();
        _disc = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        _disc.hideFlags = HideFlags.HideAndDontSave;
        return _disc;
    }
}
