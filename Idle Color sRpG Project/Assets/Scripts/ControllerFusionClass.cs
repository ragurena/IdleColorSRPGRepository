using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 残機合成。残機が1以上のキャラはどれも選べる。合成ボタンは、選んだキャラが合成できるときだけ押せる。
public class ControllerFusionClass : MonoBehaviour
{
    ControllerProduction _production;
    Font _font;
    Text _detail;
    Image _detailImage;
    Text _detailName;
    Button _fuseButton;
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
    Text _bandValue;
    Text _bandCap;
    RectTransform _content;
    uint _selectedId;
    const float FrameThin = 3f;
    const float FrameSelected = 8f;
    readonly Dictionary<uint, Sprite> _sprites = new Dictionary<uint, Sprite>();
    readonly List<CellEntry> _cells = new List<CellEntry>();

    class CellEntry
    {
        public uint Id;
        public Image Background;
        public Image Picture;
        public Text Label;
    }

    class StatGauge
    {
        public Image NextFill;
        public Image Fill;
        public Image BaseFill;
        public Text Value;
        public Text Cap;
        public bool Sqrt;
    }

    public GameObject Panel { get; private set; }

    public void Initialize(ControllerProduction production, GameObject sizeSource)
    {
        _production = production;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Build(sizeSource);
    }

    public void Open()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_content == null || _production == null)
            return;

        for (int i = 0; i < _cells.Count; i++)
        {
            if (_cells[i].Background != null)
                Destroy(_cells[i].Background.gameObject);
        }
        _cells.Clear();

        bool selectedStillThere = false;
        CharacterDefinition[] roster = CharacterRoster.All;
        for (int i = 0; i < roster.Length; i++)
        {
            CharacterClass character = _production.GetCharacter(roster[i].Id);
            if (character == null || character.ID != roster[i].Id)
                continue;
            if (character.OwnedNumCur < 1)
                continue;
            _cells.Add(CreateCell(character));
            if (character.ID == _selectedId)
                selectedStillThere = true;
        }

        ImportedCharacters.ForEach(character =>
        {
            if (character == null || character.ID < ImportedCharacters.FirstId || character.OwnedNumCur < 1)
                return;
            _cells.Add(CreateCell(character));
            if (character.ID == _selectedId)
                selectedStillThere = true;
        });

        if (selectedStillThere)
            ShowDetail();
        else if (_cells.Count > 0)
            Select(_cells[0].Id);
        else
        {
            _selectedId = 0;
            ShowDetail();
        }
    }

    static bool IsNearBlack(CharacterClass character)
    {
        if (character == null)
            return false;
        int r = character.RepresentativeR;
        int g = character.RepresentativeG;
        int b = character.RepresentativeB;
        if (r != g || g != b)
            return false;
        return r == 0 || r == 16 || r == 32;
    }

    static bool CanFuse(CharacterClass character)
    {
        if (character == null)
            return false;
        return FusionBonus.CanFuse(ToLong(character.OwnedNumCur), ToLong(character.FusionCount));
    }

    void Select(uint characterId)
    {
        CharacterClass character = _production.GetCharacter(characterId);
        if (character == null || character.ID != characterId)
            return;
        _selectedId = characterId;
        for (int i = 0; i < _cells.Count; i++)
        {
            CellEntry cell = _cells[i];
            if (cell.Background == null)
                continue;
            EnsureWhiteFrame(cell.Background.transform, cell.Id == _selectedId ? FrameSelected : FrameThin);
        }
        ShowDetail();
    }

    void PushFuse()
    {
        if (_production == null || _selectedId == 0)
            return;
        if (!_production.TryFuse(_selectedId))
            return;
        Refresh();
    }

    CellEntry CreateCell(CharacterClass character)
    {
        GameObject cell = new GameObject("FusionCell" + character.ID, typeof(RectTransform));
        cell.transform.SetParent(_content, false);
        Image background = cell.AddComponent<Image>();
        background.color = Color.black;
        Button button = cell.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
        uint id = character.ID;
        button.onClick.AddListener(() => Select(id));

        GameObject picture = new GameObject("Picture", typeof(RectTransform));
        picture.transform.SetParent(cell.transform, false);
        RectTransform pictureRect = picture.GetComponent<RectTransform>();
        pictureRect.anchorMin = new Vector2(0.143f, 0.50f);
        pictureRect.anchorMax = new Vector2(0.857f, 0.98f);
        pictureRect.offsetMin = Vector2.zero;
        pictureRect.offsetMax = Vector2.zero;

        GameObject spriteObject = new GameObject("Sprite", typeof(RectTransform));
        spriteObject.transform.SetParent(picture.transform, false);
        Stretch(spriteObject.GetComponent<RectTransform>());
        Image image = spriteObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.sprite = SpriteOf(character);
        image.color = Color.white;
        ControllerProduction.EnsureOwnedAttributeGlow(picture.transform, character);
        spriteObject.transform.SetAsLastSibling();
        CatalogNewMark.Apply(picture.transform, character);

        Text label = CreateText("Label", cell.transform, 24, TextAnchor.UpperCenter);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0.48f);
        labelRect.offsetMin = new Vector2(10f, 10f);
        labelRect.offsetMax = new Vector2(-10f, -2f);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        bool lowAfter;
        label.text = CellText(character, out lowAfter);
        if (lowAfter)
            label.color = new Color(1f, 0.16f, 0.16f, 1f);

        EnsureWhiteFrame(cell.transform, character.ID == _selectedId ? FrameSelected : FrameThin);
        return new CellEntry { Id = character.ID, Background = background, Picture = image, Label = label };
    }

    static string CellText(CharacterClass character, out bool lowAfter)
    {
        long lives = ToLong(character.OwnedNumCur);
        long cost = FusionBonus.NextCost(ToLong(character.FusionCount));
        long after = lives - cost;
        lowAfter = after <= 1;
        return character.Name + "\n" + lives.ToString() + " → " + after.ToString();
    }

    void ShowDetail()
    {
        CharacterClass character = _production != null ? _production.GetCharacter(_selectedId) : null;
        bool canFuse = CanFuse(character);
        if (_fuseButton != null)
        {
            Color tint = ControllerProduction.RepresentativeColor(character);
            if (_production != null)
                _production.StyleRepresentativeButton(_fuseButton, tint, canFuse);
            _fuseButton.interactable = canFuse;
            Text fuseLabel = _fuseButton.GetComponentInChildren<Text>();
            if (fuseLabel != null)
            {
                bool dark = canFuse && IsNearBlack(character);
                fuseLabel.color = canFuse ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
                fuseLabel.fontSize = dark ? 60 : 44;
                if (dark)
                {
                    GaugeTextGlow glow = fuseLabel.GetComponent<GaugeTextGlow>();
                    if (glow != null)
                    {
                        glow.Glow = new Color(1f, 1f, 1f, 0.9f);
                        glow.Spread = 2.2f;
                        fuseLabel.SetVerticesDirty();
                    }
                }
            }
            Transform frame = _fuseButton.transform.Find("WhiteFrame");
            if (frame != null)
                frame.gameObject.SetActive(!canFuse);
            if (!canFuse)
                SetFrameColor(_fuseButton.transform, new Color(0.45f, 0.45f, 0.45f, 1f));
        }
        PaintStatGraph(character);
        if (character == null)
        {
            if (_detailImage != null)
            {
                _detailImage.sprite = null;
                _detailImage.color = new Color(1f, 1f, 1f, 0f);
                ControllerProduction.RemoveAttributeGlow(_detailImage.transform.parent);
                CatalogNewMark.Apply(_detailImage.transform, null);
            }
            if (_detailName != null)
                _detailName.text = "";
            if (_detail != null)
                _detail.text = "キャラを選んでください";
            return;
        }
        if (_detailImage != null)
        {
            _detailImage.sprite = SpriteOf(character);
            _detailImage.color = Color.white;
            _detailImage.preserveAspect = true;
            ControllerProduction.EnsureOwnedAttributeGlow(_detailImage.transform.parent, character);
            _detailImage.transform.SetAsLastSibling();
            CatalogNewMark.Apply(_detailImage.transform, character);
        }
        if (_detailName != null)
            _detailName.text = character.Name;
        if (_detail == null)
            return;

        long count = ToLong(character.FusionCount);
        long cap = FusionBonus.MaxCount;
        bool atCap = cap > 0 && count >= cap;
        long lives = ToLong(character.OwnedNumCur);
        long cost = FusionBonus.NextCost(count);
        long nextLives = lives - cost;
        string countText = UiNumber.LevelText(UiNumber.Format(count) + "回", count, cap);
        _detail.supportRichText = true;
        _detail.text = "合成 " + countText + " / " + UiNumber.Format(cap) + "回（+" + FusionBonus.Percent(count).ToString() + "%）\n"
            + "次は " + cost.ToString() + "体で +" + FusionBonus.PercentPerFusion.ToString() + "%（+" + FusionBonus.Percent(count + 1).ToString() + "%）\n"
            + "所持 " + lives.ToString() + " → " + nextLives.ToString();
        if (atCap)
            _detail.text += "\n合成上限です";
        else if (!canFuse)
            _detail.text += "\n所持が足りないため合成できません";
    }

    static long ToLong(ulong value)
    {
        if (value > (ulong)long.MaxValue)
            return long.MaxValue;
        return (long)value;
    }

    void PaintStatGraph(CharacterClass character)
    {
        if (_gauges == null)
            return;
        if (character == null || character.Stats == null)
        {
            ClearStatGraph();
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
        LevelCapStats(born, out capHp, out capAtk, out capDef, out capSpd, out capR, out capG, out capB);
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
            if (nextRatio < ratio)
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
                _gauges[i].Value.text = UiNumber.Format(amount) + "→" + UiNumber.Format(nexts[i]);
            if (_gauges[i].Cap != null)
                _gauges[i].Cap.text = UiNumber.Format(ceilings[i]);
        }
        ulong bandCanvas = capR + capG + capB;
        if (bandCanvas < 1)
            bandCanvas = 1;
        ulong nextR = NextFused(now.RCreates, born.RCreates, level != null ? level.RCreates : 0, fusionCount);
        ulong nextG = NextFused(now.GCreates, born.GCreates, level != null ? level.GCreates : 0, fusionCount);
        ulong nextB = NextFused(now.BCreates, born.BCreates, level != null ? level.BCreates : 0, fusionCount);
        PaintLuc(born.LUC, now.LUC);
        PaintBand(born.RCreates, born.GCreates, born.BCreates, now.RCreates, now.GCreates, now.BCreates, nextR, nextG, nextB, bandCanvas);
        if (_bandValue != null)
            _bandValue.text = RgbTrio(now.RCreates, now.GCreates, now.BCreates) + "\n→" + RgbTrio(nextR, nextG, nextB);
        if (_bandCap != null)
            _bandCap.text = RgbTrio(capR, capG, capB);
    }

    static string RgbTrio(ulong red, ulong green, ulong blue)
    {
        return UiNumber.Format(red) + " " + UiNumber.Format(green) + " " + UiNumber.Format(blue);
    }

    void ClearStatGraph()
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
                if (_gauges[i].Value != null)
                    _gauges[i].Value.text = "";
                if (_gauges[i].Cap != null)
                    _gauges[i].Cap.text = "";
            }
        }
        PaintLuc(0, 0);
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
        if (_bandValue != null)
            _bandValue.text = "";
        if (_bandCap != null)
            _bandCap.text = "";
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
            Color off = new Color(0.82f, 0.82f, 0.86f, 1f);
            for (int i = 0; i < _lucPips.Length; i++)
            {
                if (_lucPips[i] == null)
                    continue;
                if (i < born)
                    _lucPips[i].color = dark;
                else if (i < lit)
                    _lucPips[i].color = on;
                else
                    _lucPips[i].color = off;
            }
        }
        if (_lucValue != null)
            _lucValue.text = UiNumber.Format(amount) + "→" + UiNumber.Format(amount);
        if (_lucCap != null)
            _lucCap.text = UiNumber.Format(baseAmount);
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

    // 棒の右端は、基礎ステータスをレベル上限まで育て、そこへ合成上限のボーナスを足した値。装備は含めない。
    // SPD と LUC は合成で増えないので、レベル上限のまま。
    void LevelCapStats(StatisticsClass born, out ulong hp, out ulong atk, out ulong def, out ulong spd, out ulong red, out ulong green, out ulong blue)
    {
        BattleBalanceConfig config = _production != null ? _production.GetBattleBalance() : null;
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
        if (grownSpd > byte.MaxValue)
            grownSpd = byte.MaxValue;
        if (grownSpd < 1)
            grownSpd = 1;
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

    static void PlaceStatNumbers(Text value, Text cap)
    {
        Place(value.rectTransform, 52f, 0f, 104f, 32f);
        value.horizontalOverflow = HorizontalWrapMode.Overflow;
        Place(cap.rectTransform, 156f, 0f, 76f, 32f);
        cap.horizontalOverflow = HorizontalWrapMode.Overflow;
        cap.color = new Color(0.78f, 0.8f, 0.84f, 1f);
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

    void BuildStatGraph(Transform parent)
    {
        EnsureWhiteSprite();
        GameObject root = new GameObject("FusionStatGraph", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Place(root.GetComponent<RectTransform>(), 300f, 0f, 400f, 280f);

        Text capHead = CreateText("CapHead", root.transform, 12, TextAnchor.MiddleRight);
        Place(capHead.rectTransform, 156f, 128f, 76f, 18f);
        capHead.text = "上限";
        capHead.color = new Color(0.78f, 0.8f, 0.84f, 1f);

        _gauges = new StatGauge[4];
        _gauges[0] = BuildGauge(root.transform, "HP", new Color(0.76f, 0.28f, 0.28f, 1f), 92f, true);
        _gauges[1] = BuildGauge(root.transform, "ATK", new Color(0.86f, 0.46f, 0.18f, 1f), 52f, true);
        _gauges[2] = BuildGauge(root.transform, "DEF", new Color(0.24f, 0.46f, 0.78f, 1f), 12f, false);
        _gauges[3] = BuildGauge(root.transform, "SPD", new Color(0.18f, 0.62f, 0.42f, 1f), -28f, false);
        BuildLucRow(root.transform, -68f);
        BuildRgbRow(root.transform, -108f);
    }

    StatGauge BuildGauge(Transform parent, string label, Color color, float y, bool sqrt)
    {
        GameObject row = new GameObject("Gauge" + label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        Place(row.GetComponent<RectTransform>(), 0f, y, 400f, 36f);

        Text name = CreateText("Label", row.transform, 16, TextAnchor.MiddleLeft);
        Place(name.rectTransform, -176f, 0f, 44f, 32f);
        name.text = label;

        Image track = CreateTrack(row.transform, -78f, 120f, 12f);
        Image nextFill = CreateFill(track.transform, Brighten(color));
        Image fill = CreateFill(track.transform, color);
        Image baseFill = CreateFill(track.transform, Darken(color));

        Text value = CreateText("Value", row.transform, 13, TextAnchor.MiddleRight);
        Text cap = CreateText("Cap", row.transform, 13, TextAnchor.MiddleRight);
        PlaceStatNumbers(value, cap);

        return new StatGauge { NextFill = nextFill, Fill = fill, BaseFill = baseFill, Value = value, Cap = cap, Sqrt = sqrt };
    }

    void BuildLucRow(Transform parent, float y)
    {
        GameObject row = new GameObject("GaugeLUC", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        Place(row.GetComponent<RectTransform>(), 0f, y, 400f, 36f);

        Text name = CreateText("Label", row.transform, 16, TextAnchor.MiddleLeft);
        Place(name.rectTransform, -176f, 0f, 44f, 32f);
        name.text = "LUC";

        int count = (int)CharacterClass.LucMax;
        if (count < 1)
            count = 1;
        _lucPips = new Image[count];
        float pip = 11f;
        float gap = 2f;
        float total = count * pip + (count - 1) * gap;
        float left = -78f - total * 0.5f + pip * 0.5f;
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

        _lucValue = CreateText("Value", row.transform, 13, TextAnchor.MiddleRight);
        _lucCap = CreateText("Cap", row.transform, 13, TextAnchor.MiddleRight);
        PlaceStatNumbers(_lucValue, _lucCap);
    }

    void BuildRgbRow(Transform parent, float y)
    {
        GameObject row = new GameObject("GaugeRGB", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        Place(row.GetComponent<RectTransform>(), 0f, y, 400f, 36f);

        Text name = CreateText("Label", row.transform, 16, TextAnchor.MiddleLeft);
        Place(name.rectTransform, -176f, 0f, 44f, 32f);
        name.text = "RGB";

        Image track = CreateTrack(row.transform, -78f, 120f, 12f);
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

        _bandValue = CreateText("Value", row.transform, 12, TextAnchor.UpperRight);
        _bandCap = CreateText("Cap", row.transform, 12, TextAnchor.MiddleRight);
        PlaceStatNumbers(_bandValue, _bandCap);
        _bandValue.fontSize = 12;
        _bandValue.alignment = TextAnchor.UpperRight;
        _bandValue.verticalOverflow = VerticalWrapMode.Overflow;
        Place(_bandValue.rectTransform, 52f, 8f, 104f, 16f);
        _bandCap.fontSize = 12;
    }

    Image CreateTrack(Transform parent, float x, float width, float height)
    {
        GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackObject.transform.SetParent(parent, false);
        Place(trackObject.GetComponent<RectTransform>(), x, 0f, width, height);
        Image track = trackObject.GetComponent<Image>();
        track.sprite = _whiteSprite;
        track.color = new Color(0.82f, 0.82f, 0.86f, 1f);
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

    Sprite SpriteOf(CharacterClass character)
    {
        Sprite cached;
        if (_sprites.TryGetValue(character.ID, out cached) && cached != null)
            return cached;

        Texture2D tex = character.ImageTexture2D;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        tex.filterMode = FilterMode.Point;
        int size = character.Size > 0 ? character.Size : tex.width;
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        _sprites[character.ID] = sprite;
        return sprite;
    }

    void Build(GameObject sizeSource)
    {
        if (Panel != null || sizeSource == null)
            return;

        Panel = new GameObject("PanelFusion", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        Panel.transform.SetParent(sizeSource.transform.parent, false);
        RectTransform panelRect = Panel.GetComponent<RectTransform>();
        RectTransform sourceRect = sizeSource.GetComponent<RectTransform>();
        panelRect.anchorMin = sourceRect.anchorMin;
        panelRect.anchorMax = sourceRect.anchorMax;
        panelRect.pivot = sourceRect.pivot;
        panelRect.anchoredPosition = sourceRect.anchoredPosition;
        panelRect.sizeDelta = sourceRect.sizeDelta;
        panelRect.localScale = Vector3.one;
        Panel.GetComponent<Image>().color = Color.black;
        Panel.GetComponent<Image>().raycastTarget = true;
        CanvasGroup group = Panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        BuildScroll();

        GameObject detailRoot = new GameObject("FusionDetail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        detailRoot.transform.SetParent(Panel.transform, false);
        Place(detailRoot.GetComponent<RectTransform>(), 0f, -320f, 1020f, 300f);
        detailRoot.GetComponent<Image>().color = Color.black;
        EnsureWhiteFrame(detailRoot.transform, FrameThin);

        GameObject detailPicture = new GameObject("FusionDetailPicture", typeof(RectTransform));
        detailPicture.transform.SetParent(detailRoot.transform, false);
        Place(detailPicture.GetComponent<RectTransform>(), -370f, 36f, 190f, 190f);
        GameObject detailSprite = new GameObject("Sprite", typeof(RectTransform));
        detailSprite.transform.SetParent(detailPicture.transform, false);
        Stretch(detailSprite.GetComponent<RectTransform>());
        _detailImage = detailSprite.AddComponent<Image>();
        _detailImage.preserveAspect = true;
        _detailImage.raycastTarget = false;
        _detailImage.color = new Color(1f, 1f, 1f, 0f);

        _detailName = CreateText("TextFusionDetailName", detailRoot.transform, 22, TextAnchor.MiddleCenter);
        Place(_detailName.rectTransform, -370f, -108f, 240f, 64f);
        _detailName.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailName.verticalOverflow = VerticalWrapMode.Truncate;

        _detail = CreateText("TextFusionDetail", detailRoot.transform, 28, TextAnchor.MiddleLeft);
        Place(_detail.rectTransform, -100f, 0f, 340f, 270f);
        _detail.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detail.verticalOverflow = VerticalWrapMode.Truncate;
        BuildStatGraph(detailRoot.transform);

        _fuseButton = CreateButton("ButtonFusion", "合成", Color.white);
        Place(_fuseButton.GetComponent<RectTransform>(), 0f, -570f, 1020f, 150f);
        Text fuseLabel = _fuseButton.GetComponentInChildren<Text>();
        if (fuseLabel != null)
            fuseLabel.fontSize = 44;
        ColorBlock fuseColors = _fuseButton.colors;
        fuseColors.normalColor = Color.black;
        fuseColors.highlightedColor = new Color(0.16f, 0.16f, 0.18f, 1f);
        fuseColors.pressedColor = new Color(0.28f, 0.28f, 0.3f, 1f);
        fuseColors.selectedColor = Color.black;
        fuseColors.disabledColor = Color.black;
        fuseColors.colorMultiplier = 1f;
        _fuseButton.colors = fuseColors;
        EnsureWhiteFrame(_fuseButton.transform, FrameThin);
        _fuseButton.interactable = false;
        _fuseButton.onClick.AddListener(PushFuse);
    }

    void BuildScroll()
    {
        GameObject scrollObject = new GameObject("FusionScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(Panel.transform, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        Place(scrollRect, 0f, 280f, 1040f, 760f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        _content = content.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(0f, 0f);

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(188f, 280f);
        grid.spacing = new Vector2(16f, 16f);
        grid.padding = new RectOffset(16, 16, 16, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = _content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        EnsureWhiteFrame(scrollObject.transform, FrameThin);
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
        return text;
    }

    Button CreateButton(string name, string label, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(Panel.transform, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        Button button = obj.AddComponent<Button>();
        Text text = CreateText("Text", obj.transform, 28, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.text = label;
        text.color = Color.white;
        return button;
    }

    static void EnsureWhiteFrame(Transform target, float thickness)
    {
        if (target == null)
            return;
        Transform frame = target.Find("WhiteFrame");
        if (frame == null)
        {
            GameObject root = new GameObject("WhiteFrame", typeof(RectTransform));
            root.transform.SetParent(target, false);
            Stretch(root.GetComponent<RectTransform>());
            AddWhiteEdge(root.transform, "Top", true, true);
            AddWhiteEdge(root.transform, "Bottom", true, false);
            AddWhiteEdge(root.transform, "Left", false, false);
            AddWhiteEdge(root.transform, "Right", false, true);
            frame = root.transform;
        }
        LayoutWhiteEdge(frame, "Top", true, true, thickness);
        LayoutWhiteEdge(frame, "Bottom", true, false, thickness);
        LayoutWhiteEdge(frame, "Left", false, false, thickness);
        LayoutWhiteEdge(frame, "Right", false, true, thickness);
        frame.SetAsLastSibling();
    }

    static void AddWhiteEdge(Transform parent, string name, bool horizontal, bool end)
    {
        GameObject edgeObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        edgeObject.transform.SetParent(parent, false);
        Image image = edgeObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        LayoutWhiteEdge(parent, name, horizontal, end, FrameThin);
    }

    static void LayoutWhiteEdge(Transform frame, string name, bool horizontal, bool end, float thickness)
    {
        Transform edge = frame.Find(name);
        if (edge == null)
            return;
        RectTransform rect = edge.GetComponent<RectTransform>();
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, end ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, end ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, end ? 1f : 0f);
            rect.sizeDelta = new Vector2(0f, thickness);
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(end ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(end ? 1f : 0f, 1f);
            rect.pivot = new Vector2(end ? 1f : 0f, 0.5f);
            rect.sizeDelta = new Vector2(thickness, 0f);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    static void SetFrameColor(Transform target, Color color)
    {
        if (target == null)
            return;
        Transform frame = target.Find("WhiteFrame");
        if (frame == null)
            return;
        Image[] images = frame.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
            images[i].color = color;
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
