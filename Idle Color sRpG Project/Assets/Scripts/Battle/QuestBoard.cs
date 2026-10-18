using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 画面の一番手前に出すクエスト一覧。バトル画面が閉じていても開ける。行は定義の数だけ作る。
public sealed class QuestBoard
{
    ControllerProduction _production;
    Font _font;
    GameObject _root;
    RectTransform _content;
    ScrollRect _scroll;
    int _choiceId;
    readonly List<QuestDef> _visible = new List<QuestDef>();
    readonly List<Row> _rows = new List<Row>();

    public bool IsOpen
    {
        get { return _root != null && _root.activeSelf; }
    }

    public void Build(Transform parent, Font font, ControllerProduction production)
    {
        _production = production;
        _font = font;
        _root = new GameObject("PanelQuest", typeof(RectTransform), typeof(Image));
        _root.transform.SetParent(parent, false);
        Image background = _root.GetComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        Stretch(_root.GetComponent<RectTransform>());

        Text title = CreateText("TextQuestTitle", _root.transform, 32, TextAnchor.MiddleCenter);
        title.text = "クエスト";
        title.color = Color.white;
        PlaceTop(title.rectTransform, 16f, 0f, 400f, 48f);

        Button close = CreateButton("ButtonQuestClose", _root.transform, "閉じる", new Color(0.75f, 0.28f, 0.24f, 1f));
        PlaceTop(close.GetComponent<RectTransform>(), 12f, 430f, 140f, 68f);
        close.onClick.AddListener(Close);

        GameObject scrollObject = new GameObject("QuestScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(_root.transform, false);
        Image scrollImage = scrollObject.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0.15f);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(24f, 24f);
        scrollRect.offsetMax = new Vector2(-24f, -88f);
        _scroll = scrollObject.GetComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 40f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Stretch(viewport.GetComponent<RectTransform>());
        _scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        _content = content.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _scroll.content = _content;

        _root.SetActive(false);
    }

    public void Open()
    {
        if (_root == null || _production == null)
            return;
        _choiceId = 0;
        _root.SetActive(true);
        Transform overlay = _root.transform.parent;
        if (overlay != null && overlay.parent != null)
            overlay.SetAsLastSibling();
        _root.transform.SetAsLastSibling();
        Rebuild(true);
    }

    public void Close()
    {
        _choiceId = 0;
        if (_root != null)
            _root.SetActive(false);
    }

    public void Refresh()
    {
        if (!IsOpen || _production == null)
            return;
        Rebuild(false);
    }

    void Rebuild(bool toTop)
    {
        float position = 1f;
        if (!toTop && _scroll != null)
            position = _scroll.verticalNormalizedPosition;
        _production.CollectVisibleQuests(_visible);
        if (_choiceId != 0 && !StillChoosing())
            _choiceId = 0;
        bool rebuilt = !SameOrder();
        if (rebuilt)
            BuildRows();
        UpdateRows();
        if (toTop || rebuilt)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            if (_scroll != null)
                _scroll.verticalNormalizedPosition = toTop ? 1f : position;
        }
    }

    bool StillChoosing()
    {
        for (int i = 0; i < _visible.Count; i++)
        {
            if (_visible[i].Id == _choiceId && _production.IsQuestClaimable(_visible[i]))
                return true;
        }
        return false;
    }

    bool SameOrder()
    {
        if (_rows.Count != _visible.Count)
            return false;
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].Id != _visible[i].Id)
                return false;
        }
        return true;
    }

    void BuildRows()
    {
        for (int i = _content.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(_content.GetChild(i).gameObject);
        _rows.Clear();
        for (int i = 0; i < _visible.Count; i++)
            _rows.Add(CreateRow(_visible[i]));
    }

    void UpdateRows()
    {
        for (int i = 0; i < _rows.Count; i++)
            Paint(_rows[i], _visible[i]);
    }

    Row CreateRow(QuestDef def)
    {
        GameObject rowObject = new GameObject("QuestRow" + def.Id.ToString(), typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rowObject.transform.SetParent(_content, false);
        Image background = rowObject.GetComponent<Image>();
        background.color = new Color(0.16f, 0.18f, 0.24f, 1f);
        LayoutElement element = rowObject.GetComponent<LayoutElement>();
        element.minHeight = 196f;
        element.preferredHeight = 196f;

        Row row = new Row();
        row.Id = def.Id;
        row.Title = CreateText("Title", rowObject.transform, 26, TextAnchor.MiddleLeft);
        row.Title.color = Color.white;
        StretchTop(row.Title.rectTransform, 16f, 240f, 10f, 32f);
        row.Reward = CreateText("Reward", rowObject.transform, 20, TextAnchor.UpperLeft);
        row.Reward.color = new Color(1f, 0.93f, 0.72f, 1f);
        row.Reward.horizontalOverflow = HorizontalWrapMode.Wrap;
        StretchTop(row.Reward.rectTransform, 16f, 240f, 44f, 76f);
        row.Fraction = CreateText("Fraction", rowObject.transform, 22, TextAnchor.MiddleLeft);
        row.Fraction.color = Color.white;
        StretchTop(row.Fraction.rectTransform, 16f, 240f, 128f, 28f);
        row.Bar = CreateBar(rowObject.transform);
        StretchTop(row.Bar.GetComponent<RectTransform>(), 16f, 240f, 158f, 22f);

        row.Claim = CreateButton("Claim", rowObject.transform, "受け取る", new Color(0.28f, 0.55f, 0.34f, 1f));
        RectTransform claimRect = row.Claim.GetComponent<RectTransform>();
        claimRect.anchorMin = new Vector2(1f, 1f);
        claimRect.anchorMax = new Vector2(1f, 1f);
        claimRect.pivot = new Vector2(1f, 1f);
        claimRect.anchoredPosition = new Vector2(-12f, -78f);
        claimRect.sizeDelta = new Vector2(210f, 100f);
        row.ClaimLabel = row.Claim.GetComponentInChildren<Text>();
        ColorBlock claimColors = row.Claim.colors;
        claimColors.disabledColor = Color.white;
        row.Claim.colors = claimColors;
        int claimId = def.Id;
        row.Claim.onClick.AddListener(() => PushClaim(claimId));

        row.Lock = CreateButton("Lock", rowObject.transform, "ロック", new Color(0.28f, 0.30f, 0.36f, 1f));
        RectTransform lockRect = row.Lock.GetComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(1f, 1f);
        lockRect.anchorMax = new Vector2(1f, 1f);
        lockRect.pivot = new Vector2(1f, 1f);
        lockRect.anchoredPosition = new Vector2(-12f, -10f);
        lockRect.sizeDelta = new Vector2(120f, 52f);
        row.LockLabel = row.Lock.GetComponentInChildren<Text>();
        int lockId = def.Id;
        row.Lock.onClick.AddListener(() => ToggleLock(lockId));

        GameObject choices = new GameObject("Choices", typeof(RectTransform));
        choices.transform.SetParent(rowObject.transform, false);
        RectTransform choiceRect = choices.GetComponent<RectTransform>();
        choiceRect.anchorMin = new Vector2(1f, 1f);
        choiceRect.anchorMax = new Vector2(1f, 1f);
        choiceRect.pivot = new Vector2(1f, 1f);
        choiceRect.anchoredPosition = new Vector2(-12f, -78f);
        choiceRect.sizeDelta = new Vector2(210f, 100f);
        row.PixelChoice = CreateButton("Pixel", choices.transform, "ピクセル生成枠+3", new Color(0.25f, 0.48f, 0.72f, 1f));
        PlaceChoice(row.PixelChoice.GetComponent<RectTransform>(), 0f);
        row.CharacterChoice = CreateButton("Character", choices.transform, "キャラ生成枠+3", new Color(0.72f, 0.42f, 0.22f, 1f));
        PlaceChoice(row.CharacterChoice.GetComponent<RectTransform>(), 52f);
        int choiceQuest = def.Id;
        row.PixelChoice.onClick.AddListener(() => ChooseReward(choiceQuest, 1));
        row.CharacterChoice.onClick.AddListener(() => ChooseReward(choiceQuest, 2));
        row.Choices = choices;
        return row;
    }

    void Paint(Row row, QuestDef def)
    {
        bool cleared = _production.IsQuestCleared(def);
        bool claimed = _production.IsQuestClaimed(def);
        bool claimable = cleared && !claimed;
        bool choosing = claimable && QuestCatalog.ChoosesProductionSlot(def) && _choiceId == def.Id;
        QuestRecord record;
        int choice = 0;
        if (_production.TryQuestRecord(def.Id, out record) && record != null)
            choice = record.Choice;
        row.Title.text = def.Title;
        row.Reward.text = QuestCatalog.RewardText(def, claimed, choice);
        row.Fraction.text = _production.QuestFraction(def);
        row.Bar.value = _production.QuestBar(def);

        string warning = claimable ? _production.QuestWarning(def, 0) : "";
        row.ClaimLabel.text = warning.Length == 0 ? "受け取る" : "受け取る\n" + warning;
        if (claimed)
            row.ClaimLabel.text = "受取済";
        row.Claim.interactable = claimable;
        row.Claim.GetComponent<Image>().color = claimable
            ? new Color(0.28f, 0.55f, 0.34f, 1f)
            : new Color(0.35f, 0.35f, 0.38f, 1f);
        row.Claim.gameObject.SetActive(!choosing);
        row.Choices.SetActive(choosing);
        bool chainLocked = _production.IsQuestChainLocked(def);
        row.LockLabel.text = chainLocked ? "ロック中" : "ロック";
        row.Lock.GetComponent<Image>().color = chainLocked
            ? new Color(0.72f, 0.52f, 0.16f, 1f)
            : new Color(0.28f, 0.30f, 0.36f, 1f);
        if (choosing)
        {
            Text pixel = row.PixelChoice.GetComponentInChildren<Text>();
            Text character = row.CharacterChoice.GetComponentInChildren<Text>();
            pixel.text = QuestCatalog.ChoiceLabel(def, 1, _production.QuestWarning(def, 1));
            character.text = QuestCatalog.ChoiceLabel(def, 2, _production.QuestWarning(def, 2));
        }
    }

    void PushClaim(int id)
    {
        QuestDef def = QuestCatalog.Find(id);
        if (def == null || _production == null || !_production.IsQuestClaimable(def))
            return;
        if (QuestCatalog.ChoosesProductionSlot(def))
        {
            _choiceId = id;
            Refresh();
            return;
        }
        _production.ClaimQuest(id, 0);
        Refresh();
    }

    void ToggleLock(int id)
    {
        if (_production == null)
            return;
        _production.ToggleQuestLock(id);
        Refresh();
    }

    void ChooseReward(int id, int choice)
    {
        if (_production == null || !_production.ClaimQuest(id, choice))
            return;
        _choiceId = 0;
        Refresh();
    }

    Slider CreateBar(Transform parent)
    {
        GameObject trackObject = new GameObject("Bar", typeof(RectTransform), typeof(Image), typeof(Slider));
        trackObject.transform.SetParent(parent, false);
        Image track = trackObject.GetComponent<Image>();
        track.color = new Color(0.08f, 0.08f, 0.1f, 1f);
        track.raycastTarget = false;
        Slider slider = trackObject.GetComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(trackObject.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillArea.transform, false);
        Image fill = fillObject.GetComponent<Image>();
        fill.color = new Color(0.32f, 0.72f, 0.4f, 1f);
        fill.raycastTarget = false;
        Stretch(fill.rectTransform);
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = track;
        ColorBlock colors = slider.colors;
        colors.disabledColor = Color.white;
        slider.colors = colors;
        return slider;
    }

    Button CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        Button button = obj.GetComponent<Button>();
        Text text = CreateText("Text", obj.transform, 18, TextAnchor.MiddleCenter);
        text.text = label;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        Stretch(text.rectTransform);
        return button;
    }

    Text CreateText(string name, Transform parent, int size, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    static void PlaceChoice(RectTransform rect, float y)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -y);
        rect.sizeDelta = new Vector2(0f, 48f);
    }

    static void PlaceTop(RectTransform rect, float yFromTop, float x, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -yFromTop);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void StretchTop(RectTransform rect, float left, float right, float yFromTop, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -(yFromTop + height));
        rect.offsetMax = new Vector2(-right, -yFromTop);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    sealed class Row
    {
        public int Id;
        public Text Title;
        public Text Reward;
        public Text Fraction;
        public Slider Bar;
        public Button Claim;
        public Text ClaimLabel;
        public Button Lock;
        public Text LockLabel;
        public GameObject Choices;
        public Button PixelChoice;
        public Button CharacterChoice;
    }
}
