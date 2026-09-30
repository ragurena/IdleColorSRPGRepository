using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 図鑑。最大所持数が 1 以上、または倒したことがあるキャラは中身を出す。それ以外はシルエット。
public class ControllerCatalogClass : MonoBehaviour
{
    ControllerProduction _production;
    Font _font;
    Text _count;
    Text _detail;
    Image _detailImage;
    RectTransform _content;
    uint _selectedId;
    readonly Dictionary<uint, Sprite> _openedSprites = new Dictionary<uint, Sprite>();
    readonly List<CellEntry> _cells = new List<CellEntry>();

    class CellEntry
    {
        public uint Id;
        public Image Background;
        public Image Picture;
    }

    public GameObject Panel { get; private set; }

    public void Initialize(ControllerProduction production, GameObject sizeSource)
    {
        _production = production;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Build(sizeSource);
        Open();
    }

    public void Open()
    {
        if (_selectedId == 0)
            _selectedId = FirstId();
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

        int opened = 0;
        int total = 0;
        CharacterDefinition[] roster = CharacterRoster.All;
        for (int i = 0; i < roster.Length; i++)
        {
            CharacterClass character = _production.GetCharacter(roster[i].Id);
            if (character == null || character.ID != roster[i].Id)
                continue;
            total++;
            if (IsKnown(character))
                opened++;
            _cells.Add(CreateCell(character));
        }

        if (_count != null)
            _count.text = "開いた " + opened.ToString() + " / " + total.ToString();
        ShowDetail();
    }

    uint FirstId()
    {
        CharacterDefinition[] roster = CharacterRoster.All;
        for (int i = 0; i < roster.Length; i++)
        {
            CharacterClass character = _production.GetCharacter(roster[i].Id);
            if (character != null && character.ID == roster[i].Id)
                return roster[i].Id;
        }
        return 0;
    }

    void Select(uint characterId)
    {
        _selectedId = characterId;
        for (int i = 0; i < _cells.Count; i++)
        {
            CellEntry cell = _cells[i];
            if (cell.Background == null)
                continue;
            cell.Background.color = cell.Id == _selectedId
                ? new Color(0.55f, 0.72f, 0.9f, 1f)
                : new Color(0.82f, 0.82f, 0.86f, 1f);
        }
        ShowDetail();
    }

    CellEntry CreateCell(CharacterClass character)
    {
        GameObject cell = new GameObject("CatalogCell" + character.ID, typeof(RectTransform));
        cell.transform.SetParent(_content, false);
        Image background = cell.AddComponent<Image>();
        background.color = character.ID == _selectedId
            ? new Color(0.55f, 0.72f, 0.9f, 1f)
            : new Color(0.82f, 0.82f, 0.86f, 1f);
        Button button = cell.AddComponent<Button>();
        button.targetGraphic = background;
        uint id = character.ID;
        button.onClick.AddListener(() => Select(id));

        GameObject picture = new GameObject("Picture", typeof(RectTransform));
        picture.transform.SetParent(cell.transform, false);
        RectTransform pictureRect = picture.GetComponent<RectTransform>();
        pictureRect.anchorMin = new Vector2(0.12f, 0.12f);
        pictureRect.anchorMax = new Vector2(0.88f, 0.88f);
        pictureRect.offsetMin = Vector2.zero;
        pictureRect.offsetMax = Vector2.zero;
        Image image = picture.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.sprite = SpriteOf(character);
        image.color = IsKnown(character) ? Color.white : Color.black;
        return new CellEntry { Id = character.ID, Background = background, Picture = image };
    }

    void ShowDetail()
    {
        CharacterClass character = _production != null ? _production.GetCharacter(_selectedId) : null;
        if (character == null || character.ID != _selectedId)
        {
            if (_detailImage != null)
                _detailImage.sprite = null;
            if (_detail != null)
                _detail.text = "キャラを選んでください";
            return;
        }

        if (_detailImage != null)
        {
            _detailImage.sprite = SpriteOf(character);
            _detailImage.color = IsKnown(character) ? Color.white : Color.black;
            _detailImage.preserveAspect = true;
        }

        if (_detail == null)
            return;

        if (!IsKnown(character))
        {
            _detail.text = "？？？\nまだ所持していない";
            return;
        }

        StatisticsClass stats = character.Stats[0];
        _detail.text = character.Name + "\n"
            + "Lv " + character.Level + "\n"
            + TypeName(character.CharacterType) + "    弱点 " + TypeName(character.WeaknessType) + "\n"
            + "HP " + stats.HPMax + "\n"
            + "ATK " + stats.ATK + "    DEF " + stats.DEF + "\n"
            + "SPD " + stats.SPD + "    LUC " + stats.LUC + "\n"
            + "代表 " + character.RepresentativeR + ", " + character.RepresentativeG + ", " + character.RepresentativeB + "\n"
            + "弱点色 " + character.WeaknessR + ", " + character.WeaknessG + ", " + character.WeaknessB;
    }

    static bool IsKnown(CharacterClass character)
    {
        return character.OwnedNumMax != 0 || character.CatalogOpened;
    }

    Sprite SpriteOf(CharacterClass character)
    {
        return OpenedSprite(character);
    }

    Sprite OpenedSprite(CharacterClass character)
    {
        Sprite cached;
        if (_openedSprites.TryGetValue(character.ID, out cached) && cached != null)
            return cached;

        Texture2D tex = character.ImageTexture2D;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        tex.filterMode = FilterMode.Point;
        int size = character.Size > 0 ? character.Size : tex.width;
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        _openedSprites[character.ID] = sprite;
        return sprite;
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

    void Build(GameObject sizeSource)
    {
        if (Panel != null || sizeSource == null)
            return;

        Panel = new GameObject("PanelCatalog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
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

        Text title = CreateText("TextCatalogTitle", Panel.transform, 40, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 0f, 610f, 1000f, 56f);
        title.text = "図鑑";

        _count = CreateText("TextCatalogCount", Panel.transform, 28, TextAnchor.MiddleCenter);
        Place(_count.rectTransform, 0f, 555f, 1000f, 40f);

        BuildScroll();

        GameObject detailRoot = new GameObject("CatalogDetail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        detailRoot.transform.SetParent(Panel.transform, false);
        RectTransform detailRect = detailRoot.GetComponent<RectTransform>();
        Place(detailRect, 0f, -525f, 1020f, 270f);
        detailRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);

        GameObject detailPicture = new GameObject("CatalogDetailPicture", typeof(RectTransform));
        detailPicture.transform.SetParent(detailRoot.transform, false);
        RectTransform detailPictureRect = detailPicture.GetComponent<RectTransform>();
        Place(detailPictureRect, -380f, 0f, 190f, 190f);
        _detailImage = detailPicture.AddComponent<Image>();
        _detailImage.preserveAspect = true;
        _detailImage.raycastTarget = false;

        _detail = CreateText("TextCatalogDetail", detailRoot.transform, 24, TextAnchor.UpperLeft);
        Place(_detail.rectTransform, 80f, 0f, 720f, 250f);
        _detail.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detail.verticalOverflow = VerticalWrapMode.Truncate;
    }

    void BuildScroll()
    {
        GameObject scrollObject = new GameObject("CatalogScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(Panel.transform, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        Place(scrollRect, 0f, 55f, 1040f, 860f);

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
        grid.cellSize = new Vector2(240f, 240f);
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
