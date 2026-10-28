using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 読み込んだ絵が、下の段から、段の中はばらばらの順で降りてキャラになる画面。
public class ImportRebirthPlayer : MonoBehaviour
{
    const float MaskWidth = 440f;
    const float MaskHeight = 470f;
    const float MaskCenterY = 328f;
    const float BodySize = 300f;
    const float BodyBottomInset = 16f;
    const float RowSpan = 1.25f;
    const float RowScatter = 0.18f;
    const float FallSeconds = 0.5f;
    const float FlashSeconds = 0.33f;
    const float SparkSeconds = 0.88f;
    const float BarSeconds = 0.42f;

    ControllerProduction _production;
    Font _font;
    Action<string> _onClosed;
    int _generation;
    Coroutine _routine;
    bool _finished;
    bool _skip;
    float _acceptSkipAt;
    string _doneMessage = "";
    CharacterClass _character;

    Text _name;
    Text _attribute;
    Text _hint;
    Button _skipButton;
    Button _backButton;
    RectTransform _mask;
    RectTransform _body;
    Vector2 _bodyRest;
    RawImage _picture;
    Texture2D _built;
    Texture2D _source;
    Texture2D _whiteTexture;
    Sprite _whiteSprite;
    Color32[] _pixels;
    bool _readable;
    int _width;
    int _height;
    float _spawnY;
    float _bodyBottom;
    Color32[] _canvasPixels;
    bool _canvasDirty;
    int _landedCount;
    int _grainCount;
    RawImage _flash;
    Texture2D _silhouette;
    readonly List<Spark> _sparks = new List<Spark>();
    readonly List<Dot> _dots = new List<Dot>();
    Gauge[] _gauges;
    Image[] _lucPips;
    Text _lucValue;
    Text _lucMax;
    ulong _lucAmount;
    Image _bandR;
    Image _bandG;
    Image _bandB;
    Text _bandValue;
    Text _bandMax;
    ulong _bandRAmount;
    ulong _bandGAmount;
    ulong _bandBAmount;
    ulong _bandCanvas;

    class Dot
    {
        public GameObject Object;
        public RectTransform Core;
        public RectTransform White;
        public RectTransform Neon;
        public int Index;
        public float Start;
        public float Land;
        public float FromX;
        public float FromY;
        public float ToX;
        public float ToY;
        public float Phase;
        public bool Landed;
    }

    class Spark
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 From;
        public Vector2 To;
        public float Size;
    }

    class Gauge
    {
        public Image Fill;
        public Text Value;
        public Text Max;
        public ulong Amount;
        public ulong Ceiling;
        // HP と ATK は、最大との差が左端に潰れるので、長さだけ割合の平方根にする。
        public bool Sqrt;
    }

    public void Initialize(ControllerProduction production, Font font, Action<string> onClosed)
    {
        _production = production;
        _font = font;
        _onClosed = onClosed;
        _whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _whiteTexture.SetPixel(0, 0, Color.white);
        _whiteTexture.Apply(false);
        _whiteTexture.filterMode = FilterMode.Point;
        _whiteSprite = Sprite.Create(_whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        BuildUi();
        gameObject.SetActive(false);
    }

    public void Play(CharacterClass character, string doneMessage)
    {
        Halt();
        _character = character;
        _doneMessage = doneMessage ?? "";
        _finished = false;
        _skip = false;
        _acceptSkipAt = Time.unscaledTime + 0.3f;
        gameObject.SetActive(true);
        if (_skipButton != null)
            _skipButton.interactable = true;
        if (_backButton != null)
            _backButton.gameObject.SetActive(false);
        if (_hint != null)
            _hint.gameObject.SetActive(true);
        int generation = _generation;
        _routine = StartCoroutine(Run(generation));
    }

    public void Halt()
    {
        _generation++;
        _skip = false;
        _finished = false;
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
        ClearDots();
        ClearSparks();
        HideFlash();
        ReleaseBuilt();
        if (_picture != null)
            _picture.color = Color.white;
        if (_body != null)
        {
            _body.anchoredPosition = _bodyRest;
            _body.localScale = Vector3.one;
        }
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void PushSkip()
    {
        if (_finished)
            return;
        if (Time.unscaledTime < _acceptSkipAt)
            return;
        _skip = true;
    }

    public void PushBack()
    {
        if (!_finished)
            return;
        string message = _doneMessage;
        Halt();
        if (_onClosed != null)
            _onClosed(message);
    }

    void OnDestroy()
    {
        ClearDots();
        ReleaseBuilt();
        if (_whiteSprite != null)
            Destroy(_whiteSprite);
        if (_whiteTexture != null)
            Destroy(_whiteTexture);
    }

    IEnumerator Run(int generation)
    {
        BeginPicture();
        if (!Alive(generation))
            yield break;
        if (_readable)
            yield return Stack(generation);
        else
            yield return Hold(generation, 0.7f);
        if (!Alive(generation) || _finished)
            yield break;
        yield return Hold(generation, 0.13f);
        if (!Alive(generation) || _finished)
            yield break;
        yield return Flash(generation);
        if (!Alive(generation) || _finished)
            yield break;
        yield return Sparkle(generation);
        if (!Alive(generation) || _finished)
            yield break;
        yield return Hop(generation, 26f, 0.28f);
        if (!Alive(generation) || _finished)
            yield break;
        yield return Hop(generation, 12f, 0.22f);
        if (!Alive(generation) || _finished)
            yield break;
        yield return FillGauges(generation);
        if (!Alive(generation) || _finished)
            yield break;
        ShowCompleted();
    }

    bool Alive(int generation)
    {
        return generation == _generation && isActiveAndEnabled;
    }

    void BeginPicture()
    {
        if (_name != null)
            _name.text = _character != null ? _character.Name : "";
        if (_attribute != null)
            _attribute.text = "";
        if (_body != null)
        {
            _body.anchoredPosition = _bodyRest;
            _body.localScale = Vector3.one;
        }
        if (_picture != null)
            _picture.color = Color.white;

        _source = _character != null ? _character.ImageTexture2D : null;
        _pixels = null;
        _readable = false;
        _width = 1;
        _height = 1;
        ReleaseBuilt();
        ClearDots();

        if (_source != null && _source.width > 0 && _source.height > 0)
        {
            _width = _source.width;
            _height = _source.height;
            _readable = TryRead(_source, out _pixels);
        }
        else if (_character != null && _character.Size > 0)
        {
            _width = _character.Size;
            _height = _character.Size;
        }

        BindGauges();
        ResetGauges();
        HideFlash();

        if (!_readable)
        {
            if (_picture != null)
                _picture.texture = _source;
            return;
        }

        _built = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        _built.filterMode = FilterMode.Point;
        _built.wrapMode = TextureWrapMode.Clamp;
        _canvasPixels = new Color32[_width * _height];
        _built.SetPixels32(_canvasPixels);
        _built.Apply(false);
        _canvasDirty = false;
        if (_picture != null)
            _picture.texture = _built;

        BuildSilhouette();
        ScheduleGrains();
    }

    void ScheduleGrains()
    {
        _dots.Clear();
        _landedCount = 0;
        _grainCount = 0;
        float cellX = BodySize / _width;
        float cellY = BodySize / _height;
        int showPerRow = _width <= 16 ? _width : (_width <= 32 ? 8 : 5);
        Color neon = NeonColor();
        for (int y = 0; y < _height; y++)
        {
            List<int> columns = new List<int>();
            for (int x = 0; x < _width; x++)
            {
                if (_pixels[y * _width + x].a != 0)
                    columns.Add(x);
            }
            Shuffle(columns);
            float rowStart = _height <= 1 ? 0.05f : 0.05f + RowSpan * y / (_height - 1f);
            int shown = 0;
            for (int n = 0; n < columns.Count; n++)
            {
                int x = columns[n];
                int index = y * _width + x;
                float toX = -BodySize * 0.5f + (x + 0.5f) * cellX;
                float toY = _bodyBottom + (y + 0.5f) * cellY;
                float fall = FallSeconds + UnityEngine.Random.Range(-0.06f, 0.08f);
                if (fall < 0.36f)
                    fall = 0.36f;
                Dot dot = new Dot();
                dot.Index = index;
                dot.Start = rowStart + UnityEngine.Random.Range(0f, RowScatter);
                dot.Land = dot.Start + fall;
                dot.FromX = toX + UnityEngine.Random.Range(-18f, 18f);
                dot.FromY = _spawnY + UnityEngine.Random.Range(0f, 36f);
                dot.ToX = toX;
                dot.ToY = toY;
                dot.Phase = UnityEngine.Random.Range(0f, 6.2832f);
                if (shown < showPerRow)
                {
                    SpawnGrain(dot, cellX, cellY, neon);
                    shown++;
                }
                _dots.Add(dot);
                _grainCount++;
            }
        }
    }

    static void Shuffle(List<int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int swap = values[i];
            values[i] = values[j];
            values[j] = swap;
        }
    }

    void SpawnGrain(Dot dot, float cellX, float cellY, Color neon)
    {
        float size = cellX < cellY ? cellX : cellY;
        if (size < 4f)
            size = 4f;
        GameObject root = new GameObject("FallDot", typeof(RectTransform));
        root.transform.SetParent(_mask, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = Vector2.zero;
        dot.Neon = CreateGlow(root.transform, "Neon", size * 2.4f, neon);
        dot.White = CreateGlow(root.transform, "White", size * 1.35f, Color.white);
        dot.Core = CreateGlow(root.transform, "Core", size, _pixels[dot.Index]);
        dot.Object = root;
        root.SetActive(false);
    }

    RectTransform CreateGlow(Transform parent, string name, float size, Color color)
    {
        GameObject glow = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glow.transform.SetParent(parent, false);
        RectTransform rect = glow.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        Image image = glow.GetComponent<Image>();
        image.sprite = _whiteSprite;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    static bool TryRead(Texture2D tex, out Color32[] pixels)
    {
        pixels = null;
        try
        {
            pixels = tex.GetPixels32();
        }
        catch (UnityException)
        {
            return false;
        }
        if (pixels == null || tex.width < 1 || tex.height < 1)
            return false;
        return pixels.Length == tex.width * tex.height;
    }

    IEnumerator Stack(int generation)
    {
        float elapsed = 0f;
        while (_landedCount < _grainCount)
        {
            if (!Alive(generation))
                yield break;
            if (_skip)
            {
                ShowCompleted();
                yield break;
            }
            MoveGrains(elapsed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        FlushCanvas();
    }

    void MoveGrains(float elapsed)
    {
        for (int i = 0; i < _dots.Count; i++)
        {
            Dot dot = _dots[i];
            if (dot.Landed)
                continue;
            if (elapsed < dot.Start)
            {
                if (dot.Object != null)
                    dot.Object.SetActive(false);
                continue;
            }
            if (dot.Object != null && !dot.Object.activeSelf)
                dot.Object.SetActive(true);
            float span = dot.Land - dot.Start;
            float u = span <= 0.0001f ? 1f : Mathf.Clamp01((elapsed - dot.Start) / span);
            if (dot.Object != null)
                PlaceGrain(dot, u);
            if (u < 1f && elapsed < dot.Land)
                continue;
            CommitGrain(dot);
        }
        FlushCanvas();
    }

    void PlaceGrain(Dot dot, float u)
    {
        Vector2 head = GrainPoint(dot, u);
        if (dot.Core != null)
            dot.Core.anchoredPosition = head;
        PlaceTrail(dot.White, dot, u, 0.1f, 1f);
        PlaceTrail(dot.Neon, dot, u, 0.24f, 0.9f);
    }

    void PlaceTrail(RectTransform trail, Dot dot, float u, float behind, float alpha)
    {
        if (trail == null)
            return;
        float back = u - behind;
        if (back < 0f)
            back = 0f;
        trail.anchoredPosition = GrainPoint(dot, back);
        Image image = trail.GetComponent<Image>();
        if (image == null)
            return;
        Color color = image.color;
        float fade = (1f - u) * alpha;
        if (fade < 0f)
            fade = 0f;
        color.a = fade;
        image.color = color;
    }

    static Vector2 GrainPoint(Dot dot, float u)
    {
        if (u < 0f)
            u = 0f;
        if (u > 1f)
            u = 1f;
        float eased = Mathf.SmoothStep(0f, 1f, u);
        float sway = Mathf.Sin(dot.Phase + u * 6.2832f) * (1f - u) * 12f;
        float x = Mathf.Lerp(dot.FromX, dot.ToX, eased) + sway;
        float y = Mathf.Lerp(dot.FromY, dot.ToY, eased);
        return new Vector2(x, y);
    }

    void CommitGrain(Dot dot)
    {
        if (dot.Landed)
            return;
        dot.Landed = true;
        _landedCount++;
        if (_canvasPixels != null && dot.Index >= 0 && dot.Index < _canvasPixels.Length)
        {
            _canvasPixels[dot.Index] = _pixels[dot.Index];
            _canvasDirty = true;
        }
        if (dot.Object != null)
        {
            Destroy(dot.Object);
            dot.Object = null;
        }
    }

    void FlushCanvas()
    {
        if (!_canvasDirty || _built == null || _canvasPixels == null)
            return;
        _built.SetPixels32(_canvasPixels);
        _built.Apply(false);
        _canvasDirty = false;
    }

    IEnumerator Hold(int generation, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (!Alive(generation))
                yield break;
            if (_skip)
            {
                ShowCompleted();
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator Sparkle(int generation)
    {
        Color neon = NeonColor();
        Vector2 origin = new Vector2(0f, _bodyBottom + BodySize * 0.5f);
        int count = 22;
        for (int i = 0; i < count; i++)
        {
            float ang = i / (float)count * 6.2832f + UnityEngine.Random.Range(-0.12f, 0.12f);
            float dist = UnityEngine.Random.Range(90f, 230f);
            bool core = i % 2 == 0;
            float size = core ? UnityEngine.Random.Range(16f, 28f) : UnityEngine.Random.Range(28f, 52f);
            Color color = core ? Color.white : neon;
            SpawnSpark(origin, ang, dist, size, color);
        }

        float t = 0f;
        while (t < SparkSeconds)
        {
            if (!Alive(generation))
                yield break;
            if (_skip)
            {
                ShowCompleted();
                yield break;
            }
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / SparkSeconds);
            float fade = Mathf.Sin(Mathf.Clamp01(u * 1.15f) * Mathf.PI);
            for (int i = 0; i < _sparks.Count; i++)
            {
                Spark spark = _sparks[i];
                if (spark.Rect == null)
                    continue;
                float travel = Mathf.SmoothStep(0f, 1f, u);
                spark.Rect.anchoredPosition = Vector2.Lerp(spark.From, spark.To, travel);
                float twinkle = 0.75f + 0.45f * Mathf.Sin(u * 16f + i);
                spark.Rect.localScale = Vector3.one * twinkle * (1.15f - u * 0.35f);
                if (spark.Image != null)
                {
                    Color c = spark.Image.color;
                    c.a = fade;
                    spark.Image.color = c;
                }
            }
            yield return null;
        }
        ClearSparks();
    }

    void SpawnSpark(Vector2 origin, float ang, float dist, float size, Color color)
    {
        GameObject sparkObject = new GameObject("Spark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sparkObject.transform.SetParent(_mask, false);
        RectTransform rect = sparkObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        rect.anchoredPosition = origin;
        Image image = sparkObject.GetComponent<Image>();
        image.sprite = _whiteSprite;
        image.raycastTarget = false;
        image.color = color;
        Spark spark = new Spark();
        spark.Rect = rect;
        spark.Image = image;
        spark.From = origin + dir * 18f;
        spark.To = origin + dir * dist;
        spark.Size = size;
        _sparks.Add(spark);
    }

    IEnumerator Flash(int generation)
    {
        RevealAttribute();
        Color neon = NeonColor();
        float t = 0f;
        while (t < FlashSeconds)
        {
            if (!Alive(generation))
                yield break;
            if (_skip)
            {
                ShowCompleted();
                yield break;
            }
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / FlashSeconds);
            float pulse = Mathf.Sin(u * Mathf.PI);
            if (_picture != null)
                _picture.color = Color.Lerp(Color.white, neon, pulse * 0.35f);
            if (_body != null)
                _body.localScale = Vector3.one * (1f + 0.1f * pulse);
            if (_flash != null && _flash.texture != null)
            {
                Color flash = Color.Lerp(neon, Color.white, pulse);
                flash.a = pulse;
                _flash.color = flash;
            }
            yield return null;
        }
        if (_picture != null)
            _picture.color = Color.white;
        if (_body != null)
            _body.localScale = Vector3.one;
        HideFlash();
    }

    IEnumerator Hop(int generation, float height, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (!Alive(generation))
                yield break;
            if (_skip)
            {
                ShowCompleted();
                yield break;
            }
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float y = 4f * height * u * (1f - u);
            if (_body != null)
            {
                Vector2 pos = _bodyRest;
                pos.y += y;
                _body.anchoredPosition = pos;
                float pinch = 0f;
                if (u > 0.78f)
                    pinch = Mathf.Sin((u - 0.78f) / 0.22f * Mathf.PI);
                _body.localScale = new Vector3(1f + 0.1f * pinch, 1f - 0.16f * pinch, 1f);
            }
            yield return null;
        }
        if (_body != null)
        {
            _body.anchoredPosition = _bodyRest;
            _body.localScale = Vector3.one;
        }
    }

    IEnumerator FillGauges(int generation)
    {
        float t = 0f;
        while (t < BarSeconds)
        {
            if (!Alive(generation))
                yield break;
            if (_skip)
            {
                ShowCompleted();
                yield break;
            }
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / BarSeconds);
            float eased = 1f - (1f - u) * (1f - u);
            PaintGauges(eased);
            yield return null;
        }
        PaintGauges(1f);
    }

    void ShowCompleted()
    {
        _finished = true;
        _skip = false;
        ClearDots();
        ClearSparks();
        HideFlash();
        if (_readable && _built != null && _pixels != null)
        {
            _built.SetPixels32(_pixels);
            _built.Apply(false);
            if (_picture != null)
                _picture.texture = _built;
        }
        else if (_picture != null)
        {
            _picture.texture = _source;
        }
        if (_picture != null)
            _picture.color = Color.white;
        if (_body != null)
        {
            _body.anchoredPosition = _bodyRest;
            _body.localScale = Vector3.one;
        }
        RevealAttribute();
        PaintGauges(1f);
        if (_hint != null)
            _hint.gameObject.SetActive(false);
        if (_skipButton != null)
            _skipButton.interactable = false;
        if (_backButton != null)
            _backButton.gameObject.SetActive(true);
    }

    void RevealAttribute()
    {
        if (_attribute == null || _character == null)
            return;
        _attribute.text = TypeName(_character.CharacterType) + "　　弱点　" + TypeName(_character.WeaknessType);
        _attribute.color = AttributeTextColor(_character.CharacterType);
    }

    void BindGauges()
    {
        if (_gauges == null)
            return;
        int side = _character != null && _character.Size > 0 ? _character.Size : (_width > 0 ? _width : 1);
        SizeStatCeiling ceiling = CharacterClass.CeilingForSize(side);
        StatisticsClass stats = null;
        StatisticsClass born = null;
        if (_character != null && _character.Stats != null)
        {
            stats = _character.Stats[0];
            if (_character.Stats.Length > 1)
                born = _character.Stats[1];
        }

        ulong hp = stats != null ? stats.HPMax : 0;
        ulong atk = stats != null ? stats.ATK : 0;
        ulong def = stats != null ? stats.DEF : 0;
        ulong spd = stats != null ? (ulong)stats.SPD : 0;
        _lucAmount = stats != null ? stats.LUC : 0;
        _bandRAmount = born != null ? born.RCreates : 0;
        _bandGAmount = born != null ? born.GCreates : 0;
        _bandBAmount = born != null ? born.BCreates : 0;
        ulong pixels = (ulong)side * (ulong)side;
        if (pixels < 1)
            pixels = 1;
        _bandCanvas = pixels;

        ulong[] amounts = { hp, atk, def, spd };
        ulong[] ceilings = { ceiling.HP, ceiling.ATK, ceiling.DEF, ceiling.SPD };
        for (int i = 0; i < _gauges.Length && i < amounts.Length; i++)
        {
            ulong amount = amounts[i];
            ulong max = ceilings[i];
            if (amount > max)
                max = amount;
            _gauges[i].Amount = amount;
            _gauges[i].Ceiling = max;
            if (_gauges[i].Max != null)
                _gauges[i].Max.text = UiNumber.Format(max);
        }
        if (_lucMax != null)
            _lucMax.text = UiNumber.Format(ceiling.LUC);
        if (_bandMax != null)
            _bandMax.text = UiNumber.Format(_bandCanvas);
    }

    void ResetGauges()
    {
        if (_gauges == null)
            return;
        for (int i = 0; i < _gauges.Length; i++)
        {
            if (_gauges[i].Fill != null)
                _gauges[i].Fill.fillAmount = 0f;
            if (_gauges[i].Value != null)
                _gauges[i].Value.text = "";
        }
        PaintPips(0);
        if (_lucValue != null)
            _lucValue.text = "";
        Span(_bandR, 0f, 0f);
        Span(_bandG, 0f, 0f);
        Span(_bandB, 0f, 0f);
        if (_bandValue != null)
            _bandValue.text = "";
    }

    void PaintGauges(float t)
    {
        if (_gauges == null)
            return;
        if (t < 0f)
            t = 0f;
        if (t > 1f)
            t = 1f;
        for (int i = 0; i < _gauges.Length; i++)
        {
            Gauge gauge = _gauges[i];
            if (gauge.Fill != null)
            {
                float ratio = Ratio(gauge.Amount, gauge.Ceiling);
                if (gauge.Sqrt)
                    ratio = Mathf.Sqrt(ratio);
                gauge.Fill.fillAmount = ratio * t;
            }
            if (gauge.Value == null)
                continue;
            gauge.Value.text = UiNumber.Format(t >= 1f ? gauge.Amount : ShownAmount(gauge.Amount, t));
        }
        int lucShown = (int)_lucAmount;
        if (t < 1f)
            lucShown = (int)ShownAmount(_lucAmount, t);
        PaintPips(lucShown);
        if (_lucValue != null)
            _lucValue.text = UiNumber.Format((ulong)lucShown);
        ulong shownR = t >= 1f ? _bandRAmount : ShownAmount(_bandRAmount, t);
        ulong shownG = t >= 1f ? _bandGAmount : ShownAmount(_bandGAmount, t);
        ulong shownB = t >= 1f ? _bandBAmount : ShownAmount(_bandBAmount, t);
        PaintBand(shownR, shownG, shownB);
        if (_bandValue != null)
            _bandValue.text = UiNumber.Format(shownR) + "  " + UiNumber.Format(shownG) + "  " + UiNumber.Format(shownB);
    }

    void PaintPips(int lit)
    {
        if (_lucPips == null)
            return;
        if (lit < 0)
            lit = 0;
        if (lit > _lucPips.Length)
            lit = _lucPips.Length;
        Color on = new Color(0.72f, 0.58f, 0.16f, 1f);
        Color off = new Color(0.82f, 0.82f, 0.86f, 1f);
        for (int i = 0; i < _lucPips.Length; i++)
        {
            if (_lucPips[i] != null)
                _lucPips[i].color = i < lit ? on : off;
        }
    }

    void PaintBand(ulong red, ulong green, ulong blue)
    {
        float canvas = _bandCanvas > 0 ? _bandCanvas : 1f;
        float r = (float)red / canvas;
        float g = (float)green / canvas;
        float b = (float)blue / canvas;
        float sum = r + g + b;
        if (sum > 1f)
        {
            r /= sum;
            g /= sum;
            b /= sum;
        }
        Span(_bandR, 0f, r);
        Span(_bandG, r, r + g);
        Span(_bandB, r + g, r + g + b);
    }

    static void Span(Image image, float from, float to)
    {
        if (image == null)
            return;
        if (from < 0f)
            from = 0f;
        if (to < from)
            to = from;
        if (to > 1f)
            to = 1f;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(from, 0f);
        rect.anchorMax = new Vector2(to, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static float Ratio(ulong amount, ulong ceiling)
    {
        if (ceiling == 0 || amount == 0)
            return 0f;
        if (amount >= ceiling)
            return 1f;
        return (float)amount / ceiling;
    }

    static ulong ShownAmount(ulong amount, float t)
    {
        double shown = amount * (double)t;
        if (shown <= 0.0)
            return 0;
        ulong rounded = (ulong)Math.Round(shown);
        if (rounded > amount)
            return amount;
        return rounded;
    }

    void ClearDots()
    {
        for (int i = 0; i < _dots.Count; i++)
            DestroyDot(_dots[i]);
        _dots.Clear();
    }

    static void DestroyDot(Dot dot)
    {
        if (dot == null)
            return;
        if (dot.Object != null)
            Destroy(dot.Object);
    }

    void ReleaseBuilt()
    {
        if (_picture != null && _built != null && _picture.texture == _built)
            _picture.texture = null;
        if (_built != null)
        {
            Destroy(_built);
            _built = null;
        }
        ReleaseSilhouette();
    }

    void BuildSilhouette()
    {
        ReleaseSilhouette();
        if (_flash == null || _pixels == null || _width < 1 || _height < 1)
            return;
        if (_pixels.Length != _width * _height)
            return;
        _silhouette = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        _silhouette.filterMode = FilterMode.Point;
        _silhouette.wrapMode = TextureWrapMode.Clamp;
        Color32[] shape = new Color32[_pixels.Length];
        for (int i = 0; i < _pixels.Length; i++)
            shape[i] = new Color32(255, 255, 255, _pixels[i].a);
        _silhouette.SetPixels32(shape);
        _silhouette.Apply(false);
        _flash.texture = _silhouette;
        HideFlash();
    }

    void ReleaseSilhouette()
    {
        if (_flash != null && _silhouette != null && _flash.texture == _silhouette)
            _flash.texture = null;
        if (_silhouette != null)
        {
            Destroy(_silhouette);
            _silhouette = null;
        }
    }

    void BuildUi()
    {
        Image backdrop = gameObject.GetComponent<Image>();
        if (backdrop == null)
            backdrop = gameObject.AddComponent<Image>();
        backdrop.color = Color.black;
        backdrop.raycastTarget = true;

        GameObject skipObject = new GameObject("ButtonRebirthSkip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        skipObject.transform.SetParent(transform, false);
        Stretch(skipObject.GetComponent<RectTransform>());
        Image skipImage = skipObject.GetComponent<Image>();
        skipImage.color = new Color(1f, 1f, 1f, 0.01f);
        skipImage.raycastTarget = true;
        _skipButton = skipObject.GetComponent<Button>();
        _skipButton.targetGraphic = skipImage;
        _skipButton.transition = Selectable.Transition.None;
        _skipButton.onClick.AddListener(PushSkip);

        _name = CreateText("TextRebirthName", transform, 36, TextAnchor.MiddleCenter);
        Place(_name.rectTransform, 0f, 624f, 960f, 56f);
        _name.supportRichText = false;

        GameObject maskObject = new GameObject("RebirthMask", typeof(RectTransform), typeof(RectMask2D));
        maskObject.transform.SetParent(transform, false);
        _mask = maskObject.GetComponent<RectTransform>();
        Place(_mask, 0f, MaskCenterY, MaskWidth, MaskHeight);
        _spawnY = MaskHeight * 0.5f - 8f;
        _bodyBottom = -MaskHeight * 0.5f + BodyBottomInset;

        GameObject bodyObject = new GameObject("RebirthBody", typeof(RectTransform));
        bodyObject.transform.SetParent(_mask, false);
        _body = bodyObject.GetComponent<RectTransform>();
        _body.anchorMin = new Vector2(0.5f, 0.5f);
        _body.anchorMax = new Vector2(0.5f, 0.5f);
        _body.pivot = new Vector2(0.5f, 0f);
        _body.sizeDelta = new Vector2(BodySize, BodySize);
        _bodyRest = new Vector2(0f, _bodyBottom);
        _body.anchoredPosition = _bodyRest;

        GameObject pictureObject = new GameObject("ImageRebirth", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        pictureObject.transform.SetParent(_body, false);
        Stretch(pictureObject.GetComponent<RectTransform>());
        _picture = pictureObject.GetComponent<RawImage>();
        _picture.raycastTarget = false;
        _picture.color = Color.white;

        GameObject flashObject = new GameObject("ImageRebirthFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        flashObject.transform.SetParent(_body, false);
        Stretch(flashObject.GetComponent<RectTransform>());
        _flash = flashObject.GetComponent<RawImage>();
        _flash.color = new Color(1f, 1f, 1f, 0f);
        _flash.raycastTarget = false;

        _attribute = CreateText("TextRebirthAttribute", transform, 28, TextAnchor.MiddleCenter);
        Place(_attribute.rectTransform, 0f, 56f, 960f, 40f);

        Text maxHeader = CreateText("TextRebirthMaxHeader", transform, 18, TextAnchor.MiddleLeft);
        Place(maxHeader.rectTransform, 385f, 18f, 130f, 24f);
        maxHeader.text = "最大";
        maxHeader.color = new Color(0.62f, 0.62f, 0.66f, 1f);

        BuildGauges();

        _hint = CreateText("TextRebirthHint", transform, 22, TextAnchor.MiddleCenter);
        Place(_hint.rectTransform, 0f, -400f, 700f, 40f);
        _hint.text = "タップで見るのを飛ばす";
        _hint.color = new Color(0.75f, 0.75f, 0.78f, 1f);

        GameObject backObject = new GameObject("ButtonRebirthBack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        backObject.transform.SetParent(transform, false);
        Place(backObject.GetComponent<RectTransform>(), 0f, -400f, 360f, 84f);
        Image backImage = backObject.GetComponent<Image>();
        backImage.color = Color.white;
        _backButton = backObject.GetComponent<Button>();
        _backButton.targetGraphic = backImage;
        _backButton.onClick.AddListener(PushBack);
        Text backLabel = CreateText("Text", backObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(backLabel.rectTransform);
        backLabel.text = "一覧へ戻る";
        if (_production != null)
            _production.StyleRepresentativeButton(_backButton, Color.white, true);
        backObject.SetActive(false);
    }

    void BuildGauges()
    {
        _gauges = new Gauge[4];
        _gauges[0] = BuildBar("HP", new Color(0.76f, 0.28f, 0.28f, 1f), -28f, true);
        _gauges[1] = BuildBar("ATK", new Color(0.86f, 0.46f, 0.18f, 1f), -76f, true);
        _gauges[2] = BuildBar("DEF", new Color(0.24f, 0.46f, 0.78f, 1f), -124f, false);
        _gauges[3] = BuildBar("SPD", new Color(0.18f, 0.62f, 0.42f, 1f), -172f, false);
        BuildLuc(-224f);
        BuildBand(-276f);
    }

    Gauge BuildBar(string label, Color color, float y, bool sqrt)
    {
        GameObject row = new GameObject("Gauge" + label, typeof(RectTransform));
        row.transform.SetParent(transform, false);
        Place(row.GetComponent<RectTransform>(), 0f, y, 940f, 40f);

        Text name = CreateText("Label", row.transform, 22, TextAnchor.MiddleLeft);
        Place(name.rectTransform, -410f, 0f, 80f, 36f);
        name.text = label;

        GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackObject.transform.SetParent(row.transform, false);
        Place(trackObject.GetComponent<RectTransform>(), -80f, 0f, 420f, 16f);
        Image track = trackObject.GetComponent<Image>();
        track.sprite = _whiteSprite;
        track.color = new Color(0.82f, 0.82f, 0.86f, 1f);
        track.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(trackObject.transform, false);
        Stretch(fillObject.GetComponent<RectTransform>());
        Image fill = fillObject.GetComponent<Image>();
        fill.sprite = _whiteSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.color = color;
        fill.raycastTarget = false;

        Text value = CreateText("Value", row.transform, 22, TextAnchor.MiddleRight);
        Place(value.rectTransform, 230f, 0f, 120f, 36f);
        value.text = "";

        Text max = CreateText("Max", row.transform, 20, TextAnchor.MiddleLeft);
        Place(max.rectTransform, 385f, 0f, 130f, 36f);
        max.color = new Color(0.62f, 0.62f, 0.66f, 1f);

        Gauge gauge = new Gauge();
        gauge.Fill = fill;
        gauge.Value = value;
        gauge.Max = max;
        gauge.Sqrt = sqrt;
        return gauge;
    }

    void BuildLuc(float y)
    {
        GameObject row = new GameObject("GaugeLUC", typeof(RectTransform));
        row.transform.SetParent(transform, false);
        Place(row.GetComponent<RectTransform>(), 0f, y, 940f, 40f);

        Text name = CreateText("Label", row.transform, 22, TextAnchor.MiddleLeft);
        Place(name.rectTransform, -410f, 0f, 80f, 36f);
        name.text = "LUC";

        int count = (int)CharacterClass.LucMax;
        if (count < 1)
            count = 1;
        _lucPips = new Image[count];
        float pip = 26f;
        float gap = 12f;
        float total = count * pip + (count - 1) * gap;
        float left = -290f + (420f - total) * 0.5f + pip * 0.5f;
        for (int i = 0; i < count; i++)
        {
            GameObject pipObject = new GameObject("Pip" + i.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pipObject.transform.SetParent(row.transform, false);
            Place(pipObject.GetComponent<RectTransform>(), left + i * (pip + gap), 0f, pip, pip);
            Image image = pipObject.GetComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = new Color(0.82f, 0.82f, 0.86f, 1f);
            image.raycastTarget = false;
            _lucPips[i] = image;
        }

        _lucValue = CreateText("Value", row.transform, 22, TextAnchor.MiddleRight);
        Place(_lucValue.rectTransform, 230f, 0f, 120f, 36f);
        _lucValue.text = "";

        _lucMax = CreateText("Max", row.transform, 20, TextAnchor.MiddleLeft);
        Place(_lucMax.rectTransform, 385f, 0f, 130f, 36f);
        _lucMax.color = new Color(0.62f, 0.62f, 0.66f, 1f);
    }

    void BuildBand(float y)
    {
        GameObject row = new GameObject("GaugeRGB", typeof(RectTransform));
        row.transform.SetParent(transform, false);
        Place(row.GetComponent<RectTransform>(), 0f, y, 940f, 40f);

        Text name = CreateText("Label", row.transform, 22, TextAnchor.MiddleLeft);
        Place(name.rectTransform, -410f, 0f, 80f, 36f);
        name.text = "RGB";

        GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackObject.transform.SetParent(row.transform, false);
        Place(trackObject.GetComponent<RectTransform>(), -80f, 0f, 420f, 16f);
        Image track = trackObject.GetComponent<Image>();
        track.sprite = _whiteSprite;
        track.color = new Color(0.82f, 0.82f, 0.86f, 1f);
        track.raycastTarget = false;

        _bandR = CreateBand(trackObject.transform, new Color(0.82f, 0.22f, 0.22f, 1f));
        _bandG = CreateBand(trackObject.transform, new Color(0.20f, 0.62f, 0.28f, 1f));
        _bandB = CreateBand(trackObject.transform, new Color(0.20f, 0.38f, 0.82f, 1f));

        _bandValue = CreateText("Value", row.transform, 18, TextAnchor.MiddleRight);
        Place(_bandValue.rectTransform, 220f, 0f, 180f, 36f);
        _bandValue.text = "";

        _bandMax = CreateText("Max", row.transform, 20, TextAnchor.MiddleLeft);
        Place(_bandMax.rectTransform, 385f, 0f, 130f, 36f);
        _bandMax.color = new Color(0.62f, 0.62f, 0.66f, 1f);
    }

    Image CreateBand(Transform parent, Color color)
    {
        GameObject bandObject = new GameObject("Band", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bandObject.transform.SetParent(parent, false);
        Image image = bandObject.GetComponent<Image>();
        image.sprite = _whiteSprite;
        image.color = color;
        image.raycastTarget = false;
        Span(image, 0f, 0f);
        return image;
    }

    Color NeonColor()
    {
        CharacterType type = _character != null ? _character.CharacterType : CharacterType.None;
        switch (type)
        {
            case CharacterType.Fire: return new Color(1f, 0.18f, 0.05f, 1f);
            case CharacterType.Grass: return new Color(0.15f, 1f, 0.28f, 1f);
            case CharacterType.Water: return new Color(0.15f, 0.55f, 1f, 1f);
            case CharacterType.Light: return new Color(1f, 0.92f, 0.35f, 1f);
            case CharacterType.Dark: return new Color(0.36f, 0.04f, 0.52f, 1f);
            default: return Color.white;
        }
    }

    void HideFlash()
    {
        if (_flash == null)
            return;
        Color color = _flash.color;
        color.a = 0f;
        _flash.color = color;
    }

    void ClearSparks()
    {
        for (int i = 0; i < _sparks.Count; i++)
        {
            if (_sparks[i] != null && _sparks[i].Rect != null)
                Destroy(_sparks[i].Rect.gameObject);
        }
        _sparks.Clear();
    }

    static Color AttributeTextColor(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Fire: return new Color(1f, 0.42f, 0.22f, 1f);
            case CharacterType.Grass: return new Color(0.4f, 0.95f, 0.48f, 1f);
            case CharacterType.Water: return new Color(0.4f, 0.72f, 1f, 1f);
            case CharacterType.Light: return new Color(1f, 0.92f, 0.4f, 1f);
            case CharacterType.Dark: return new Color(0.78f, 0.52f, 1f, 1f);
            default: return new Color(0.86f, 0.86f, 0.88f, 1f);
        }
    }

    static string TypeName(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Fire: return "火";
            case CharacterType.Grass: return "草";
            case CharacterType.Water: return "水";
            case CharacterType.Light: return "光";
            case CharacterType.Dark: return "闇";
            default: return "なし";
        }
    }

    Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

}
