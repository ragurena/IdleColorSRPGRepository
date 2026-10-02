using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 残機合成。残機が1以上のキャラを並べ、消費後も1体残るキャラだけ選べる。
public class ControllerFusionClass : MonoBehaviour
{
    ControllerProduction _production;
    Font _font;
    Text _detail;
    Button _fuseButton;
    RectTransform _content;
    uint _selectedId;
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

        if (!selectedStillThere || !CanFuse(_production.GetCharacter(_selectedId)))
            _selectedId = FirstFusableId();
        ShowDetail();
    }

    uint FirstFusableId()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            CharacterClass character = _production.GetCharacter(_cells[i].Id);
            if (CanFuse(character))
                return character.ID;
        }
        return 0;
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
        if (!CanFuse(character))
            return;
        _selectedId = characterId;
        for (int i = 0; i < _cells.Count; i++)
        {
            CellEntry cell = _cells[i];
            if (cell.Background == null)
                continue;
            CharacterClass listed = _production.GetCharacter(cell.Id);
            cell.Background.color = CellColor(listed, cell.Id == _selectedId);
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
        bool canFuse = CanFuse(character);
        GameObject cell = new GameObject("FusionCell" + character.ID, typeof(RectTransform));
        cell.transform.SetParent(_content, false);
        Image background = cell.AddComponent<Image>();
        background.color = CellColor(character, character.ID == _selectedId);
        Button button = cell.AddComponent<Button>();
        button.targetGraphic = background;
        button.interactable = canFuse;
        uint id = character.ID;
        button.onClick.AddListener(() => Select(id));

        GameObject picture = new GameObject("Picture", typeof(RectTransform));
        picture.transform.SetParent(cell.transform, false);
        RectTransform pictureRect = picture.GetComponent<RectTransform>();
        pictureRect.anchorMin = new Vector2(0.22f, 0.48f);
        pictureRect.anchorMax = new Vector2(0.78f, 0.96f);
        pictureRect.offsetMin = Vector2.zero;
        pictureRect.offsetMax = Vector2.zero;
        Image image = picture.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.sprite = SpriteOf(character);
        image.color = canFuse ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);

        Text label = CreateText("Label", cell.transform, 16, TextAnchor.UpperCenter);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0.48f);
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, -2f);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.text = CellText(character);
        label.color = canFuse ? new Color(0.12f, 0.12f, 0.14f, 1f) : new Color(0.45f, 0.45f, 0.48f, 1f);

        return new CellEntry { Id = character.ID, Background = background, Picture = image, Label = label };
    }

    static string CellText(CharacterClass character)
    {
        long lives = ToLong(character.OwnedNumCur);
        long cost = FusionBonus.NextCost(ToLong(character.FusionCount));
        long need = cost >= long.MaxValue ? long.MaxValue : cost + 1;
        return character.Name + "\n"
            + "残機 " + lives.ToString() + "\n"
            + "必要 " + need.ToString() + "\n"
            + "減算 " + cost.ToString();
    }

    static Color CellColor(CharacterClass character, bool selected)
    {
        if (!CanFuse(character))
            return new Color(0.62f, 0.62f, 0.64f, 1f);
        if (selected)
            return new Color(0.55f, 0.72f, 0.9f, 1f);
        return new Color(0.82f, 0.82f, 0.86f, 1f);
    }

    void ShowDetail()
    {
        CharacterClass character = _production != null ? _production.GetCharacter(_selectedId) : null;
        bool canFuse = CanFuse(character);
        if (_fuseButton != null)
            _fuseButton.interactable = canFuse;
        if (_detail == null)
            return;
        if (!canFuse)
        {
            _detail.text = "残機が1体以上残るキャラを選んでください";
            return;
        }

        long count = ToLong(character.FusionCount);
        long lives = ToLong(character.OwnedNumCur);
        long cost = FusionBonus.NextCost(count);
        long grownHp = Grown(character.Stats[1].HPMax, character.Stats[2].HPMax);
        long grownAtk = Grown(character.Stats[1].ATK, character.Stats[2].ATK);
        long grownDef = Grown(character.Stats[1].DEF, character.Stats[2].DEF);
        long grownR = Grown(character.Stats[1].RCreates, character.Stats[2].RCreates);
        long grownG = Grown(character.Stats[1].GCreates, character.Stats[2].GCreates);
        long grownB = Grown(character.Stats[1].BCreates, character.Stats[2].BCreates);
        long hp = (long)character.Stats[0].HPMax;
        long atk = (long)character.Stats[0].ATK;
        long def = (long)character.Stats[0].DEF;
        long createR = (long)character.Stats[0].RCreates;
        long createG = (long)character.Stats[0].GCreates;
        long createB = (long)character.Stats[0].BCreates;
        _detail.text = character.Name + "\n"
            + "合成 " + count.ToString() + "回（+" + FusionBonus.Percent(count).ToString() + "%）\n"
            + "次は " + cost.ToString() + "体で +" + FusionBonus.PercentPerFusion.ToString() + "%（+" + FusionBonus.Percent(count + 1).ToString() + "%）\n"
            + "残機 " + lives.ToString() + " → " + (lives - cost).ToString() + "\n"
            + "HP " + hp.ToString() + " → " + NextStat(hp, grownHp, count).ToString() + "\n"
            + "ATK " + atk.ToString() + " → " + NextStat(atk, grownAtk, count).ToString() + "\n"
            + "DEF " + def.ToString() + " → " + NextStat(def, grownDef, count).ToString() + "\n"
            + "R " + createR.ToString() + " → " + NextStat(createR, grownR, count).ToString() + "\n"
            + "G " + createG.ToString() + " → " + NextStat(createG, grownG, count).ToString() + "\n"
            + "B " + createB.ToString() + " → " + NextStat(createB, grownB, count).ToString();
    }

    static long NextStat(long current, long grown, long fusionCount)
    {
        long next = current - FusionBonus.Bonus(grown, fusionCount) + FusionBonus.Bonus(grown, fusionCount + 1);
        if (next < 0)
            next = 0;
        return next;
    }

    static long Grown(ulong baseStat, ulong levelStat)
    {
        long grown = ToLong(baseStat) + ToLong(levelStat);
        if (grown < 0)
            grown = long.MaxValue;
        return grown;
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
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
        Panel.GetComponent<Image>().color = new Color(0.93f, 0.93f, 0.96f, 1f);
        Panel.GetComponent<Image>().raycastTarget = true;
        CanvasGroup group = Panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Text title = CreateText("TextFusionTitle", Panel.transform, 40, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 0f, 640f, 1000f, 56f);
        title.text = "合成";

        BuildScroll();

        GameObject detailRoot = new GameObject("FusionDetail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        detailRoot.transform.SetParent(Panel.transform, false);
        Place(detailRoot.GetComponent<RectTransform>(), 0f, -430f, 1020f, 300f);
        detailRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);

        _detail = CreateText("TextFusionDetail", detailRoot.transform, 22, TextAnchor.UpperLeft);
        Place(_detail.rectTransform, 0f, 8f, 960f, 270f);
        _detail.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detail.verticalOverflow = VerticalWrapMode.Truncate;

        _fuseButton = CreateButton("ButtonFusion", "合成", new Color(0.25f, 0.48f, 0.75f, 1f));
        Place(_fuseButton.GetComponent<RectTransform>(), 0f, -640f, 280f, 64f);
        _fuseButton.onClick.AddListener(PushFuse);
    }

    void BuildScroll()
    {
        GameObject scrollObject = new GameObject("FusionScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(Panel.transform, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        Place(scrollRect, 0f, 170f, 1040f, 760f);

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
        grid.cellSize = new Vector2(240f, 280f);
        grid.spacing = new Vector2(16f, 16f);
        grid.padding = new RectOffset(16, 16, 16, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
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
    }

    Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.12f, 0.12f, 0.14f, 1f);
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
