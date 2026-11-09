using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 残機合成。一度所持したキャラは、今の所持が0でも一覧に出す。合成ボタンは、選んだキャラが合成できるときだけ押せる。
public class ControllerFusionClass : MonoBehaviour
{
    ControllerProduction _production;
    Font _font;
    Text _detail;
    RectTransform _detailRows;
    Text _lineCount;
    Text _lineCost;
    Text _lineRate;
    Text _lineNote;
    readonly Text[] _stockLabel = new Text[4];
    readonly Text[] _stockNow = new Text[4];
    readonly Text[] _stockArrow = new Text[4];
    readonly Text[] _stockNext = new Text[4];
    float _detailTextW;
    const float DetailRowH = 30f;
    Image _detailImage;
    Text _detailName;
    Button _fuseButton;
    CharacterStatGraph _stats;
    RectTransform _content;
    uint _selectedId;
    bool _fusionFxPlaying;
    const float FrameThin = 3f;
    const float FrameSelected = 8f;
    static readonly Color CellBackground = Color.black;
    static readonly Color CellBackgroundSelected = new Color(0.2f, 0.2f, 0.2f, 1f);
    readonly Dictionary<uint, Sprite> _sprites = new Dictionary<uint, Sprite>();
    readonly List<CellEntry> _cells = new List<CellEntry>();

    class CellEntry
    {
        public uint Id;
        public Image Background;
        public Image Picture;
        public Text Label;
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
            if (character.OwnedNumMax < 1)
                continue;
            _cells.Add(CreateCell(character));
            if (character.ID == _selectedId)
                selectedStillThere = true;
        }

        ImportedCharacters.ForEach(character =>
        {
            if (character == null || character.ID < ImportedCharacters.FirstId || character.OwnedNumMax < 1)
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
        if (_fusionFxPlaying)
            return;
        CharacterClass character = _production.GetCharacter(characterId);
        if (character == null || character.ID != characterId)
            return;
        _selectedId = characterId;
        for (int i = 0; i < _cells.Count; i++)
        {
            CellEntry cell = _cells[i];
            if (cell.Background == null)
                continue;
            bool selected = cell.Id == _selectedId;
            cell.Background.color = selected ? CellBackgroundSelected : CellBackground;
            EnsureWhiteFrame(cell.Background.transform, selected ? FrameSelected : FrameThin);
        }
        ShowDetail();
    }

    void PushFuse()
    {
        if (_production == null || _selectedId == 0 || _fusionFxPlaying)
            return;
        CharacterClass character = _production.GetCharacter(_selectedId);
        if (!_production.TryFuse(_selectedId))
            return;
        Refresh();
        Color color = ControllerProduction.RepresentativeColor(character);
        if (_fuseButton != null)
            _fuseButton.interactable = false;
        _fusionFxPlaying = true;
        FusionCompleteFx.Play(_detailImage, color, OnFusionFxDone);
    }

    void OnFusionFxDone()
    {
        _fusionFxPlaying = false;
        ShowDetail();
    }

    CellEntry CreateCell(CharacterClass character)
    {
        GameObject cell = new GameObject("FusionCell" + character.ID, typeof(RectTransform));
        cell.transform.SetParent(_content, false);
        bool selected = character.ID == _selectedId;
        Image background = cell.AddComponent<Image>();
        background.color = selected ? CellBackgroundSelected : CellBackground;
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
        label.supportRichText = true;
        label.text = CellText(character);

        EnsureWhiteFrame(cell.transform, selected ? FrameSelected : FrameThin);
        return new CellEntry { Id = character.ID, Background = background, Picture = image, Label = label };
    }

    static string CellText(CharacterClass character)
    {
        long lives = ToLong(character.OwnedNumCur);
        long count = ToLong(character.FusionCount);
        long cap = FusionBonus.MaxCount;
        long cost = FusionBonus.NextCost(count);
        long after = lives - cost;
        string fusion = UiNumber.LevelText("合成 " + UiNumber.Format(count) + "回", count, cap);
        string change = lives.ToString() + " → " + after.ToString();
        if (after <= 1)
            change = "<color=#ff2a2a>" + change + "</color>";
        return character.Name + "\n" + fusion + "\n" + change;
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
        if (_stats != null)
            _stats.Paint(character, _production != null ? _production.GetBattleBalance() : null);
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
            {
                _detail.gameObject.SetActive(true);
                _detail.text = "キャラを選んでください";
            }
            if (_detailRows != null)
                _detailRows.gameObject.SetActive(false);
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
        string countLine = UiNumber.LevelText(UiNumber.Format(count) + "/" + UiNumber.Format(cap) + "回", count, cap);
        long nowPercent = FusionBonus.Percent(count);
        long nextPercent = FusionBonus.Percent(atCap ? count : count + 1);
        _detail.gameObject.SetActive(false);
        if (_detailRows != null)
            _detailRows.gameObject.SetActive(true);
        character.EnsureStock();
        OwnedStock after = character.Stock;
        int spend = cost > int.MaxValue ? int.MaxValue : (int)cost;
        if (spend < 0)
            spend = 0;
        after.Consume(spend, true);
        if (_lineCount != null)
        {
            _lineCount.supportRichText = true;
            _lineCount.text = countLine;
        }
        if (_lineCost != null)
            _lineCost.text = "次は " + cost.ToString() + "体で合成";
        if (_lineRate != null)
            _lineRate.text = "+" + nowPercent.ToString() + "% → +" + nextPercent.ToString() + "%";
        long[] nowCounts = { lives, character.Stock.Full, character.Stock.Eight, character.Stock.Half };
        long[] nextCounts = { nextLives, after.Full, after.Eight, after.Half };
        for (int i = 0; i < 4; i++)
        {
            if (_stockNow[i] != null)
                _stockNow[i].text = nowCounts[i].ToString();
            if (_stockNext[i] != null)
                _stockNext[i].text = nextCounts[i].ToString();
        }
        string note = "";
        if (atCap)
            note = "合成上限です";
        else if (!canFuse)
            note = "所持が足りないため合成できません";
        if (_lineNote != null)
        {
            _lineNote.text = note;
            _lineNote.gameObject.SetActive(note.Length > 0);
        }
        LayoutDetail(note);
    }

    // 所持と倍率3行は、矢印と数字の列を揃える。空行は所持の直後。
    void LayoutDetail(string note)
    {
        if (_detailRows == null)
            return;
        float noteH = 0f;
        if (!string.IsNullOrEmpty(note))
            noteH = note.Length > 8 ? DetailRowH * 2f : DetailRowH;
        float block = DetailRowH * 8f + noteH;
        float y = block * 0.5f - DetailRowH * 0.5f;
        PlaceDetailLine(_lineCount, y, 0f, _detailTextW);
        y -= DetailRowH;
        PlaceDetailLine(_lineCost, y, 0f, _detailTextW);
        y -= DetailRowH;
        PlaceDetailLine(_lineRate, y, 0f, _detailTextW);
        y -= DetailRowH;
        PlaceStockRow(0, y);
        y -= DetailRowH;
        y -= DetailRowH;
        PlaceStockRow(1, y);
        y -= DetailRowH;
        PlaceStockRow(2, y);
        y -= DetailRowH;
        PlaceStockRow(3, y);
        if (_lineNote != null && noteH > 0f)
        {
            float noteY = y - DetailRowH * 0.5f - noteH * 0.5f;
            PlaceDetailLine(_lineNote, noteY, 0f, _detailTextW);
            _lineNote.rectTransform.sizeDelta = new Vector2(_detailTextW, noteH);
        }
    }

    void PlaceStockRow(int index, float y)
    {
        float left = -_detailTextW * 0.5f;
        const float labelW = 80f;
        const float numW = 44f;
        const float arrowW = 20f;
        const float gap = 4f;
        float x = left;
        PlaceDetailLine(_stockLabel[index], y, x + labelW * 0.5f, labelW);
        x += labelW + gap;
        PlaceDetailLine(_stockNow[index], y, x + numW * 0.5f, numW);
        x += numW + gap;
        PlaceDetailLine(_stockArrow[index], y, x + arrowW * 0.5f, arrowW);
        x += arrowW + gap;
        PlaceDetailLine(_stockNext[index], y, x + numW * 0.5f, numW);
    }

    void PlaceDetailLine(Text text, float y, float x, float width)
    {
        if (text == null)
            return;
        Place(text.rectTransform, x, y, width, DetailRowH);
    }

    static long ToLong(ulong value)
    {
        if (value > (ulong)long.MaxValue)
            return long.MaxValue;
        return (long)value;
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
        const float detailW = 1020f;
        Place(detailRoot.GetComponent<RectTransform>(), 0f, -325f, detailW, 312f);
        detailRoot.GetComponent<Image>().color = Color.black;

        // 左余白・キャラ↔テキスト・テキスト↔バー・右余白を同じ幅にして、列を均等に置く。
        const float colGap = 24f;
        const float charW = 200f;
        const float textW = 200f;
        float statsW = detailW - colGap * 4f - charW - textW;
        float cursor = -detailW * 0.5f + colGap;
        float charX = cursor + charW * 0.5f;
        cursor += charW + colGap;
        float textX = cursor + textW * 0.5f;
        cursor += textW + colGap;
        float statsX = cursor + statsW * 0.5f;

        GameObject detailPicture = new GameObject("FusionDetailPicture", typeof(RectTransform));
        detailPicture.transform.SetParent(detailRoot.transform, false);
        Place(detailPicture.GetComponent<RectTransform>(), charX, 36f, 190f, 190f);
        GameObject detailSprite = new GameObject("Sprite", typeof(RectTransform));
        detailSprite.transform.SetParent(detailPicture.transform, false);
        Stretch(detailSprite.GetComponent<RectTransform>());
        _detailImage = detailSprite.AddComponent<Image>();
        _detailImage.preserveAspect = true;
        _detailImage.raycastTarget = false;
        _detailImage.color = new Color(1f, 1f, 1f, 0f);

        _detailName = CreateText("TextFusionDetailName", detailRoot.transform, 22, TextAnchor.MiddleCenter);
        Place(_detailName.rectTransform, charX, -108f, 220f, 64f);
        _detailName.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailName.verticalOverflow = VerticalWrapMode.Truncate;

        // 肖像の上下にはみ出す量を同じにして、行は枠の中央に置く。
        _detailTextW = textW;
        _detail = CreateText("TextFusionDetail", detailRoot.transform, 24, TextAnchor.MiddleLeft);
        Place(_detail.rectTransform, textX, 36f, textW, 240f);
        _detail.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detail.verticalOverflow = VerticalWrapMode.Overflow;
        _detail.lineSpacing = 0.9f;

        GameObject rows = new GameObject("FusionDetailLines", typeof(RectTransform));
        rows.transform.SetParent(detailRoot.transform, false);
        _detailRows = rows.GetComponent<RectTransform>();
        Place(_detailRows, textX, 36f, textW, 240f);
        _detailRows.gameObject.SetActive(false);
        _lineCount = CreateDetailLine("Count", TextAnchor.MiddleLeft, true);
        _lineCost = CreateDetailLine("Cost", TextAnchor.MiddleLeft, true);
        _lineRate = CreateDetailLine("Rate", TextAnchor.MiddleLeft, true);
        string[] stockLabels = { "所持", "×1:", "×0.8:", "×0.5:" };
        for (int i = 0; i < 4; i++)
        {
            _stockLabel[i] = CreateDetailLine("StockLabel" + i.ToString(), TextAnchor.MiddleLeft, false);
            _stockLabel[i].text = stockLabels[i];
            _stockNow[i] = CreateDetailLine("StockNow" + i.ToString(), TextAnchor.MiddleRight, false);
            _stockArrow[i] = CreateDetailLine("StockArrow" + i.ToString(), TextAnchor.MiddleCenter, false);
            _stockArrow[i].text = "→";
            _stockNext[i] = CreateDetailLine("StockNext" + i.ToString(), TextAnchor.MiddleRight, false);
        }
        _lineNote = CreateDetailLine("Note", TextAnchor.UpperLeft, true);
        _lineNote.horizontalOverflow = HorizontalWrapMode.Wrap;
        _lineNote.verticalOverflow = VerticalWrapMode.Overflow;
        _lineNote.gameObject.SetActive(false);
        _stats = CharacterStatGraph.Create(
            detailRoot.transform,
            "FusionStatGraph",
            _font,
            statsX,
            4f,
            statsW,
            300f,
            false,
            Color.white,
            new Color(0.78f, 0.8f, 0.84f, 1f),
            new Color(0.82f, 0.82f, 0.86f, 1f),
            18,
            16,
            16,
            13,
            14,
            26f,
            124f,
            48f,
            38f,
            false,
            false);

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

    Text CreateDetailLine(string name, TextAnchor alignment, bool wrap)
    {
        Text text = CreateText(name, _detailRows, 24, alignment);
        text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
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
