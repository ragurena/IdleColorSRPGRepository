using UnityEngine;
using UnityEngine.UI;

// 合成と図鑑で共通のステータスグラフ。
// 棒の暗い部分が初期、色の部分が今。右端は、いまのレベル上限まで育て、そこへ合成上限を足した値。装備は含めない。
// 合成画面は「今→次の1回」のみ（上限列なし）。図鑑は現在値と上限。初期は棒の暗い部分だけ。
// SPD と LUC は合成で増えない。LUC はレベルでも増えないので、上限は 10。
public sealed class CharacterStatGraph
{
    Font _font;
    Color _labelColor;
    Color _capColor;
    Color _trackColor;
    float _width;
    float _labelX;
    float _labelW;
    float _trackX;
    float _trackW;
    float _valueX;
    float _valueW;
    float _capX;
    float _capW;
    float _bar;
    float _rowH;
    float _textH;
    float _firstRowY;
    float _rowPitch;
    float _headGap;
    float _bornX;
    float _bornW;
    bool _showBorn;
    bool _showCap;
    int _labelSize;
    int _valueSize;
    int _capSize;
    int _headSize;
    int _rgbSize;
    bool _stretchLuc;
    Sprite _whiteSprite;
    StatGauge[] _gauges;
    Image[] _lucPips;
    Text _lucValue;
    Text _lucCap;
    Image _nextBandR;
    Image _nextBandG;
    Image _nextBandB;
    Image _bandR;
    Image _bandG;
    Image _bandB;
    Image _baseBandR;
    Image _baseBandG;
    Image _baseBandB;
    Text _bandBorn;
    Text _bandValue;
    Text _bandCap;
    Text _lucBorn;
    Text _capHead;

    class StatGauge
    {
        public Image NextFill;
        public Image Fill;
        public Image BaseFill;
        public Text Born;
        public Text Value;
        public Text Cap;
    }

    public GameObject Root { get; private set; }

    public static CharacterStatGraph Create(
        Transform parent,
        string name,
        Font font,
        float x,
        float y,
        float width,
        float height,
        bool stretchLuc,
        Color labelColor,
        Color capColor,
        Color trackColor,
        int labelSize = 16,
        int valueSize = 13,
        int capSize = 13,
        int headSize = 12,
        int rgbSize = 12,
        float bar = 12f,
        float firstRowY = 92f,
        float rowPitch = 40f,
        float headGap = 36f,
        bool showBorn = false,
        bool showCap = true)
    {
        CharacterStatGraph graph = new CharacterStatGraph();
        graph._font = font;
        graph._width = width;
        graph._stretchLuc = stretchLuc;
        graph._labelColor = labelColor;
        graph._capColor = capColor;
        graph._trackColor = trackColor;
        graph._labelSize = labelSize;
        graph._valueSize = valueSize;
        graph._capSize = capSize;
        graph._headSize = headSize;
        graph._rgbSize = rgbSize;
        graph._bar = bar;
        float text = labelSize > valueSize ? labelSize : valueSize;
        if (showCap && capSize > text)
            text = capSize;
        graph._textH = text + 8f;
        if (graph._textH < 32f)
            graph._textH = 32f;
        graph._rowH = bar + 10f;
        if (graph._rowH < graph._textH + 4f)
            graph._rowH = graph._textH + 4f;
        if (graph._rowH < 36f)
            graph._rowH = 36f;
        graph._firstRowY = firstRowY;
        graph._rowPitch = rowPitch;
        graph._headGap = headGap;
        graph._showBorn = showBorn;
        graph._showCap = showCap;
        graph._labelW = 44f * labelSize / 16f;
        graph._valueW = 104f * valueSize / 13f;
        graph._capW = showCap ? 76f * capSize / 13f : 0f;
        graph.Measure();
        graph.Build(parent, name, x, y, height);
        return graph;
    }

    public void SetShown(bool shown)
    {
        if (Root != null)
            Root.SetActive(shown);
    }

    public void Paint(CharacterClass character, BattleBalanceConfig config)
    {
        if (_gauges == null)
            return;
        if (character == null || character.Stats == null)
        {
            Clear();
            return;
        }

        StatisticsClass now = character.Stats[0];
        StatisticsClass born = character.Stats[1];
        StatisticsClass level = character.Stats.Length > 2 ? character.Stats[2] : null;
        long fusionCount = ToLong(character.FusionCount);
        ulong capHp;
        ulong capAtk;
        ulong capDef;
        ulong capSpd;
        ulong capR;
        ulong capG;
        ulong capB;
        LevelCapStats(born, config, out capHp, out capAtk, out capDef, out capSpd, out capR, out capG, out capB);
        PaintCapHead(config);
        ulong nextHp = NextFused(now.HPMax, born.HPMax, level != null ? level.HPMax : 0, fusionCount);
        ulong nextAtk = NextFused(now.ATK, born.ATK, level != null ? level.ATK : 0, fusionCount);
        ulong nextDef = NextFused(now.DEF, born.DEF, level != null ? level.DEF : 0, fusionCount);
        ulong[] amounts = { now.HPMax, now.ATK, now.DEF, now.SPD };
        ulong[] nexts = { nextHp, nextAtk, nextDef, now.SPD };
        ulong[] bases = { born.HPMax, born.ATK, born.DEF, born.SPD };
        ulong[] ceilings = { capHp, capAtk, capDef, capSpd };
        for (int i = 0; i < _gauges.Length && i < amounts.Length; i++)
        {
            ulong amount = amounts[i];
            ulong baseAmount = bases[i];
            float ratio = StatRatio(amount, ceilings[i]);
            float nextRatio = StatRatio(nexts[i], ceilings[i]);
            float baseRatio = StatRatio(baseAmount, ceilings[i]);
            if (nextRatio < ratio || _showBorn)
                nextRatio = ratio;
            if (baseRatio > ratio)
                baseRatio = ratio;
            if (_gauges[i].NextFill != null)
                _gauges[i].NextFill.fillAmount = nextRatio;
            if (_gauges[i].Fill != null)
                _gauges[i].Fill.fillAmount = ratio;
            if (_gauges[i].BaseFill != null)
                _gauges[i].BaseFill.fillAmount = baseRatio;
            if (_gauges[i].Value != null)
                _gauges[i].Value.text = _showBorn
                    ? UiNumber.Format(amount)
                    : UiNumber.Format(amount) + "→" + UiNumber.Format(nexts[i]);
            if (_gauges[i].Born != null)
                _gauges[i].Born.text = UiNumber.Format(baseAmount);
            if (_gauges[i].Cap != null)
                _gauges[i].Cap.text = UiNumber.Format(ceilings[i]);
        }
        ulong bandCanvas = capR + capG + capB;
        if (bandCanvas < 1)
            bandCanvas = 1;
        ulong nextR = NextFused(now.RCreates, born.RCreates, level != null ? level.RCreates : 0, fusionCount);
        ulong nextG = NextFused(now.GCreates, born.GCreates, level != null ? level.GCreates : 0, fusionCount);
        ulong nextB = NextFused(now.BCreates, born.BCreates, level != null ? level.BCreates : 0, fusionCount);
        if (_showBorn)
        {
            nextR = now.RCreates;
            nextG = now.GCreates;
            nextB = now.BCreates;
        }
        PaintLuc(born.LUC, now.LUC);
        PaintBand(born.RCreates, born.GCreates, born.BCreates, now.RCreates, now.GCreates, now.BCreates, nextR, nextG, nextB, bandCanvas);
        if (_bandBorn != null)
            _bandBorn.text = RgbTrio(born.RCreates, born.GCreates, born.BCreates);
        if (_bandValue != null)
            _bandValue.text = _showBorn
                ? RgbLines(now.RCreates, now.GCreates, now.BCreates)
                : RgbTrio(now.RCreates, now.GCreates, now.BCreates) + "\n→" + RgbTrio(nextR, nextG, nextB);
        if (_bandCap != null)
            _bandCap.text = _showBorn
                ? RgbLines(capR, capG, capB)
                : RgbTrio(capR, capG, capB);
    }

    public void Clear()
    {
        if (_gauges != null)
        {
            for (int i = 0; i < _gauges.Length; i++)
            {
                if (_gauges[i].NextFill != null)
                    _gauges[i].NextFill.fillAmount = 0f;
                if (_gauges[i].Fill != null)
                    _gauges[i].Fill.fillAmount = 0f;
                if (_gauges[i].BaseFill != null)
                    _gauges[i].BaseFill.fillAmount = 0f;
                if (_gauges[i].Born != null)
                    _gauges[i].Born.text = "";
                if (_gauges[i].Value != null)
                    _gauges[i].Value.text = "";
                if (_gauges[i].Cap != null)
                    _gauges[i].Cap.text = "";
            }
        }
        PaintLuc(0, 0);
        if (_lucBorn != null)
            _lucBorn.text = "";
        if (_lucValue != null)
            _lucValue.text = "";
        if (_lucCap != null)
            _lucCap.text = "";
        SpanBand(_nextBandR, 0f, 0f);
        SpanBand(_nextBandG, 0f, 0f);
        SpanBand(_nextBandB, 0f, 0f);
        SpanBand(_bandR, 0f, 0f);
        SpanBand(_bandG, 0f, 0f);
        SpanBand(_bandB, 0f, 0f);
        SpanBand(_baseBandR, 0f, 0f);
        SpanBand(_baseBandG, 0f, 0f);
        SpanBand(_baseBandB, 0f, 0f);
        if (_bandBorn != null)
            _bandBorn.text = "";
        if (_bandValue != null)
            _bandValue.text = "";
        if (_bandCap != null)
            _bandCap.text = "";
        if (_capHead != null)
            _capHead.text = "上限\nLv0\n合成0";
    }

    void PaintCapHead(BattleBalanceConfig config)
    {
        if (_capHead == null)
            return;
        long maxLevel = BattleBalanceConfig.InitialLevelCap;
        if (config != null && config.MaxLevel > maxLevel)
            maxLevel = config.MaxLevel;
        long fusionCap = FusionBonus.MaxCount;
        if (fusionCap < FusionBonus.InitialCap)
            fusionCap = FusionBonus.InitialCap;
        _capHead.text = "上限\nLv" + UiNumber.Format(maxLevel) + "\n合成" + UiNumber.Format(fusionCap);
    }

    void Measure()
    {
        float half = _width * 0.5f;
        float labelHalf = _labelW * 0.5f;
        _labelX = -half + 2f + labelHalf;
        float trackLeft = _labelX + labelHalf + 16f;
        float trackRight;
        if (_showBorn)
        {
            float col = 110f;
            float colHalf = col * 0.5f;
            _valueW = col;
            _capW = col;
            _capX = half - 2f - colHalf;
            _valueX = _capX - col - 6f;
            trackRight = _valueX - colHalf - 6f;
        }
        else if (!_showCap)
        {
            // 上限列なし。数値を右端に寄せて、棒を長くする。
            if (_valueW < 128f)
                _valueW = 128f;
            float valueHalf = _valueW * 0.5f;
            _valueX = half - 6f - valueHalf;
            _capX = _valueX;
            trackRight = _valueX - valueHalf - 14f;
        }
        else
        {
            float valueHalf = _valueW * 0.5f;
            float capHalf = _capW * 0.5f;
            _capX = half - 6f - capHalf;
            _valueX = _capX - capHalf - 14f - valueHalf;
            trackRight = _valueX - valueHalf - 18f;
        }
        _trackW = trackRight - trackLeft;
        if (_trackW < 8f)
            _trackW = 8f;
        _trackX = (trackLeft + trackRight) * 0.5f;
    }

    void Build(Transform parent, string name, float x, float y, float height)
    {
        EnsureWhiteSprite();
        Root = new GameObject(name, typeof(RectTransform));
        Root.transform.SetParent(parent, false);
        Place(Root.GetComponent<RectTransform>(), x, y, _width, height);

        float hp = _firstRowY;
        float step = _rowPitch;
        if (_showCap)
        {
            float headY = hp + _headGap;
            float headH = _headSize <= 12 ? 18f : _headSize + 6f;
            // 「上限」の上に、その列が想定するレベル上限と合成上限を出す。
            float capHeadH = headH * 3.35f;
            _capHead = CreateText("CapHead", Root.transform, _headSize, TextAnchor.MiddleRight);
            Place(_capHead.rectTransform, _capX, headY + (capHeadH - headH) * 0.5f, _capW + 24f, capHeadH);
            _capHead.supportRichText = true;
            _capHead.lineSpacing = 0.92f;
            _capHead.horizontalOverflow = HorizontalWrapMode.Overflow;
            _capHead.verticalOverflow = VerticalWrapMode.Overflow;
            _capHead.color = _capColor;
            PaintCapHead(null);
        }

        _gauges = new StatGauge[4];
        _gauges[0] = BuildGauge("HP", new Color(0.76f, 0.28f, 0.28f, 1f), hp);
        _gauges[1] = BuildGauge("ATK", new Color(0.86f, 0.46f, 0.18f, 1f), hp - step);
        _gauges[2] = BuildGauge("DEF", new Color(0.24f, 0.46f, 0.78f, 1f), hp - step * 2f);
        _gauges[3] = BuildGauge("SPD", new Color(0.18f, 0.62f, 0.42f, 1f), hp - step * 3f);
        BuildLucRow(hp - step * 4f);
        BuildRgbRow(hp - step * 5f);
    }

    StatGauge BuildGauge(string label, Color color, float y)
    {
        GameObject row = new GameObject("Gauge" + label, typeof(RectTransform));
        row.transform.SetParent(Root.transform, false);
        Place(row.GetComponent<RectTransform>(), 0f, y, _width, _rowH);

        Text name = CreateText("Label", row.transform, _labelSize, TextAnchor.MiddleLeft);
        Place(name.rectTransform, _labelX, 0f, _labelW, _textH);
        name.text = label;

        Image track = CreateTrack(row.transform, _trackX, _trackW, _bar);
        Image nextFill = CreateFill(track.transform, Brighten(color));
        Image fill = CreateFill(track.transform, color);
        Image baseFill = CreateFill(track.transform, Darken(color));

        Text born = CreateBornText();
        Text value = CreateText("Value", row.transform, _valueSize, TextAnchor.MiddleRight);
        Text cap = _showCap ? CreateText("Cap", row.transform, _capSize, TextAnchor.MiddleRight) : null;
        PlaceStatNumbers(born, value, cap);

        return new StatGauge { NextFill = nextFill, Fill = fill, BaseFill = baseFill, Born = born, Value = value, Cap = cap };
    }

    void BuildLucRow(float y)
    {
        GameObject row = new GameObject("GaugeLUC", typeof(RectTransform));
        row.transform.SetParent(Root.transform, false);
        Place(row.GetComponent<RectTransform>(), 0f, y, _width, _rowH);

        Text name = CreateText("Label", row.transform, _labelSize, TextAnchor.MiddleLeft);
        Place(name.rectTransform, _labelX, 0f, _labelW, _textH);
        name.text = "LUC";

        int count = (int)CharacterClass.LucMax;
        if (count < 1)
            count = 1;
        _lucPips = new Image[count];
        float pipW;
        float pipH;
        float gap;
        float left;
        if (_stretchLuc)
        {
            gap = 2f;
            pipH = _bar;
            pipW = (_trackW - (count - 1) * gap) / count;
            if (pipW < 4f)
                pipW = 4f;
            left = _trackX - _trackW * 0.5f + pipW * 0.5f;
        }
        else
        {
            pipW = 11f;
            pipH = 11f;
            gap = 2f;
            float total = count * pipW + (count - 1) * gap;
            left = _trackX - total * 0.5f + pipW * 0.5f;
        }
        for (int i = 0; i < count; i++)
        {
            GameObject pipObject = new GameObject("Pip" + i.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pipObject.transform.SetParent(row.transform, false);
            Place(pipObject.GetComponent<RectTransform>(), left + i * (pipW + gap), 0f, pipW, pipH);
            Image image = pipObject.GetComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = _trackColor;
            image.raycastTarget = false;
            _lucPips[i] = image;
        }

        _lucBorn = CreateBornText();
        _lucValue = CreateText("Value", row.transform, _valueSize, TextAnchor.MiddleRight);
        _lucCap = _showCap ? CreateText("Cap", row.transform, _capSize, TextAnchor.MiddleRight) : null;
        PlaceStatNumbers(_lucBorn, _lucValue, _lucCap);
    }

    void BuildRgbRow(float y)
    {
        GameObject row = new GameObject("GaugeRGB", typeof(RectTransform));
        row.transform.SetParent(Root.transform, false);
        Place(row.GetComponent<RectTransform>(), 0f, y, _width, _rowH);

        Text name = CreateText("Label", row.transform, _labelSize, TextAnchor.MiddleLeft);
        Place(name.rectTransform, _labelX, 0f, _labelW, _textH);
        name.text = "RGB";

        Image track = CreateTrack(row.transform, _trackX, _trackW, _bar);
        Color red = new Color(0.95f, 0.32f, 0.28f, 1f);
        Color green = new Color(0.28f, 0.82f, 0.36f, 1f);
        Color blue = new Color(0.32f, 0.55f, 0.95f, 1f);
        _nextBandR = CreateBand(track.transform, Brighten(red));
        _nextBandG = CreateBand(track.transform, Brighten(green));
        _nextBandB = CreateBand(track.transform, Brighten(blue));
        _bandR = CreateBand(track.transform, red);
        _bandG = CreateBand(track.transform, green);
        _bandB = CreateBand(track.transform, blue);
        _baseBandR = CreateBand(track.transform, new Color(0.42f, 0.10f, 0.10f, 1f));
        _baseBandG = CreateBand(track.transform, new Color(0.08f, 0.32f, 0.12f, 1f));
        _baseBandB = CreateBand(track.transform, new Color(0.08f, 0.16f, 0.42f, 1f));

        _bandBorn = CreateBornText();
        if (_bandBorn != null)
            _bandBorn.fontSize = _rgbSize;
        _bandValue = CreateText("Value", row.transform, _rgbSize, _showBorn ? TextAnchor.MiddleRight : TextAnchor.UpperRight);
        _bandCap = _showCap ? CreateText("Cap", row.transform, _rgbSize, TextAnchor.MiddleRight) : null;
        PlaceStatNumbers(_bandBorn, _bandValue, _bandCap);
        _bandValue.fontSize = _rgbSize;
        if (_bandCap != null)
            _bandCap.fontSize = _rgbSize;
        if (_showBorn)
        {
            float rgbH = _rgbSize * 3f + 8f;
            _bandValue.alignment = TextAnchor.UpperRight;
            _bandValue.verticalOverflow = VerticalWrapMode.Overflow;
            Place(_bandValue.rectTransform, _valueX, -rgbH * 0.25f, _valueW, rgbH);
            if (_bandCap != null)
            {
                _bandCap.alignment = TextAnchor.UpperRight;
                _bandCap.verticalOverflow = VerticalWrapMode.Overflow;
                Place(_bandCap.rectTransform, _capX, -rgbH * 0.25f, _capW, rgbH);
            }
        }
        else
        {
            _bandValue.alignment = TextAnchor.UpperRight;
            _bandValue.verticalOverflow = VerticalWrapMode.Overflow;
            if (_rgbSize <= 12)
                Place(_bandValue.rectTransform, _valueX, 8f, _valueW, 16f);
            else
                Place(_bandValue.rectTransform, _valueX, 2f, _valueW, _rgbSize * 2f + 4f);
        }
    }

    static Text CreateBornText()
    {
        return null;
    }

    void PlaceStatNumbers(Text born, Text value, Text cap)
    {
        if (born != null)
        {
            Place(born.rectTransform, _bornX, 0f, _bornW, _textH);
            born.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        Place(value.rectTransform, _valueX, 0f, _valueW, _textH);
        value.horizontalOverflow = HorizontalWrapMode.Overflow;
        if (cap == null)
            return;
        Place(cap.rectTransform, _capX, 0f, _capW, _textH);
        cap.horizontalOverflow = HorizontalWrapMode.Overflow;
        cap.color = _capColor;
    }

    void PaintLuc(ulong baseAmount, ulong amount)
    {
        if (_lucPips != null)
        {
            int lit = amount > (ulong)_lucPips.Length ? _lucPips.Length : (int)amount;
            int born = baseAmount > (ulong)_lucPips.Length ? _lucPips.Length : (int)baseAmount;
            if (born > lit)
                born = lit;
            Color on = new Color(0.95f, 0.78f, 0.28f, 1f);
            Color dark = new Color(0.42f, 0.30f, 0.08f, 1f);
            for (int i = 0; i < _lucPips.Length; i++)
            {
                if (_lucPips[i] == null)
                    continue;
                if (i < born)
                    _lucPips[i].color = dark;
                else if (i < lit)
                    _lucPips[i].color = on;
                else
                    _lucPips[i].color = _trackColor;
            }
        }
        if (_lucBorn != null)
            _lucBorn.text = UiNumber.Format(baseAmount);
        if (_lucValue != null)
            _lucValue.text = _showBorn
                ? UiNumber.Format(amount)
                : UiNumber.Format(amount) + "→" + UiNumber.Format(amount);
        if (_lucCap != null)
            _lucCap.text = UiNumber.Format(_showBorn ? CharacterClass.LucMax : baseAmount);
    }

    void PaintBand(ulong baseRed, ulong baseGreen, ulong baseBlue, ulong red, ulong green, ulong blue, ulong nextRed, ulong nextGreen, ulong nextBlue, ulong canvas)
    {
        float width = canvas > 0 ? canvas : 1f;
        float nr = nextRed / width;
        float ng = nextGreen / width;
        float nb = nextBlue / width;
        float r = red / width;
        float g = green / width;
        float b = blue / width;
        float sum = nr + ng + nb;
        float currentSum = r + g + b;
        if (currentSum > sum)
            sum = currentSum;
        float scale = sum > 1f ? 1f / sum : 1f;
        nr *= scale;
        ng *= scale;
        nb *= scale;
        r *= scale;
        g *= scale;
        b *= scale;
        if (r > nr)
            r = nr;
        if (g > ng)
            g = ng;
        if (b > nb)
            b = nb;
        float br = baseRed / width * scale;
        float bg = baseGreen / width * scale;
        float bb = baseBlue / width * scale;
        if (br > r)
            br = r;
        if (bg > g)
            bg = g;
        if (bb > b)
            bb = b;
        SpanBand(_nextBandR, 0f, nr);
        SpanBand(_nextBandG, nr, nr + ng);
        SpanBand(_nextBandB, nr + ng, nr + ng + nb);
        SpanBand(_bandR, 0f, r);
        SpanBand(_bandG, nr, nr + g);
        SpanBand(_bandB, nr + ng, nr + ng + b);
        SpanBand(_baseBandR, 0f, br);
        SpanBand(_baseBandG, nr, nr + bg);
        SpanBand(_baseBandB, nr + ng, nr + ng + bb);
    }

    // 次の1回で増える分。合成上限に達しているときは今の値のまま。SPD と LUC には使わない。
    static ulong NextFused(ulong current, ulong baseStat, ulong levelStat, long fusionCount)
    {
        long cap = FusionBonus.MaxCount;
        if (cap < FusionBonus.InitialCap)
            cap = FusionBonus.InitialCap;
        if (fusionCount >= cap)
            return current;
        long grown = ToLong(baseStat) + ToLong(levelStat);
        if (grown < 0)
            grown = long.MaxValue;
        long nowBonus = FusionBonus.Bonus(grown, fusionCount);
        long nextBonus = FusionBonus.Bonus(grown, fusionCount + 1);
        if (nextBonus <= nowBonus)
            return current;
        long gain = nextBonus - nowBonus;
        if (current >= (ulong)long.MaxValue)
            return current;
        ulong room = (ulong)long.MaxValue - current;
        if ((ulong)gain >= room)
            return (ulong)long.MaxValue;
        return current + (ulong)gain;
    }

    static void LevelCapStats(StatisticsClass born, BattleBalanceConfig config, out ulong hp, out ulong atk, out ulong def, out ulong spd, out ulong red, out ulong green, out ulong blue)
    {
        if (config == null)
            config = BattleBalanceConfig.CreateDefault();
        long level = config.MaxLevel;
        if (level < 1)
            level = 1;

        long grownHp;
        long grownAtk;
        long grownDef;
        long grownSpd;
        long baseSpd = born != null ? born.SPD : 1;
        BattleLevelGrowth.ApplyLevels(
            born != null ? ToLong(born.HPMax) : 0,
            born != null ? ToLong(born.ATK) : 0,
            born != null ? ToLong(born.DEF) : 0,
            baseSpd,
            level,
            config,
            out grownHp, out grownAtk, out grownDef, out grownSpd);
        long steps = BattleLevelGrowth.GrowthSteps(level);
        long grownR = BattleLevelGrowth.AddGrowth(born != null ? ToLong(born.RCreates) : 0, config.RgbGrowthRate, steps);
        long grownG = BattleLevelGrowth.AddGrowth(born != null ? ToLong(born.GCreates) : 0, config.RgbGrowthRate, steps);
        long grownB = BattleLevelGrowth.AddGrowth(born != null ? ToLong(born.BCreates) : 0, config.RgbGrowthRate, steps);
        grownSpd = CharacterClass.ClampSpd(grownSpd);
        hp = WithFusionCap(grownHp);
        atk = WithFusionCap(grownAtk);
        def = WithFusionCap(grownDef);
        spd = (ulong)grownSpd;
        red = WithFusionCap(grownR);
        green = WithFusionCap(grownG);
        blue = WithFusionCap(grownB);
    }

    static ulong WithFusionCap(long grown)
    {
        if (grown < 0)
            grown = 0;
        long count = FusionBonus.MaxCount;
        if (count < FusionBonus.InitialCap)
            count = FusionBonus.InitialCap;
        long bonus = FusionBonus.Bonus(grown, count);
        if (bonus < 0)
            bonus = 0;
        if (grown > long.MaxValue - bonus)
            return (ulong)long.MaxValue;
        return (ulong)(grown + bonus);
    }

    static long ToLong(ulong value)
    {
        if (value > (ulong)long.MaxValue)
            return long.MaxValue;
        return (long)value;
    }

    static string RgbTrio(ulong red, ulong green, ulong blue)
    {
        return UiNumber.Format(red) + " " + UiNumber.Format(green) + " " + UiNumber.Format(blue);
    }

    static string RgbLines(ulong red, ulong green, ulong blue)
    {
        return UiNumber.Format(red) + "\n" + UiNumber.Format(green) + "\n" + UiNumber.Format(blue);
    }

    static Color Darken(Color color)
    {
        return new Color(color.r * 0.42f, color.g * 0.42f, color.b * 0.42f, 1f);
    }

    static Color Brighten(Color color)
    {
        return Color.Lerp(color, Color.white, 0.55f);
    }

    static float StatRatio(ulong amount, ulong ceiling)
    {
        if (ceiling == 0 || amount == 0)
            return 0f;
        if (amount >= ceiling)
            return 1f;
        return (float)amount / ceiling;
    }

    static void SpanBand(Image image, float from, float to)
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

    Image CreateTrack(Transform parent, float x, float width, float height)
    {
        GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackObject.transform.SetParent(parent, false);
        Place(trackObject.GetComponent<RectTransform>(), x, 0f, width, height);
        Image track = trackObject.GetComponent<Image>();
        track.sprite = _whiteSprite;
        track.color = _trackColor;
        track.raycastTarget = false;
        return track;
    }

    Image CreateFill(Transform parent, Color color)
    {
        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(parent, false);
        Stretch(fillObject.GetComponent<RectTransform>());
        Image fill = fillObject.GetComponent<Image>();
        fill.sprite = _whiteSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.color = color;
        fill.raycastTarget = false;
        return fill;
    }

    Image CreateBand(Transform parent, Color color)
    {
        GameObject bandObject = new GameObject("Band", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bandObject.transform.SetParent(parent, false);
        Image image = bandObject.GetComponent<Image>();
        image.sprite = _whiteSprite;
        image.color = color;
        image.raycastTarget = false;
        SpanBand(image, 0f, 0f);
        return image;
    }

    void EnsureWhiteSprite()
    {
        if (_whiteSprite != null)
            return;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = _labelColor;
        text.raycastTarget = false;
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
