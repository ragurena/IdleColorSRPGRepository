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
    CharacterStatGraph _stats;
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
        label.supportRichText = true;
        label.text = CellText(character);

        EnsureWhiteFrame(cell.transform, character.ID == _selectedId ? FrameSelected : FrameThin);
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
        _stats = CharacterStatGraph.Create(
            detailRoot.transform,
            "FusionStatGraph",
            _font,
            300f,
            0f,
            400f,
            280f,
            false,
            Color.white,
            new Color(0.78f, 0.8f, 0.84f, 1f),
            new Color(0.82f, 0.82f, 0.86f, 1f));

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
