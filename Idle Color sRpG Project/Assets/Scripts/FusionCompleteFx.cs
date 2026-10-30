using System;
using UnityEngine;
using UnityEngine.UI;

// 合成完了。代表色の光が集まり、輪郭から染み込み、跳ねて着地で飛び散る。
public class FusionCompleteFx : MonoBehaviour
{
    const float GatherDuration = 0.55f;
    const float WhiteHoldDuration = 0.08f;
    const float HopDuration = 0.38f;
    const float LandDuration = 0.42f;
    const int SparkCount = 10;
    const int GatherRingCount = 5;

    static Sprite _disc;
    static Sprite _solidWhite;
    static readonly float[] GatherRingFrom = { 2.7f, 2.3f, 1.95f, 1.65f, 1.35f };

    Image _portrait;
    RectTransform _host;
    RectTransform _portraitRt;
    Vector2 _hostRest;
    Vector3 _hostScale;
    Color _portraitRest;
    bool _restReady;
    bool _portraitRestReady;
    Image _outline;
    RectTransform _outlineRt;
    // キャラ形マスク＋白塗り。乗算では色つき絵を白くできないので、形だけ白く塗る。
    RectTransform _washRt;
    Image _washMaskImage;
    Mask _washMask;
    Image _washFill;
    Image[] _sparks;
    RectTransform[] _sparkRts;
    Image[] _gatherRings;
    RectTransform[] _gatherRingRts;
    Image[] _gatherHalos;
    RectTransform[] _gatherHaloRts;
    Image[] _gatherCores;
    RectTransform[] _gatherCoreRts;
    Sprite _outlineSprite;
    float _outlineScaleX = 1f;
    float _outlineScaleY = 1f;
    Color _color;
    float _elapsed;
    float _size;
    float _phaseStart;
    int _phase;
    bool _playing;
    bool _landStarted;
    float _landTime;
    Action _onDone;

    public bool IsPlaying
    {
        get { return _playing; }
    }

    public static void Play(Image portrait, Color color, Action onDone)
    {
        if (portrait == null)
        {
            if (onDone != null)
                onDone();
            return;
        }

        Transform host = portrait.transform.parent != null ? portrait.transform.parent : portrait.transform;
        FusionCompleteFx fx = host.GetComponent<FusionCompleteFx>();
        if (fx == null)
            fx = host.gameObject.AddComponent<FusionCompleteFx>();
        fx.Begin(portrait, color, onDone);
    }

    public void Stop()
    {
        if (!_playing)
            return;
        Finish(false);
    }

    void Begin(Image portrait, Color color, Action onDone)
    {
        if (_playing)
            Finish(false);

        _portrait = portrait;
        _portraitRt = portrait.rectTransform;
        _host = transform as RectTransform;
        _onDone = onDone;
        _color = color;
        _color.a = 1f;
        _elapsed = 0f;
        _phase = 0;
        _phaseStart = 0f;
        _landStarted = false;
        _landTime = 0f;
        _playing = true;
        _portraitRest = portrait.color;
        _portraitRestReady = true;

        if (!_restReady && _host != null)
        {
            _hostRest = _host.anchoredPosition;
            _hostScale = _host.localScale;
            if (_hostScale == Vector3.zero)
                _hostScale = Vector3.one;
            _restReady = true;
        }

        EnsureBuilt();
        FitLayers();
        HideSparks();
        HideGatherRings();
        if (_outline != null)
        {
            _outline.enabled = true;
            PaintOutline(0f);
        }
        HideWash();
        enabled = true;
    }

    void Update()
    {
        if (!_playing)
            return;
        _elapsed += Time.deltaTime;
        float local = _elapsed - _phaseStart;

        if (_phase == 0)
        {
            DrawGather(local);
            if (local >= GatherDuration)
                EnterPhase(1);
        }
        else if (_phase == 1)
        {
            // 真っ白で一瞬ためてから跳ねる。
            DrawWhiteHold(local);
            if (local >= WhiteHoldDuration)
                EnterPhase(2);
        }
        else if (_phase == 2)
        {
            float landAt = HopDuration * 0.68f;
            DrawHop(Mathf.Min(local, HopDuration));
            if (local >= landAt)
            {
                _landStarted = true;
                _landTime = local - landAt;
                DrawLand(_landTime);
            }
            if (local >= HopDuration && _landTime >= LandDuration)
                Finish(true);
        }
    }

    void EnterPhase(int phase)
    {
        _phase = phase;
        _phaseStart = _elapsed;
        if (phase == 2)
            RestoreHost();
    }

    void DrawGather(float t)
    {
        float u = GatherDuration <= 0f ? 1f : Mathf.Clamp01(t / GatherDuration);
        // 終盤で一気に中心へ吸い付く。
        float ease = u * u;
        if (u > 0.55f)
        {
            float tail = (u - 0.55f) / 0.45f;
            ease = Mathf.Lerp(0.55f * 0.55f, 1f, 1f - (1f - tail) * (1f - tail));
        }
        RestoreHost();

        // 外の輪郭がキャラ輪郭に重なるほど、白さを足す。
        float overlapWhite = DrawGatherRings(u);
        if (u >= 0.98f)
            overlapWhite = 1f;

        // 光粒もキャラの約1.5倍外側から。
        float outer = Mathf.Max(_size * 1.55f, 100f);
        float inner = 2f;
        for (int i = 0; i < SparkCount; i++)
        {
            float angle = i * Mathf.PI * 2f / SparkCount + 0.2f;
            float dist = Mathf.Lerp(outer, inner, ease);
            float sparkScale = Mathf.Lerp(1.85f, 0.9f, ease);
            float sparkAlpha = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(u / 0.15f));
            if (u > 0.82f)
                sparkAlpha *= 1f - (u - 0.82f) / 0.18f;
            ShowSpark(i, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist, sparkScale, sparkAlpha);
        }

        float body = ease;
        PaintOutline(Mathf.Lerp(0.25f, 1f, body));
        if (_outlineRt != null)
        {
            float outlineSpread = Mathf.Lerp(1.4f, 1.05f, body);
            _outlineRt.localScale = new Vector3(outlineSpread, outlineSpread, 1f);
        }
        // Image.color は乗算なので白にできない。同じ形の白オーバーレイで白く見せる。
        // 輪郭の重なりと時間の大きい方を使い、最後は必ず真っ白へ。
        float white = Mathf.Max(overlapWhite, ease);
        if (u >= 0.97f)
            white = 1f;
        PaintPortraitGlow(white);
    }

    // 戻り値: 輪郭がキャラに重なった度合い（0〜1）。これがキャラの白さ。
    float DrawGatherRings(float u)
    {
        if (_gatherRings == null)
            return u;
        float overlapSum = 0f;
        int count = 0;
        for (int i = 0; i < _gatherRings.Length; i++)
        {
            Image ring = _gatherRings[i];
            if (ring == null)
                continue;
            count++;
            // 大きい輪郭ほど少し遅れて縮む。
            float delay = i * 0.07f;
            float local = Mathf.Clamp01((u - delay) / Mathf.Max(0.01f, 1f - delay));
            float ringEase = local * local;
            if (local > 0.5f)
            {
                float tail = (local - 0.5f) / 0.5f;
                ringEase = Mathf.Lerp(0.25f, 1f, 1f - (1f - tail) * (1f - tail));
            }
            float from = i < GatherRingFrom.Length ? GatherRingFrom[i] : 1.4f;
            float scale = Mathf.Lerp(from, 1.02f, ringEase);
            float alpha = Mathf.Lerp(0.35f, 1f, local);
            if (local < 0.08f)
                alpha *= local / 0.08f;
            if (u > 0.9f)
                alpha *= 1f - (u - 0.9f) / 0.1f * 0.35f;

            // キャラ輪郭付近（だいたい 1.35〜1.02）に入った分だけ白くする。
            const float overlapStart = 1.38f;
            const float overlapEnd = 1.02f;
            float overlap = 0f;
            if (scale <= overlapStart)
                overlap = 1f - Mathf.Clamp01((scale - overlapEnd) / (overlapStart - overlapEnd));
            overlapSum += overlap;

            // 外側=代表色＆太い → 近づくほど白＆細い、という二重グラデ。
            float whiteT = Mathf.Clamp01(ringEase * 0.92f + overlap * 0.35f);
            float widthPad = Mathf.Lerp(0.2f, 0.04f, whiteT);
            float corePull = Mathf.Lerp(0.03f, 0.01f, whiteT);

            Color haloColor = Color.Lerp(_color, Color.white, whiteT * 0.25f);
            haloColor.a = alpha * Mathf.Lerp(0.55f, 0.2f, whiteT);
            Color bodyColor = Color.Lerp(_color, Color.white, Mathf.Lerp(0.08f, 0.85f, whiteT));
            bodyColor.a = alpha * Mathf.Lerp(0.75f, 0.95f, whiteT);
            Color coreColor = Color.Lerp(_color, Color.white, Mathf.Lerp(0.4f, 1f, whiteT));
            coreColor.a = alpha * Mathf.Lerp(0.45f, 1f, whiteT);

            float haloScale = scale * (1f + widthPad);
            float coreScale = scale * (1f - corePull);
            PaintGatherLayer(_gatherHalos, _gatherHaloRts, i, haloScale, haloColor);
            PaintGatherLayer(_gatherRings, _gatherRingRts, i, scale, bodyColor);
            PaintGatherLayer(_gatherCores, _gatherCoreRts, i, coreScale, coreColor);
        }
        if (count < 1)
            return u;
        return Mathf.Clamp01(overlapSum / count);
    }

    static void PaintGatherLayer(Image[] images, RectTransform[] rects, int index, float scale, Color color)
    {
        if (images == null || rects == null || index < 0 || index >= images.Length)
            return;
        Image image = images[index];
        RectTransform rt = rects[index];
        if (image == null || rt == null)
            return;
        bool shown = color.a > 0.02f;
        image.gameObject.SetActive(shown);
        image.enabled = shown;
        image.color = color;
        rt.localScale = new Vector3(scale, scale, 1f);
        rt.anchoredPosition = Vector2.zero;
    }

    void DrawWhiteHold(float t)
    {
        HideSparks();
        HideGatherRings();
        RestoreHost();
        PaintOutline(1f);
        if (_outlineRt != null)
            _outlineRt.localScale = Vector3.one * 1.06f;
        PaintPortraitGlow(1f);
    }

    void DrawHop(float t)
    {
        float u = HopDuration <= 0f ? 1f : Mathf.Clamp01(t / HopDuration);
        float wave = HopWave(u);
        float height = Mathf.Clamp(_size * 0.22f, 14f, 34f);
        if (_host != null && _restReady)
        {
            _host.anchoredPosition = _hostRest + new Vector2(0f, wave * height);
            float stretch = 1f + 0.16f * wave;
            float squeeze = 1f - 0.06f * wave;
            _host.localScale = new Vector3(_hostScale.x * squeeze, _hostScale.y * stretch, _hostScale.z);
        }

        HideGatherRings();
        float glow = Mathf.Lerp(0.7f, 1f, wave);
        if (u > 0.55f)
            glow = Mathf.Lerp(1f, 0.2f, (u - 0.55f) / 0.45f);
        PaintOutline(glow);
        if (_outlineRt != null)
        {
            float spread = 1f + 0.48f * wave;
            _outlineRt.localScale = new Vector3(spread, spread, 1f);
        }
        // 発散までは真っ白のまま。色戻しは着地。
        if (!_landStarted)
            PaintPortraitGlow(1f);
    }

    void DrawLand(float t)
    {
        float u = LandDuration <= 0f ? 1f : Mathf.Clamp01(t / LandDuration);
        float reach = Mathf.Max(_size * 0.85f, 56f);
        for (int i = 0; i < SparkCount; i++)
        {
            float angle = i * Mathf.PI * 2f / SparkCount + 0.35f;
            float open = 1f - (1f - u) * (1f - u);
            float dist = Mathf.Lerp(10f, reach, open);
            float fade = 1f - Mathf.Clamp01((u - 0.5f) / 0.5f);
            float pop = Mathf.Clamp01(u / 0.1f) * fade;
            ShowSpark(i, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist, Mathf.Lerp(1.1f, 1.9f, open), pop);
        }
        // 光の発散と同時に、白オーバーレイを外して元の見た目へ。
        float snap = 1f - Mathf.Clamp01(u / 0.12f);
        snap = snap * snap;
        PaintPortraitGlow(snap);
    }

    void ShowSpark(int index, Vector2 pos, float scale, float alpha)
    {
        if (_sparks == null || index < 0 || index >= _sparks.Length)
            return;
        Image spark = _sparks[index];
        spark.gameObject.SetActive(true);
        spark.enabled = alpha > 0.01f;
        _sparkRts[index].anchoredPosition = pos;
        _sparkRts[index].localScale = new Vector3(scale, scale, 1f);
        Color c = Color.Lerp(_color, Color.white, 0.42f);
        c.a = Mathf.Clamp01(alpha * 1.15f);
        spark.color = c;
    }

    void HideSparks()
    {
        if (_sparks == null)
            return;
        for (int i = 0; i < _sparks.Length; i++)
        {
            if (_sparks[i] != null)
                _sparks[i].gameObject.SetActive(false);
        }
    }

    void HideGatherRings()
    {
        HideGatherArray(_gatherHalos);
        HideGatherArray(_gatherRings);
        HideGatherArray(_gatherCores);
    }

    static void HideGatherArray(Image[] images)
    {
        if (images == null)
            return;
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
                continue;
            images[i].enabled = false;
            images[i].gameObject.SetActive(false);
        }
    }

    static void InsertGatherArray(Image[] images, int insert)
    {
        if (images == null)
            return;
        for (int i = images.Length - 1; i >= 0; i--)
        {
            if (images[i] != null)
                images[i].transform.SetSiblingIndex(insert);
        }
    }

    Image[] CreateGatherLayer(string prefix)
    {
        Image[] images = new Image[GatherRingCount];
        for (int i = 0; i < GatherRingCount; i++)
        {
            GameObject ringObject = new GameObject(prefix + i.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringObject.transform.SetParent(transform, false);
            Image image = ringObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            images[i] = image;
            ringObject.SetActive(false);
        }
        return images;
    }

    static RectTransform[] GatherRects(Image[] images)
    {
        RectTransform[] rects = new RectTransform[images.Length];
        for (int i = 0; i < images.Length; i++)
            rects[i] = images[i] != null ? images[i].rectTransform : null;
        return rects;
    }

    void PaintOutline(float alpha)
    {
        if (_outline == null)
            return;
        Color c = Color.Lerp(_color, Color.white, 0.28f);
        c.a = Mathf.Clamp01(alpha);
        _outline.color = c;
        _outline.enabled = alpha > 0.01f;
    }

    // キャラの不透明部分だけを真っ白に塗る（Mask）。
    void PaintPortraitGlow(float white)
    {
        white = Mathf.Clamp01(white);
        if (_portrait != null && _portraitRestReady)
            _portrait.color = _portraitRest;
        if (_washRt == null || _washFill == null || _washMaskImage == null)
            return;
        if (white <= 0.01f)
        {
            HideWash();
            return;
        }
        if (_portrait != null)
        {
            if (_washMaskImage.sprite != _portrait.sprite)
                _washMaskImage.sprite = _portrait.sprite;
            _washRt.SetSiblingIndex(_portrait.transform.GetSiblingIndex() + 1);
        }
        _washRt.gameObject.SetActive(true);
        _washMaskImage.enabled = true;
        _washFill.enabled = true;
        _washFill.color = new Color(1f, 1f, 1f, white);
    }

    void HideWash()
    {
        if (_washFill != null)
        {
            _washFill.color = new Color(1f, 1f, 1f, 0f);
            _washFill.enabled = false;
        }
        if (_washRt != null)
            _washRt.gameObject.SetActive(false);
    }

    void RestorePortrait()
    {
        if (_portrait != null && _portraitRestReady)
            _portrait.color = _portraitRest;
        HideWash();
    }

    void EnsureBuilt()
    {
        if (_outline == null)
        {
            GameObject outlineObject = new GameObject("FusionFxOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            outlineObject.transform.SetParent(transform, false);
            _outline = outlineObject.GetComponent<Image>();
            _outline.raycastTarget = false;
            _outline.preserveAspect = false;
            _outline.type = Image.Type.Simple;
            _outlineRt = _outline.rectTransform;
        }
        Transform staleSoak = transform.Find("FusionFxSoak");
        if (staleSoak != null)
            Destroy(staleSoak.gameObject);

        if (_washRt == null)
        {
            Transform existingWash = transform.Find("FusionFxWhiteWash");
            GameObject washObject;
            if (existingWash != null)
                washObject = existingWash.gameObject;
            else
            {
                washObject = new GameObject("FusionFxWhiteWash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
                washObject.transform.SetParent(transform, false);
            }
            _washRt = washObject.GetComponent<RectTransform>();
            _washMaskImage = washObject.GetComponent<Image>();
            if (_washMaskImage == null)
                _washMaskImage = washObject.AddComponent<Image>();
            _washMaskImage.raycastTarget = false;
            _washMaskImage.preserveAspect = true;
            _washMaskImage.type = Image.Type.Simple;
            _washMaskImage.color = Color.white;
            _washMask = washObject.GetComponent<Mask>();
            if (_washMask == null)
                _washMask = washObject.AddComponent<Mask>();
            _washMask.showMaskGraphic = false;

            Transform fillTransform = washObject.transform.Find("Fill");
            GameObject fillObject = fillTransform != null ? fillTransform.gameObject : null;
            if (fillObject == null)
            {
                fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillObject.transform.SetParent(washObject.transform, false);
            }
            _washFill = fillObject.GetComponent<Image>();
            if (_washFill == null)
                _washFill = fillObject.AddComponent<Image>();
            _washFill.sprite = SolidWhite();
            _washFill.raycastTarget = false;
            _washFill.type = Image.Type.Simple;
            _washFill.color = new Color(1f, 1f, 1f, 0f);
            Stretch(_washFill.rectTransform);
            washObject.SetActive(false);
        }
        if (_sparks == null)
        {
            _sparks = new Image[SparkCount];
            _sparkRts = new RectTransform[SparkCount];
            for (int i = 0; i < SparkCount; i++)
            {
                GameObject sparkObject = new GameObject("FusionFxSpark" + i.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                sparkObject.transform.SetParent(transform, false);
                Image image = sparkObject.GetComponent<Image>();
                image.sprite = Disc();
                image.raycastTarget = false;
                RectTransform rt = image.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(22f, 22f);
                _sparks[i] = image;
                _sparkRts[i] = rt;
                sparkObject.SetActive(false);
            }
        }
        if (_gatherRings == null || _gatherHalos == null || _gatherCores == null)
        {
            DestroyGatherNamed("FusionFxGatherHalo");
            DestroyGatherNamed("FusionFxGatherRing");
            DestroyGatherNamed("FusionFxGatherCore");
            _gatherHalos = CreateGatherLayer("FusionFxGatherHalo");
            _gatherHaloRts = GatherRects(_gatherHalos);
            _gatherRings = CreateGatherLayer("FusionFxGatherRing");
            _gatherRingRts = GatherRects(_gatherRings);
            _gatherCores = CreateGatherLayer("FusionFxGatherCore");
            _gatherCoreRts = GatherRects(_gatherCores);
        }
    }

    void DestroyGatherNamed(string prefix)
    {
        for (int i = 0; i < GatherRingCount + 2; i++)
        {
            Transform found = transform.Find(prefix + i.ToString());
            if (found != null)
                Destroy(found.gameObject);
        }
    }

    void FitOutlineLike(RectTransform rt)
    {
        if (rt == null)
            return;
        float extraX = (_outlineScaleX - 1f) * 0.5f;
        float extraY = (_outlineScaleY - 1f) * 0.5f;
        if (extraX < 0f)
            extraX = 0f;
        if (extraY < 0f)
            extraY = 0f;
        rt.anchorMin = new Vector2(-extraX, -extraY);
        rt.anchorMax = new Vector2(1f + extraX, 1f + extraY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    void FitGatherArray(Image[] images, RectTransform[] rects)
    {
        if (images == null)
            return;
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
                continue;
            images[i].sprite = _outlineSprite;
            FitOutlineLike(rects != null && i < rects.Length ? rects[i] : images[i].rectTransform);
            images[i].enabled = false;
            images[i].gameObject.SetActive(false);
        }
    }

    void FitLayers()
    {
        if (_host == null)
            return;
        float width = _host.rect.width;
        float height = _host.rect.height;
        _size = Mathf.Min(width, height);
        if (_size < 8f)
            _size = 120f;

        Sprite source = _portrait != null ? _portrait.sprite : null;
        ReleaseOutline();
        if (source != null && source.texture != null && source.texture.isReadable)
        {
            float scaleX;
            float scaleY;
            _outlineSprite = PixelWorkerFx.CreateOutline(source, out scaleX, out scaleY);
            _outlineScaleX = scaleX;
            _outlineScaleY = scaleY;
        }

        if (_outline != null)
        {
            _outline.sprite = _outlineSprite;
            _outline.enabled = _outlineSprite != null;
            FitOutlineLike(_outlineRt);
        }

        FitGatherArray(_gatherHalos, _gatherHaloRts);
        FitGatherArray(_gatherRings, _gatherRingRts);
        FitGatherArray(_gatherCores, _gatherCoreRts);

        if (_washRt != null && _washMaskImage != null)
        {
            _washMaskImage.sprite = source;
            _washMaskImage.preserveAspect = _portrait != null && _portrait.preserveAspect;
            if (_portraitRt != null)
            {
                _washRt.anchorMin = _portraitRt.anchorMin;
                _washRt.anchorMax = _portraitRt.anchorMax;
                _washRt.pivot = _portraitRt.pivot;
                _washRt.anchoredPosition = _portraitRt.anchoredPosition;
                _washRt.sizeDelta = _portraitRt.sizeDelta;
                _washRt.offsetMin = _portraitRt.offsetMin;
                _washRt.offsetMax = _portraitRt.offsetMax;
            }
            else
                Stretch(_washRt);
            _washRt.localScale = Vector3.one;
            if (_washFill != null)
                Stretch(_washFill.rectTransform);
            HideWash();
        }

        if (_sparks != null)
        {
            float spark = Mathf.Clamp(_size * 0.22f, 22f, 42f);
            for (int i = 0; i < _sparks.Length; i++)
                _sparkRts[i].sizeDelta = new Vector2(spark, spark);
        }

        // 輪郭リングは絵の後ろ、白塗りは絵の上、火花は最前面。
        Transform glow = transform.Find("AttributeGlow");
        int insert = 0;
        if (glow != null)
            insert = glow.GetSiblingIndex() + 1;
        // 外側ハロ → 本体 → 白コアの順（手前ほど白い細い線）。
        InsertGatherArray(_gatherHalos, insert);
        InsertGatherArray(_gatherRings, insert);
        InsertGatherArray(_gatherCores, insert);
        if (_outline != null)
            _outline.transform.SetSiblingIndex(insert);
        if (_washRt != null && _portrait != null)
            _washRt.SetSiblingIndex(_portrait.transform.GetSiblingIndex() + 1);
        if (_sparks != null)
        {
            for (int i = 0; i < _sparks.Length; i++)
            {
                if (_sparks[i] != null)
                    _sparks[i].transform.SetAsLastSibling();
            }
        }
    }

    void Finish(bool invokeDone)
    {
        _playing = false;
        HideSparks();
        HideGatherRings();
        PaintOutline(0f);
        if (_outline != null)
            _outline.enabled = false;
        RestorePortrait();
        RestoreHost();
        ReleaseOutline();
        enabled = false;
        Action done = _onDone;
        _onDone = null;
        if (invokeDone && done != null)
            done();
    }

    void RestoreHost()
    {
        if (_host == null || !_restReady)
            return;
        _host.anchoredPosition = _hostRest;
        _host.localScale = _hostScale;
    }

    void ReleaseOutline()
    {
        if (_outlineSprite == null)
            return;
        if (_outlineSprite.texture != null)
            Destroy(_outlineSprite.texture);
        Destroy(_outlineSprite);
        _outlineSprite = null;
    }

    void OnDestroy()
    {
        ReleaseOutline();
    }

    static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static float HopWave(float t)
    {
        if (t <= 0f || t >= 1f)
            return 0f;
        if (t < 0.7f)
            return Mathf.Sin(t / 0.7f * Mathf.PI);
        return Mathf.Sin((t - 0.7f) / 0.3f * Mathf.PI) * 0.22f;
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

    static Sprite SolidWhite()
    {
        if (_solidWhite != null)
            return _solidWhite;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _solidWhite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
        _solidWhite.hideFlags = HideFlags.HideAndDontSave;
        return _solidWhite;
    }
}
