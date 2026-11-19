using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 星で解放し、白ピクセルで強化するスキル画面。
public class ControllerEnhanceClass : MonoBehaviour
{
    ControllerProduction _production;
    Font _font;
    Text _starsHeld;
    Image _whiteChip;
    Text _whiteHeld;
    RectTransform _scrollRect;
    RectTransform _headerStar;
    RectTransform _content;
    readonly List<SkillRow> _rows = new List<SkillRow>();
    int _shownStars = -1;
    ulong _shownWhite = ulong.MaxValue;
    const float FrameThin = 3f;
    const float RowWidth = 1000f;
    const float RowHeight = 240f;
    const float RowGap = 16f;
    const float ListBottomPad = 30f;
    const float HeaderHeight = 56f;
    const float HeaderGap = 12f;
    static readonly Color RowBackground = new Color(0.08f, 0.08f, 0.1f, 1f);
    static readonly Color StarLit = new Color(1f, 0.86f, 0.2f, 1f);
    static readonly Color StarGlowLit = new Color(1f, 1f, 0.82f, 1f);
    static readonly Color ButtonYellow = new Color(1f, 0.86f, 0.2f, 1f);

    class SkillRow
    {
        public EnhanceSkillId Id;
        public Image Icon;
        public Text Name;
        public Text Description;
        public Text Level;
        public Text Effect;
        public Button Action;
        public Text ActionLabel;
        public RectTransform ButtonCostGroup;
        public GameObject ButtonStar;
        public Image ButtonWhiteChip;
        public Text ButtonCost;
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
        PlaceList();
        _shownStars = -1;
        _shownWhite = ulong.MaxValue;
        Refresh();
    }

    void Update()
    {
        if (!IsVisible() || _production == null)
            return;
        int stars = _production.GetHeldChallengeStars();
        ulong white = _production.GetWhitePixelStock();
        if (stars == _shownStars && white == _shownWhite)
            return;
        Refresh();
    }

    bool IsVisible()
    {
        if (Panel == null)
            return false;
        CanvasGroup group = Panel.GetComponent<CanvasGroup>();
        return group != null && group.alpha > 0.01f && group.interactable;
    }

    public void Refresh()
    {
        if (_production == null)
            return;
        int stars = _production.GetHeldChallengeStars();
        ulong white = _production.GetWhitePixelStock();
        _shownStars = stars;
        _shownWhite = white;
        if (_starsHeld != null)
            _starsHeld.text = UiNumber.Format(stars);
        if (_whiteChip != null)
        {
            _whiteChip.color = Color.white;
            ControllerProduction.StyleColorChip(_whiteChip);
        }
        if (_whiteHeld != null)
            _whiteHeld.text = UiNumber.Format(white);
        for (int i = 0; i < _rows.Count; i++)
            PaintRow(_rows[i]);
    }

    void PaintRow(SkillRow row)
    {
        if (row == null || _production == null)
            return;
        EnhanceSkillDef def = EnhanceSkillCatalog.Get(row.Id);
        if (def == null)
            return;
        int levelCap = _production.EnhanceLevelCap(row.Id);
        bool unlocked = _production.IsEnhanceUnlocked(row.Id);
        int level = _production.EnhanceLevel(row.Id);
        if (row.Name != null)
            row.Name.text = def.Name;
        if (row.Description != null)
            row.Description.text = def.Description;
        if (row.Level != null)
        {
            if (!unlocked)
            {
                row.Level.gameObject.SetActive(false);
            }
            else
            {
                row.Level.gameObject.SetActive(true);
                if (level >= levelCap)
                    row.Level.text = UiNumber.LevelText("Lv " + UiNumber.Format(level) + " / " + UiNumber.Format(levelCap) + " MAX", level, levelCap);
                else
                    row.Level.text = "Lv " + UiNumber.Format(level) + " / " + UiNumber.Format(levelCap);
            }
        }
        if (row.Effect != null)
        {
            if (!unlocked)
            {
                row.Effect.text = "効果 " + EnhanceSkillCatalog.EffectText(row.Id, 1) + "（Lv1時）";
                PlaceLeft(row.Effect.rectTransform, -420f, -50f, 620f, 40f);
            }
            else
            {
                PlaceLeft(row.Effect.rectTransform, -420f, -78f, 620f, 40f);
                string now = EnhanceSkillCatalog.EffectText(row.Id, level);
                if (level >= levelCap)
                    row.Effect.text = "効果 " + now;
                else
                    row.Effect.text = "効果 " + now + " → " + EnhanceSkillCatalog.EffectText(row.Id, level + 1);
            }
        }

        bool canPress = false;
        string actionText = "解放";
        bool showStar = false;
        bool showWhite = false;
        string buttonCost = "";
        if (!unlocked)
        {
            int need = def.UnlockStars;
            int held = _production.GetHeldChallengeStars();
            actionText = "解放";
            showStar = true;
            buttonCost = UiNumber.Format(need);
            canPress = held >= need;
        }
        else if (level >= levelCap)
        {
            actionText = "MAX";
            buttonCost = "";
            canPress = false;
        }
        else
        {
            long need = EnhanceSkillCatalog.UpgradeCost(def, level + 1);
            ulong held = _production.GetWhitePixelStock();
            actionText = "強化";
            showWhite = true;
            buttonCost = UiNumber.Format(need);
            canPress = held >= (ulong)need && need > 0 && need < long.MaxValue;
        }

        if (row.ActionLabel != null)
        {
            row.ActionLabel.text = actionText;
            Place(row.ActionLabel.rectTransform, 0f, -40f, 180f, 48f);
        }
        if (row.ButtonCostGroup != null)
            row.ButtonCostGroup.gameObject.SetActive(showStar || showWhite);
        if (row.ButtonStar != null)
            row.ButtonStar.SetActive(showStar);
        if (row.ButtonWhiteChip != null)
        {
            row.ButtonWhiteChip.gameObject.SetActive(showWhite);
            if (showWhite)
            {
                row.ButtonWhiteChip.color = Color.white;
                ControllerProduction.StyleColorChip(row.ButtonWhiteChip);
            }
        }
        if (row.ButtonCost != null)
        {
            row.ButtonCost.text = buttonCost;
            if (showStar)
            {
                // 星+数をまとめて中央揃え。数字は星から少し離す。
                Place(row.ButtonCostGroup, 0f, 36f, 140f, 48f);
                Place(row.ButtonStar.GetComponent<RectTransform>(), -28f, 0f, 74f, 74f);
                PlaceLeft(row.ButtonCost.rectTransform, 18f, 0f, 70f, 40f);
                row.ButtonCost.alignment = TextAnchor.MiddleLeft;
                row.ButtonCost.fontSize = 30;
            }
            else if (showWhite)
            {
                Place(row.ButtonCostGroup, 0f, 36f, 150f, 48f);
                Place(row.ButtonWhiteChip.rectTransform, -34f, 0f, 28f, 28f);
                PlaceLeft(row.ButtonCost.rectTransform, 8f, 0f, 90f, 40f);
                row.ButtonCost.alignment = TextAnchor.MiddleLeft;
                row.ButtonCost.fontSize = 26;
            }
        }
        if (row.Action != null && _production != null)
        {
            Color tint = showStar ? ButtonYellow : Color.white;
            _production.StyleRepresentativeButton(row.Action, tint, canPress);
        }
    }

    void PushAction(EnhanceSkillId id)
    {
        if (_production == null)
            return;
        if (_production.IsEnhanceUnlocked(id))
        {
            if (_production.TryUpgradeEnhance(id))
                Refresh();
            return;
        }

        EnhanceSkillDef def = EnhanceSkillCatalog.Get(id);
        if (def == null)
            return;
        int need = def.UnlockStars;
        if (_production.GetHeldChallengeStars() < need)
            return;
        string name = def.Name ?? "この";
        string message = "星を" + UiNumber.Format(need) + "個消費して\n" + name + "機能\nを開放しますか？";
        _production.ShowConfirm(message, () =>
        {
            if (_production.TryUnlockEnhance(id))
                Refresh();
        });
    }

    void Build(GameObject sizeSource)
    {
        if (Panel != null || sizeSource == null)
            return;

        Panel = new GameObject("PanelEnhance", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
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

        _headerStar = CreateGlowingStar(Panel.transform, "HeaderStar", -480f, 0f, 36f).GetComponent<RectTransform>();
        _starsHeld = CreateText("TextEnhanceStars", Panel.transform, 34, TextAnchor.MiddleLeft);
        PlaceLeft(_starsHeld.rectTransform, -448f, 0f, 200f, 48f);

        _whiteChip = CreateWhiteChip(Panel.transform, "ImageEnhanceWhiteChip");
        _whiteHeld = CreateText("TextEnhanceWhite", Panel.transform, 34, TextAnchor.MiddleLeft);

        BuildScroll();
        PlaceList();
        for (int i = 0; i < EnhanceSkillCatalog.Count; i++)
        {
            EnhanceSkillDef def = EnhanceSkillCatalog.GetAt(i);
            if (def != null)
                _rows.Add(CreateRow(def));
        }
    }

    // リスト下端はページ下から 10。上はヘッダーの下。
    void PlaceList()
    {
        if (Panel == null || _scrollRect == null)
            return;
        Canvas.ForceUpdateCanvases();
        RectTransform panelRect = Panel.GetComponent<RectTransform>();
        float panelHeight = panelRect.rect.height;
        if (panelHeight < 400f)
            return;

        float top = panelHeight * 0.5f - 16f;
        float bottom = -panelHeight * 0.5f + ListBottomPad;
        float headerY = top - HeaderHeight * 0.5f;
        if (_headerStar != null)
            Place(_headerStar, -480f, headerY, 84f, 84f);
        if (_starsHeld != null)
            PlaceLeft(_starsHeld.rectTransform, -448f, headerY, 200f, 48f);
        // 右端寄り: カラーチップ　所持数（チップと数字のあいだに余白）
        if (_whiteChip != null)
            Place(_whiteChip.rectTransform, 280f, headerY, 32f, 32f);
        if (_whiteHeld != null)
            PlaceLeft(_whiteHeld.rectTransform, 318f, headerY, 200f, 48f);

        float listTop = headerY - HeaderHeight * 0.5f - HeaderGap;
        float listHeight = listTop - bottom;
        if (listHeight < 200f)
            return;
        float listCenter = (listTop + bottom) * 0.5f;
        Place(_scrollRect, 0f, listCenter, 1040f, listHeight);
    }

    void BuildScroll()
    {
        GameObject scrollObject = new GameObject("EnhanceScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(Panel.transform, false);
        _scrollRect = scrollObject.GetComponent<RectTransform>();
        Place(_scrollRect, 0f, 0f, 1040f, 800f);

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
        grid.cellSize = new Vector2(RowWidth, RowHeight);
        grid.spacing = new Vector2(0f, RowGap);
        grid.padding = new RectOffset(16, 16, 16, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 1;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Vertical;

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

    SkillRow CreateRow(EnhanceSkillDef def)
    {
        GameObject row = new GameObject("EnhanceRow" + ((int)def.Id).ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        row.transform.SetParent(_content, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(RowWidth, RowHeight);
        Image background = row.GetComponent<Image>();
        background.color = RowBackground;
        EnsureWhiteFrame(row.transform, FrameThin);

        SkillRow entry = new SkillRow { Id = def.Id };
        entry.Icon = CreateSkillIcon(row.transform, def.Icon);
        Place(entry.Icon.rectTransform, -460f, 78f, 56f, 56f);
        entry.Name = CreateText("Name", row.transform, 40, TextAnchor.MiddleLeft);
        entry.Name.fontStyle = FontStyle.Bold;
        PlaceLeft(entry.Name.rectTransform, -420f, 78f, 620f, 48f);
        entry.Description = CreateText("Description", row.transform, 22, TextAnchor.MiddleLeft);
        PlaceLeft(entry.Description.rectTransform, -420f, 8f, 620f, 40f);
        entry.Description.color = new Color(0.82f, 0.84f, 0.9f, 1f);
        entry.Level = CreateText("Level", row.transform, 24, TextAnchor.MiddleLeft);
        PlaceLeft(entry.Level.rectTransform, -420f, -40f, 620f, 34f);
        entry.Level.supportRichText = true;
        entry.Effect = CreateText("Effect", row.transform, 24, TextAnchor.MiddleLeft);
        PlaceLeft(entry.Effect.rectTransform, -420f, -78f, 620f, 40f);

        entry.Action = CreateButton("ButtonAction", "解放", row.transform);
        Place(entry.Action.GetComponent<RectTransform>(), 380f, 0f, 180f, 180f);
        entry.ActionLabel = entry.Action.transform.Find("Text").GetComponent<Text>();
        // 下段: 解放 / 強化 / MAX
        Place(entry.ActionLabel.rectTransform, 0f, -40f, 180f, 48f);
        entry.ActionLabel.alignment = TextAnchor.MiddleCenter;

        // 上段: 星+数をグループ化して中央に置く
        GameObject costGroup = new GameObject("ButtonCostGroup", typeof(RectTransform));
        costGroup.transform.SetParent(entry.Action.transform, false);
        entry.ButtonCostGroup = costGroup.GetComponent<RectTransform>();
        Place(entry.ButtonCostGroup, 0f, 36f, 140f, 48f);
        entry.ButtonStar = CreateGlowingStar(costGroup.transform, "ButtonStar", -28f, 0f, 26f);
        entry.ButtonStar.SetActive(false);
        entry.ButtonWhiteChip = CreateWhiteChip(costGroup.transform, "ButtonWhiteChip");
        Place(entry.ButtonWhiteChip.rectTransform, -28f, 0f, 28f, 28f);
        entry.ButtonWhiteChip.gameObject.SetActive(false);
        entry.ButtonCost = CreateText("ButtonCost", costGroup.transform, 30, TextAnchor.MiddleLeft);
        PlaceLeft(entry.ButtonCost.rectTransform, 18f, 0f, 90f, 40f);
        entry.ButtonCost.text = "";

        if (_production != null)
            _production.StyleRepresentativeButton(entry.Action, Color.white, false);

        EnhanceSkillId id = def.Id;
        entry.Action.onClick.AddListener(() => PushAction(id));
        return entry;
    }

    Button CreateButton(string name, string label, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = Color.white;
        Button button = obj.AddComponent<Button>();
        Text text = CreateText("Text", obj.transform, 36, TextAnchor.MiddleCenter);
        Place(text.rectTransform, 0f, -40f, 180f, 48f);
        text.text = label;
        text.raycastTarget = false;
        return button;
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
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    Image CreateWhiteChip(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image chip = obj.GetComponent<Image>();
        chip.color = Color.white;
        chip.raycastTarget = false;
        ControllerProduction.StyleColorChip(chip);
        Place(chip.rectTransform, 0f, 0f, 32f, 32f);
        return chip;
    }

    Image CreateSkillIcon(Transform parent, EnhanceIconKind kind)
    {
        GameObject obj = new GameObject("SkillIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        if (kind == EnhanceIconKind.ChipR || kind == EnhanceIconKind.ChipG || kind == EnhanceIconKind.ChipB)
        {
            if (kind == EnhanceIconKind.ChipR)
                image.color = new Color(1f, 0.2f, 0.14f, 1f);
            else if (kind == EnhanceIconKind.ChipG)
                image.color = new Color(0.2f, 0.9f, 0.26f, 1f);
            else
                image.color = new Color(0.2f, 0.46f, 1f, 1f);
            ControllerProduction.StyleColorChip(image);
            return image;
        }

        string file = "RGB生産_1024x1024.png";
        if (kind == EnhanceIconKind.Pixel)
            file = "ピクセル生産_1024x1024.png";
        else if (kind == EnhanceIconKind.Character)
            file = "キャラ生産_1024x1024.png";
        else if (kind == EnhanceIconKind.Battle)
            file = "バトル_1024x1024.png";
        Sprite sprite = ControllerProduction.GetSceneFeatureIcon(file);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0.75f, 0.78f, 0.85f, 1f);
            ControllerProduction.StyleColorChip(image);
        }
        return image;
    }

    // ステージと同じ光る星。ぼかし → 外側の光 → 本体。
    static GameObject CreateGlowingStar(Transform parent, string name, float x, float y, float size)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Place(root.GetComponent<RectTransform>(), x, y, size + 48f, size + 48f);

        Image blur = CreateStarLayer(root.transform, "Blur", ChallengeMarks.StarBlur(), size + 48f, new Color(1f, 1f, 1f, 0.95f));
        Image glow = CreateStarLayer(root.transform, "Glow", ChallengeMarks.Star(), size + 28f, StarGlowLit);
        Image star = CreateStarLayer(root.transform, "Star", ChallengeMarks.Star(), size, StarLit);
        blur.rectTransform.anchoredPosition = Vector2.zero;
        glow.rectTransform.anchoredPosition = Vector2.zero;
        star.rectTransform.anchoredPosition = new Vector2(0f, 1f);
        return root;
    }

    static Image CreateStarLayer(Transform parent, string name, Sprite sprite, float size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = true;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;
        return image;
    }

    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    // 左端基準。星の右に数字を並べるとき用。
    static void PlaceLeft(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void EnsureWhiteFrame(Transform target, float thickness)
    {
        if (target == null)
            return;
        Transform existing = target.Find("WhiteFrame");
        if (existing != null)
        {
            SetFrameThickness(existing, thickness);
            return;
        }
        GameObject root = new GameObject("WhiteFrame", typeof(RectTransform));
        root.transform.SetParent(target, false);
        Stretch(root.GetComponent<RectTransform>());
        AddEdge(root.transform, "Top", true, true, thickness);
        AddEdge(root.transform, "Bottom", true, false, thickness);
        AddEdge(root.transform, "Left", false, false, thickness);
        AddEdge(root.transform, "Right", false, true, thickness);
    }

    static void SetFrameThickness(Transform frame, float thickness)
    {
        for (int i = 0; i < frame.childCount; i++)
        {
            RectTransform edge = frame.GetChild(i) as RectTransform;
            if (edge == null)
                continue;
            if (edge.name == "Top" || edge.name == "Bottom")
                edge.sizeDelta = new Vector2(0f, thickness);
            else
                edge.sizeDelta = new Vector2(thickness, 0f);
        }
    }

    static void AddEdge(Transform parent, string name, bool horizontal, bool end, float thickness)
    {
        GameObject edge = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        edge.transform.SetParent(parent, false);
        Image image = edge.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
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
}
