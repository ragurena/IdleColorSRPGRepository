using UnityEngine;
using UnityEngine.UI;

// キャラ生産の担当。作業中は右へ小さく跳んで、次は左へ小さく跳ぶ。回転も伸び縮みもしない。
// 1体できあがったときは真上に大きく飛んで、着地した瞬間に、RGB生産と同じ床の波紋と粒が湧く。
public class CharacterWorkerFx : MonoBehaviour
{
    const int SparkCount = 7;
    const float HopPause = 0.35f;
    const float JumpDuration = 0.48f;
    const float RippleDuration = 0.4f;
    const float EffectDuration = 0.55f;
    const float CelebrateDuration = JumpDuration + EffectDuration;

    static Sprite _glow;
    static Sprite _line;
    static Sprite _disc;
    static Sprite _spark;

    RectTransform _buttonRt;
    RectTransform _backRoot;
    RectTransform _frontRoot;
    Image _figure;
    bool _hidImage;
    Color _shownImage;
    bool _portraitParked;
    int _portraitSibling;
    RingHalf _backA;
    RingHalf _frontA;
    RingHalf _backB;
    RingHalf _frontB;
    Image _flash;
    RectTransform _flashRt;
    Image[] _sparks;
    Image[] _sparkCores;
    RectTransform[] _sparkRts;
    Vector2 _rest;
    Vector3 _restScale;
    Quaternion _restRotation;
    bool _restReady;
    bool _working;
    bool _celebrating;
    bool _moveBody;
    float _celebrateTime;
    float _size;
    float _footY;
    float _sprayWidth;
    float _sprayHeight;
    Color _tint = Color.white;

    public static float ProgressElapsed { get; private set; }
    public static float ProgressInterval { get; private set; }
    public static int ProgressTick { get; private set; }

    public static void SetProgressClock(float elapsed, float interval)
    {
        ProgressElapsed = elapsed;
        ProgressInterval = interval > 0.05f ? interval : 1f;
    }

    public static void NoteProgressTick()
    {
        ProgressTick++;
    }

    public static CharacterWorkerFx Attach(Button button)
    {
        if (button == null)
            return null;
        CharacterWorkerFx fx = button.GetComponent<CharacterWorkerFx>();
        if (fx == null)
            fx = button.gameObject.AddComponent<CharacterWorkerFx>();
        return fx;
    }

    public void Hide()
    {
        _working = false;
        _celebrating = false;
        RestoreBody();
        ReleaseLayers();
        _restReady = false;
        enabled = false;
    }

    // 行を複製したとき、元の担当の位置や粒を壊さないよう参照だけ外す。
    public void DiscardSharedCopy()
    {
        if (_buttonRt == transform as RectTransform && _restReady)
            RestoreBody();
        ReleaseLayers();
        _backRoot = null;
        _frontRoot = null;
        _figure = null;
        _backA = null;
        _frontA = null;
        _backB = null;
        _frontB = null;
        _flash = null;
        _flashRt = null;
        _sparks = null;
        _sparkCores = null;
        _sparkRts = null;
        _buttonRt = null;
        _restReady = false;
        _working = false;
        _celebrating = false;
        enabled = false;
    }

    public void Work(Color tint)
    {
        _tint = tint;
        if (_celebrating)
            return;
        if (!_working)
            CaptureRest();
        _working = true;
        ReleaseLayers();
        enabled = true;
    }

    public void Celebrate(Color tint, bool moveBody)
    {
        _tint = tint;
        _moveBody = moveBody;
        if (!_working)
            CaptureRest();
        _working = moveBody;
        _celebrating = true;
        _celebrateTime = 0f;
        ReleaseLayers();
        enabled = true;
    }

    void Awake()
    {
        _buttonRt = transform as RectTransform;
        enabled = false;
    }

    void OnDestroy()
    {
        ReleaseLayers();
        if (_backRoot != null)
            Destroy(_backRoot.gameObject);
        if (_frontRoot != null)
            Destroy(_frontRoot.gameObject);
        if (_figure != null)
            Destroy(_figure.gameObject);
    }

    void LateUpdate()
    {
        if (_buttonRt == null)
            _buttonRt = transform as RectTransform;
        if (_buttonRt == null)
            return;
        if (_celebrating)
        {
            _celebrateTime += Time.deltaTime;
            DrawCelebrate();
            float length = _moveBody ? CelebrateDuration : EffectDuration;
            if (_celebrateTime >= length)
            {
                _celebrating = false;
                if (!_moveBody)
                {
                    RestoreBody();
                    ReleaseLayers();
                    _working = false;
                    enabled = false;
                }
            }
            return;
        }
        if (_working)
            DrawWork();
    }

    void DrawWork()
    {
        float width;
        float height;
        if (!BodySize(out width, out height))
            return;
        float interval = ProgressInterval > 0.05f ? ProgressInterval : 1f;
        float local = ProgressElapsed / interval;
        if (local < 0f)
            local = 0f;
        if (local > 1f)
            local = 1f;
        float pause = HopPause;
        if (pause > interval * 0.4f)
            pause = interval * 0.4f;
        float pauseU = pause / interval;
        int sideOffset = Mathf.Abs(GetInstanceID()) & 1;
        bool landedRight = ((ProgressTick + sideOffset) & 1) == 0;
        float from = landedRight ? 1f : -1f;
        float to = -from;
        float travel = from;
        float arc = 0f;
        if (local >= pauseU)
        {
            float span = 1f - pauseU;
            float u = span <= 0f ? 1f : (local - pauseU) / span;
            float eased = Mathf.Lerp(u, u * u * (3f - 2f * u), 0.75f);
            travel = Mathf.Lerp(from, to, eased);
            arc = Mathf.Sin(eased * Mathf.PI);
        }
        float sway = Mathf.Clamp(width * 0.04f, 5f, 11f);
        float bounce = Mathf.Clamp(height * 0.06f, 6f, 12f);
        _buttonRt.anchoredPosition = _rest + new Vector2(travel * sway, bounce * arc);
        HoldPose();
        ReleaseLayers();
    }

    void DrawCelebrate()
    {
        float width;
        float height;
        if (!BodySize(out width, out height))
            return;
        if (!_moveBody)
        {
            _buttonRt.anchoredPosition = _rest;
            HoldPose();
            DrawFloor(_celebrateTime, width, height);
            return;
        }
        if (_celebrateTime < JumpDuration)
        {
            float hopT = _celebrateTime / JumpDuration;
            float arc = Mathf.Sin(hopT * Mathf.PI);
            float bounce = Mathf.Clamp(height * 0.58f, 44f, 96f);
            _buttonRt.anchoredPosition = _rest + new Vector2(0f, bounce * arc);
            HoldPose();
            ReleaseLayers();
            return;
        }

        _buttonRt.anchoredPosition = _rest;
        HoldPose();
        float elapsed = _celebrateTime - JumpDuration;
        DrawFloor(elapsed, width, height);
    }

    void DrawFloor(float elapsed, float width, float height)
    {
        EnsureVisuals();
        if (_backRoot == null || _frontRoot == null)
            return;
        _backRoot.gameObject.SetActive(true);
        _frontRoot.gameObject.SetActive(true);
        _size = width < height ? width : height;
        if (_size < 8f)
            _size = 72f;
        _footY = -height * 0.4f;
        _sprayWidth = _size * 0.72f;
        _sprayHeight = height * 1.45f;
        PlaceRing(_backA);
        PlaceRing(_frontA);
        PlaceRing(_backB);
        PlaceRing(_frontB);
        if (_flashRt != null)
        {
            _flashRt.sizeDelta = new Vector2(_size * 0.72f, _size * 0.72f);
            PlaceFlashAtFeet(_flashRt);
        }
        float sparkSize = Mathf.Max(10f, _size * 0.2f);
        if (_sparkRts != null)
        {
            for (int i = 0; i < _sparkRts.Length; i++)
            {
                if (_sparkRts[i] != null)
                    _sparkRts[i].sizeDelta = new Vector2(sparkSize, sparkSize);
            }
        }

        DrawRipple(_backA, _frontA, 0f, RippleDuration, 0.16f, 1.22f, 0.8f, elapsed);
        DrawRipple(_backB, _frontB, 0.08f, RippleDuration - 0.06f, 0.1f, 0.92f, 0.8f, elapsed);
        DrawFlash(elapsed);
        DrawSparks(elapsed);
        PlaceLayers();
    }

    void DrawRipple(RingHalf back, RingHalf front, float delay, float life, float from, float to, float alpha, float elapsed)
    {
        float t = elapsed - delay;
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
        Color c = _tint;
        c.a = a;
        PaintRing(back, c, a);
        PaintRing(front, c, a);
    }

    void DrawFlash(float elapsed)
    {
        const float life = 0.16f;
        if (_flash == null || elapsed < 0f || elapsed > life)
        {
            if (_flash != null)
                _flash.enabled = false;
            return;
        }
        _flash.enabled = true;
        float u = elapsed / life;
        float scale = Mathf.Lerp(0.08f, 0.7f, 1f - (1f - u) * (1f - u));
        _flashRt.localScale = new Vector3(scale, scale, 1f);
        Color c = Color.Lerp(_tint, Color.white, 0.28f);
        c.a = 1f - u * u;
        _flash.color = c;
    }

    void DrawSparks(float elapsed)
    {
        if (_sparks == null)
            return;
        for (int i = 0; i < SparkCount; i++)
        {
            float side = SparkCount <= 1 ? 0f : (i / (SparkCount - 1f)) * 2f - 1f;
            bool fast = (i % 2) == 0;
            float start = Mathf.Abs(side) * 0.02f;
            float life = fast ? 0.32f : 0.5f;
            float t = elapsed - start;
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
            Color c = Color.Lerp(_tint, Color.white, (1f - u) * 0.2f);
            c.a = pop;
            image.color = c;
            if (_sparkCores != null && _sparkCores[i] != null)
                _sparkCores[i].color = new Color(1f, 1f, 1f, pop);
        }
    }

    void HoldPose()
    {
        _buttonRt.localRotation = _restRotation;
        _buttonRt.localScale = _restScale;
    }

    bool BodySize(out float width, out float height)
    {
        width = _buttonRt.rect.width;
        height = _buttonRt.rect.height;
        if (width < 1f || height < 1f)
            return false;
        if (!_restReady)
            CaptureRest();
        return _restReady;
    }

    void CaptureRest()
    {
        if (_buttonRt == null)
            _buttonRt = transform as RectTransform;
        if (_buttonRt == null)
            return;
        Vector3 euler = _buttonRt.localEulerAngles;
        float z = euler.z > 180f ? euler.z - 360f : euler.z;
        if (z < -180f)
            z += 360f;
        if (Mathf.Abs(z) < 8f)
            _buttonRt.localRotation = Quaternion.Euler(euler.x, euler.y, 0f);
        _rest = _buttonRt.anchoredPosition;
        _restScale = _buttonRt.localScale;
        if (_restScale == Vector3.zero)
            _restScale = Vector3.one;
        if (Mathf.Abs(_restScale.x - _restScale.y) < 0.12f)
        {
            float uniform = (_restScale.x + _restScale.y) * 0.5f;
            _restScale = new Vector3(uniform, uniform, _restScale.z);
            _buttonRt.localScale = _restScale;
        }
        _restRotation = _buttonRt.localRotation;
        _restReady = true;
    }

    void RestoreBody()
    {
        if (_buttonRt == null || !_restReady)
            return;
        _buttonRt.anchoredPosition = _rest;
        _buttonRt.localScale = _restScale;
        _buttonRt.localRotation = _restRotation;
    }

    void EnsureVisuals()
    {
        if (_backRoot != null && _frontRoot != null && _backA != null && _frontA != null && _sparks != null && _sparks.Length == SparkCount)
            return;
        DestroyChild("CharacterWorkerSparks");
        DestroyChild("CharacterWorkerBack");
        DestroyChild("CharacterWorkerFront");
        DestroyChild("CharacterWorkerFigure");

        _backRoot = CreateLayer("CharacterWorkerBack");
        _backA = CreateHalfRing("RingABack", true, _backRoot);
        _backB = CreateHalfRing("RingBBack", true, _backRoot);

        _figure = CreateImage("CharacterWorkerFigure", transform, null);
        FillRect(_figure.rectTransform);
        _figure.gameObject.SetActive(false);

        _frontRoot = CreateLayer("CharacterWorkerFront");
        _frontA = CreateHalfRing("RingAFront", false, _frontRoot);
        _frontB = CreateHalfRing("RingBFront", false, _frontRoot);
        _flash = CreateImage("Flash", _frontRoot, DiscSprite());
        _flashRt = _flash.rectTransform;
        AnchorCenter(_flashRt);
        PlaceFlashAtFeet(_flashRt);

        _sparks = new Image[SparkCount];
        _sparkCores = new Image[SparkCount];
        _sparkRts = new RectTransform[SparkCount];
        Sprite spark = SparkSprite();
        Sprite core = DiscSprite();
        float sparkSize = 18f;
        for (int i = 0; i < SparkCount; i++)
        {
            GameObject holder = new GameObject("Spark" + i.ToString(), typeof(RectTransform));
            holder.transform.SetParent(_frontRoot, false);
            _sparkRts[i] = holder.GetComponent<RectTransform>();
            AnchorCenter(_sparkRts[i]);
            _sparkRts[i].sizeDelta = new Vector2(sparkSize, sparkSize);
            _sparks[i] = CreateImage("Glow", holder.transform, spark);
            FillRect(_sparks[i].rectTransform);
            _sparkCores[i] = CreateImage("Core", holder.transform, core);
            CenterPortion(_sparkCores[i].rectTransform, 0.42f);
        }
    }

    RectTransform CreateLayer(string name)
    {
        GameObject rootObject = new GameObject(name, typeof(RectTransform));
        rootObject.transform.SetParent(transform, false);
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        return root;
    }

    void DestroyChild(string name)
    {
        Transform child = transform.Find(name);
        if (child != null)
            Destroy(child.gameObject);
    }

    void PlaceLayers()
    {
        if (_backRoot == null || _frontRoot == null)
            return;
        _backRoot.SetAsFirstSibling();
        Transform portrait = transform.Find("PersonPortrait");
        bool person = portrait != null && portrait.gameObject.activeSelf;
        if (person)
        {
            if (!_portraitParked)
            {
                _portraitSibling = portrait.GetSiblingIndex();
                _portraitParked = true;
            }
            portrait.SetSiblingIndex(_backRoot.GetSiblingIndex() + 1);
            if (_figure != null)
                _figure.gameObject.SetActive(false);
        }
        else
        {
            ShowFigure();
            if (_figure != null)
                _figure.rectTransform.SetSiblingIndex(_backRoot.GetSiblingIndex() + 1);
        }
        int character = person ? portrait.GetSiblingIndex() : (_figure != null ? _figure.rectTransform.GetSiblingIndex() : 0);
        _frontRoot.SetSiblingIndex(character + 1);
        Transform bar = transform.Find("PersonSkillBar");
        if (bar != null)
            bar.SetAsLastSibling();
    }

    void ShowFigure()
    {
        Button button = GetComponent<Button>();
        Image source = button != null ? button.image : null;
        if (source == null || _figure == null)
            return;
        _figure.sprite = source.sprite;
        _figure.type = source.type;
        _figure.preserveAspect = source.preserveAspect;
        if (!_hidImage)
        {
            _shownImage = source.color;
            _hidImage = true;
        }
        Color shown = _shownImage;
        if (shown.a < 0.99f)
            shown.a = 1f;
        _figure.color = shown;
        _figure.gameObject.SetActive(true);
        source.color = new Color(_shownImage.r, _shownImage.g, _shownImage.b, 0f);
    }

    void ReleaseLayers()
    {
        if (_portraitParked)
        {
            Transform portrait = transform.Find("PersonPortrait");
            if (portrait != null)
            {
                int index = _portraitSibling;
                if (index < 0)
                    index = 0;
                if (index > transform.childCount - 1)
                    index = transform.childCount - 1;
                portrait.SetSiblingIndex(index);
            }
            _portraitParked = false;
        }
        if (_hidImage)
        {
            Button button = GetComponent<Button>();
            Image source = button != null ? button.image : null;
            if (source != null && source.color.a <= 0.01f)
                source.color = _shownImage;
            _hidImage = false;
        }
        if (_figure != null)
            _figure.gameObject.SetActive(false);
        if (_backRoot != null)
            _backRoot.gameObject.SetActive(false);
        if (_frontRoot != null)
            _frontRoot.gameObject.SetActive(false);
    }

    void PlaceRing(RingHalf ring)
    {
        if (ring == null || ring.Root == null)
            return;
        ring.Root.sizeDelta = new Vector2(_size, _size);
        ring.Root.anchorMin = new Vector2(0.5f, 0f);
        ring.Root.anchorMax = new Vector2(0.5f, 0f);
        ring.Root.pivot = new Vector2(0.5f, 0.5f);
        float height = _buttonRt != null ? _buttonRt.rect.height : _size;
        ring.Root.anchoredPosition = new Vector2(0f, height * 0.14f);
    }

    void PlaceFlashAtFeet(RectTransform rt)
    {
        if (rt == null)
            return;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        float height = _buttonRt != null ? _buttonRt.rect.height : _size;
        rt.anchoredPosition = new Vector2(0f, height * 0.14f);
    }

    RingHalf CreateHalfRing(string name, bool upper, Transform parent)
    {
        RingHalf half = new RingHalf();
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
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

    static void AnchorCenter(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
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
