using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 図鑑。最大所持数が 1 以上、または倒したことがあるキャラは中身を出す。それ以外はシルエット。
public class ControllerCatalogClass : MonoBehaviour
{
    ControllerProduction _production;
    Font _font;
    Text _count;
    Text _pageButtonLabel;
    Text _detail;
    Text _detailName;
    Text _detailStatus;
    GameObject _repRow;
    GameObject _weakRow;
    Image _repChip;
    Image _weakChip;
    RgbDigits _repDigits;
    RgbDigits _weakDigits;

    struct RgbDigits
    {
        public Text Red;
        public Text Green;
        public Text Blue;
    }
    Image _detailImage;
    RectTransform _detailPictureRect;
    GameObject _personBoard;
    Text _personName;
    Text _personSpecialty;
    PersonSkillRow[] _personRows;
    Sprite _whiteSprite;
    CharacterStatGraph _stats;
    RectTransform _content;
    uint _selectedId;
    int _page;
    int _selectedPersonId = PersonIds.Protagonist;
    const float FrameThin = 3f;
    const float FrameSelected = 8f;
    readonly Dictionary<uint, Sprite> _openedSprites = new Dictionary<uint, Sprite>();
    readonly Dictionary<uint, Sprite> _silhouetteSprites = new Dictionary<uint, Sprite>();
    readonly List<CellEntry> _cells = new List<CellEntry>();

    class CellEntry
    {
        public uint Id;
        public Image Background;
        public Image Picture;
    }

    class PersonSkillRow
    {
        public Text Level;
        public Image Fill;
        public Text Value;
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
        if (_page == 1)
        {
            RefreshPeople();
            return;
        }

        for (int i = 0; i < _cells.Count; i++)
        {
            if (_cells[i].Background != null)
                Destroy(_cells[i].Background.gameObject);
        }
        _cells.Clear();

        int joined = 0;
        int total = 0;
        CharacterDefinition[] roster = CharacterRoster.All;
        for (int i = 0; i < roster.Length; i++)
        {
            CharacterClass character = _production.GetCharacter(roster[i].Id);
            if (character == null || character.ID != roster[i].Id)
                continue;
            total++;
            if (character.OwnedNumMax != 0)
                joined++;
            _cells.Add(CreateCell(character));
        }

        ImportedCharacters.ForEach(character =>
        {
            if (character == null || character.ID < ImportedCharacters.FirstId)
                return;
            total++;
            if (character.OwnedNumMax != 0)
                joined++;
            _cells.Add(CreateCell(character));
        });

        if (_count != null)
            _count.text = "仲間にした数 " + UiNumber.Format(joined) + " / " + UiNumber.Format(total);
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
        if (_production != null)
            _production.ClearCatalogNew(characterId);
        for (int i = 0; i < _cells.Count; i++)
        {
            CellEntry cell = _cells[i];
            if (cell.Background == null)
                continue;
            cell.Background.color = CellFill(cell.Id == _selectedId);
            EnsureWhiteFrame(cell.Background.transform, cell.Id == _selectedId ? FrameSelected : FrameThin);
        }
        ShowDetail();
    }

    CellEntry CreateCell(CharacterClass character)
    {
        GameObject cell = new GameObject("CatalogCell" + character.ID, typeof(RectTransform));
        cell.transform.SetParent(_content, false);
        Image background = cell.AddComponent<Image>();
        background.color = CellFill(character.ID == _selectedId);
        Button button = cell.AddComponent<Button>();
        button.targetGraphic = background;
        uint id = character.ID;
        button.onClick.AddListener(() => Select(id));

        bool known = IsKnown(character);
        GameObject picture = new GameObject("Picture", typeof(RectTransform));
        picture.transform.SetParent(cell.transform, false);
        RectTransform pictureRect = picture.GetComponent<RectTransform>();
        // オーラは絵の枠いっぱいに伸びる。キャラは正方形なので、枠も正方形にする。
        pictureRect.anchorMin = new Vector2(0.195f, 0.36f);
        pictureRect.anchorMax = new Vector2(0.805f, 0.97f);
        pictureRect.offsetMin = Vector2.zero;
        pictureRect.offsetMax = Vector2.zero;
        Image image = picture.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.sprite = SpriteOf(character);
        image.color = Color.white;
        ApplyDarkAura(picture.transform, character, known);
        CatalogNewMark.Apply(picture.transform, character);
        CreateCellStatus(cell.transform, character, known);
        EnsureWhiteFrame(cell.transform, character.ID == _selectedId ? FrameSelected : FrameThin);
        return new CellEntry { Id = character.ID, Background = background, Picture = image };
    }

    void CreateCellStatus(Transform parent, CharacterClass character, bool known)
    {
        Text label = CreateText("Status", parent, 22, TextAnchor.MiddleCenter);
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0.34f);
        rect.offsetMin = new Vector2(4f, 2f);
        rect.offsetMax = new Vector2(-4f, -2f);
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.text = CellStatusLabel(character, known);
    }

    // 出会っているだけで仲間にしたことがないキャラは、レベルと合成回数を出さない。
    string CellStatusLabel(CharacterClass character, bool known)
    {
        if (!known)
            return "？？？\n？？？";
        if (character.OwnedNumMax == 0)
            return "未所持";
        return CellStatusText(character);
    }

    string CellStatusText(CharacterClass character)
    {
        long level = ToLong(character.Level);
        if (level < 1)
            level = 1;
        long levelCap = 0;
        BattleBalanceConfig config = _production != null ? _production.GetBattleBalance() : null;
        if (config != null)
            levelCap = config.MaxLevel;
        long fusion = ToLong(character.FusionCount);
        long fusionCap = FusionBonus.MaxCount;
        string levelText = UiNumber.LevelText("Lv " + UiNumber.Format(level), level, levelCap);
        string fusionText = UiNumber.LevelText("合成 " + UiNumber.Format(fusion) + "回", fusion, fusionCap);
        return levelText + "\n" + fusionText;
    }

    string DetailStatusText(CharacterClass character)
    {
        long level = ToLong(character.Level);
        if (level < 1)
            level = 1;
        long levelCap = 0;
        BattleBalanceConfig config = _production != null ? _production.GetBattleBalance() : null;
        if (config != null)
            levelCap = config.MaxLevel;
        long fusion = ToLong(character.FusionCount);
        long fusionCap = FusionBonus.MaxCount;
        string fusionText = UiNumber.LevelText("合成 " + UiNumber.Format(fusion) + "回", fusion, fusionCap);
        string levelText = UiNumber.LevelText("Lv " + UiNumber.Format(level), level, levelCap);
        return levelText + "\n" + fusionText;
    }

    static long ToLong(ulong value)
    {
        if (value > (ulong)long.MaxValue)
            return long.MaxValue;
        return (long)value;
    }

    void ShowDetail()
    {
        if (_page == 1)
        {
            ShowPersonDetail();
            return;
        }
        UseCharacterLayout();
        CharacterClass character = _production != null ? _production.GetCharacter(_selectedId) : null;
        if (character == null || character.ID != _selectedId)
        {
            if (_detailImage != null)
            {
                _detailImage.sprite = null;
                _detailImage.color = Color.white;
                _detailImage.preserveAspect = true;
                CatalogNewMark.Apply(_detailImage.transform, null);
            }
            if (_detailName != null)
                _detailName.text = "";
            if (_detailStatus != null)
                _detailStatus.text = "";
            ShowColorRows(false);
            if (_detailImage != null)
                ControllerProduction.RemoveAttributeGlow(_detailImage.transform);
            ShowStatusNote("キャラを選んでください");
            return;
        }

        if (_detailImage != null)
        {
            _detailImage.sprite = SpriteOf(character);
            _detailImage.color = Color.white;
            _detailImage.preserveAspect = true;
            ApplyDarkAura(_detailImage.transform, character, IsKnown(character));
            CatalogNewMark.Apply(_detailImage.transform, character);
        }

        if (!IsKnown(character))
        {
            if (_detailName != null)
                _detailName.text = "？？？";
            ShowColorRows(false);
            if (_detailStatus != null)
                _detailStatus.text = "？？？\n？？？";
            ShowStatusNote("まだ所持していない");
            return;
        }

        if (_detailName != null)
            _detailName.text = character.Name;
        ShowColorRows(true);
        PaintColorRow(_repChip, _repDigits, character.RepresentativeR, character.RepresentativeG, character.RepresentativeB);
        PaintColorRow(_weakChip, _weakDigits, character.WeaknessR, character.WeaknessG, character.WeaknessB);
        if (_detailStatus != null)
            _detailStatus.text = DetailStatusText(character);
        if (_detail != null)
            _detail.gameObject.SetActive(false);
        if (_stats != null)
        {
            _stats.SetShown(true);
            _stats.Paint(character, _production != null ? _production.GetBattleBalance() : null);
        }
    }

    void UseCharacterLayout()
    {
        if (_detailPictureRect != null)
            Place(_detailPictureRect, -341f, 144f, 220f, 220f);
        if (_detailName != null)
            _detailName.gameObject.SetActive(true);
        if (_detailStatus != null)
            _detailStatus.gameObject.SetActive(true);
        if (_personBoard != null)
            _personBoard.SetActive(false);
        if (_detailImage != null)
            ControllerProduction.RemovePersonAttributeFrame(_detailImage.transform);
    }

    GameObject CreateColorRow(Transform parent, string name, string caption, float y, out Image chip, out RgbDigits digits)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        Place(row.GetComponent<RectTransform>(), -341f, y, 290f, 34f);

        Text captionText = CreateText("Caption", row.transform, 18, TextAnchor.MiddleRight);
        Place(captionText.rectTransform, -98f, 0f, 52f, 34f);
        captionText.text = caption;

        GameObject chipObject = new GameObject("Chip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        chipObject.transform.SetParent(row.transform, false);
        chip = chipObject.GetComponent<Image>();
        chip.raycastTarget = false;
        Place(chip.rectTransform, -46f, 0f, 24f, 24f);

        digits = new RgbDigits();
        digits.Red = CreateRgbDigit(row.transform, "Red", 10f, new Color(1f, 0.2f, 0.14f, 0.9f));
        CreateRgbComma(row.transform, 36f);
        digits.Green = CreateRgbDigit(row.transform, "Green", 62f, new Color(0.2f, 0.9f, 0.26f, 0.9f));
        CreateRgbComma(row.transform, 88f);
        digits.Blue = CreateRgbDigit(row.transform, "Blue", 114f, new Color(0.2f, 0.46f, 1f, 0.9f));
        return row;
    }

    Text CreateRgbDigit(Transform parent, string name, float x, Color glow)
    {
        Text text = CreateText(name, parent, 18, TextAnchor.MiddleRight);
        Place(text.rectTransform, x, 0f, 36f, 34f);
        text.color = new Color(0.93f, 0.94f, 0.96f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        GaugeTextGlow mark = text.gameObject.AddComponent<GaugeTextGlow>();
        mark.Glow = glow;
        mark.Spread = 1.8f;
        return text;
    }

    void CreateRgbComma(Transform parent, float x)
    {
        Text text = CreateText("Comma", parent, 18, TextAnchor.MiddleCenter);
        Place(text.rectTransform, x, 0f, 14f, 34f);
        text.color = new Color(0.93f, 0.94f, 0.96f, 1f);
        text.text = ",";
    }

    void ShowColorRows(bool shown)
    {
        if (_repRow != null)
            _repRow.SetActive(shown);
        if (_weakRow != null)
            _weakRow.SetActive(shown);
    }

    static void PaintColorRow(Image chip, RgbDigits digits, int red, int green, int blue)
    {
        red = ClampByte(red);
        green = ClampByte(green);
        blue = ClampByte(blue);
        if (chip != null)
        {
            chip.color = new Color(red / 255f, green / 255f, blue / 255f, 1f);
            ControllerProduction.StyleColorChip(chip);
        }
        if (digits.Red != null)
            digits.Red.text = red.ToString();
        if (digits.Green != null)
            digits.Green.text = green.ToString();
        if (digits.Blue != null)
            digits.Blue.text = blue.ToString();
    }

    static int ClampByte(int value)
    {
        if (value < 0)
            return 0;
        if (value > 255)
            return 255;
        return value;
    }

    void UsePersonLayout()
    {
        if (_detailPictureRect != null)
            Place(_detailPictureRect, -373f, 70f, 230f, 230f);
        if (_detailName != null)
            _detailName.gameObject.SetActive(false);
        if (_detailStatus != null)
            _detailStatus.gameObject.SetActive(false);
        if (_detailImage != null)
            ControllerProduction.RemoveAttributeGlow(_detailImage.transform);
        ShowColorRows(false);
        if (_stats != null)
        {
            _stats.Clear();
            _stats.SetShown(false);
        }
        if (_detail != null)
            _detail.gameObject.SetActive(false);
        if (_personBoard != null)
            _personBoard.SetActive(true);
    }

    void ShowStatusNote(string message)
    {
        if (_stats != null)
        {
            _stats.Clear();
            _stats.SetShown(false);
        }
        if (_detail == null)
            return;
        _detail.gameObject.SetActive(true);
        _detail.alignment = TextAnchor.MiddleLeft;
        _detail.fontSize = 24;
        _detail.lineSpacing = 1f;
        Place(_detail.rectTransform, 130f, 19f, 700f, 500f);
        _detail.text = message;
    }

    static bool IsKnown(CharacterClass character)
    {
        return character.OwnedNumMax != 0 || character.CatalogOpened;
    }

    static void ApplyDarkAura(Transform host, CharacterClass character, bool known)
    {
        if (host == null)
            return;
        if (known)
            ControllerProduction.EnsureOwnedAttributeGlow(host, character);
        else
            ControllerProduction.RemoveAttributeGlow(host);
    }

    Sprite SpriteOf(CharacterClass character)
    {
        return IsKnown(character) ? OpenedSprite(character) : SilhouetteSprite(character);
    }

    Sprite SilhouetteSprite(CharacterClass character)
    {
        Sprite cached;
        if (_silhouetteSprites.TryGetValue(character.ID, out cached) && cached != null)
            return cached;

        Texture2D tex = character.ImageTexture2D;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        int size = character.Size > 0 ? character.Size : tex.width;
        if (size > tex.width)
            size = tex.width;
        if (size > tex.height)
            size = tex.height;
        Color[] pixels = tex.GetPixels(0, 0, size, size);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);
        Texture2D white = new Texture2D(size, size, TextureFormat.RGBA32, false);
        white.filterMode = FilterMode.Point;
        white.SetPixels(pixels);
        white.Apply();
        Sprite sprite = Sprite.Create(white, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        _silhouetteSprites[character.ID] = sprite;
        return sprite;
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
        Panel.GetComponent<Image>().color = Color.black;
        Panel.GetComponent<Image>().raycastTarget = true;
        CanvasGroup group = Panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        _pageButtonLabel = CreatePageButton("ButtonCatalogPage", "人", new Vector2(420f, 610f), TogglePage);

        _count = CreateText("TextCatalogCount", Panel.transform, 40, TextAnchor.MiddleCenter);
        Place(_count.rectTransform, -80f, 610f, 720f, 56f);

        BuildScroll();

        GameObject detailRoot = new GameObject("CatalogDetail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        detailRoot.transform.SetParent(Panel.transform, false);
        RectTransform detailRect = detailRoot.GetComponent<RectTransform>();
        // 下端は -620。上端は -23。リストはこの上で終わる。
        Place(detailRect, 0f, -321.5f, 1020f, 597f);
        detailRoot.transform.SetAsLastSibling();
        detailRoot.GetComponent<Image>().color = Color.black;

        GameObject detailPicture = new GameObject("CatalogDetailPicture", typeof(RectTransform));
        detailPicture.transform.SetParent(detailRoot.transform, false);
        _detailPictureRect = detailPicture.GetComponent<RectTransform>();
        Place(_detailPictureRect, -341f, 144f, 220f, 220f);
        _detailImage = detailPicture.AddComponent<Image>();
        _detailImage.preserveAspect = true;
        _detailImage.raycastTarget = false;

        _detailName = CreateText("TextCatalogDetailName", detailRoot.transform, 26, TextAnchor.MiddleCenter);
        Place(_detailName.rectTransform, -341f, -17f, 290f, 36f);
        _detailName.supportRichText = true;
        _detailName.horizontalOverflow = HorizontalWrapMode.Overflow;
        _detailName.verticalOverflow = VerticalWrapMode.Overflow;

        _repRow = CreateColorRow(detailRoot.transform, "CatalogRepColor", "Main", -83f, out _repChip, out _repDigits);
        _weakRow = CreateColorRow(detailRoot.transform, "CatalogWeakColor", "Weak", -137f, out _weakChip, out _weakDigits);
        ShowColorRows(false);

        _detailStatus = CreateText("TextCatalogDetailStatus", detailRoot.transform, 36, TextAnchor.UpperCenter);
        Place(_detailStatus.rectTransform, -341f, -217f, 290f, 80f);
        _detailStatus.supportRichText = true;
        _detailStatus.horizontalOverflow = HorizontalWrapMode.Overflow;
        _detailStatus.verticalOverflow = VerticalWrapMode.Overflow;

        _detail = CreateText("TextCatalogDetail", detailRoot.transform, 24, TextAnchor.MiddleLeft);
        Place(_detail.rectTransform, 130f, 19f, 700f, 500f);
        _detail.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detail.verticalOverflow = VerticalWrapMode.Truncate;
        _detail.gameObject.SetActive(false);

        _stats = CharacterStatGraph.Create(
            detailRoot.transform,
            "CatalogStatGraph",
            _font,
            140f,
            19f,
            650f,
            560f,
            true,
            Color.white,
            new Color(0.78f, 0.78f, 0.78f, 1f),
            new Color(0.82f, 0.82f, 0.82f, 1f),
            28,
            24,
            22,
            22,
            28,
            32f,
            176.5f,
            78f,
            46f,
            true);
        BuildPersonBoard(detailRoot.transform);
        EnsureWhiteFrame(detailRoot.transform, FrameThin);
    }

    void BuildPersonBoard(Transform parent)
    {
        _personBoard = new GameObject("CatalogPersonBoard", typeof(RectTransform));
        _personBoard.transform.SetParent(parent, false);
        Place(_personBoard.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        _personName = CreateText("PersonName", _personBoard.transform, 30, TextAnchor.MiddleCenter);
        Place(_personName.rectTransform, -373f, -80f, 230f, 40f);
        _personName.horizontalOverflow = HorizontalWrapMode.Overflow;

        _personSpecialty = CreateText("PersonSpecialty", _personBoard.transform, 26, TextAnchor.MiddleCenter);
        Place(_personSpecialty.rectTransform, -373f, -128f, 230f, 36f);
        _personSpecialty.horizontalOverflow = HorizontalWrapMode.Overflow;

        string[] labels = { "R", "G", "B", "ピクセル", "キャラ作成", "バトル" };
        Color[] colors =
        {
            new Color(0.95f, 0.32f, 0.28f, 1f),
            new Color(0.28f, 0.82f, 0.36f, 1f),
            new Color(0.32f, 0.55f, 0.95f, 1f),
            new Color(0.45f, 0.86f, 0.95f, 1f),
            new Color(0.95f, 0.78f, 0.28f, 1f),
            new Color(0.72f, 0.48f, 0.95f, 1f),
        };
        _personRows = new PersonSkillRow[labels.Length];
        float y = 210f;
        for (int i = 0; i < labels.Length; i++)
        {
            _personRows[i] = CreatePersonSkillRow(_personBoard.transform, labels[i], colors[i], y);
            y -= 82f;
        }
        _personBoard.SetActive(false);
    }

    PersonSkillRow CreatePersonSkillRow(Transform parent, string label, Color fillColor, float y)
    {
        GameObject row = new GameObject("PersonSkill" + label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        Place(row.GetComponent<RectTransform>(), 148f, y, 700f, 48f);

        Text name = CreateText("Label", row.transform, 26, TextAnchor.MiddleLeft);
        Place(name.rectTransform, -276f, 0f, 148f, 40f);
        name.text = label;
        name.horizontalOverflow = HorizontalWrapMode.Overflow;

        PersonSkillRow skill = new PersonSkillRow();
        skill.Level = CreateText("Level", row.transform, 26, TextAnchor.MiddleLeft);
        Place(skill.Level.rectTransform, -152f, 0f, 84f, 40f);
        skill.Level.horizontalOverflow = HorizontalWrapMode.Overflow;

        EnsureWhiteSprite();
        GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackObject.transform.SetParent(row.transform, false);
        Place(trackObject.GetComponent<RectTransform>(), 16f, 0f, 228f, 22f);
        Image track = trackObject.GetComponent<Image>();
        track.sprite = _whiteSprite;
        track.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        track.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(trackObject.transform, false);
        Stretch(fillObject.GetComponent<RectTransform>());
        skill.Fill = fillObject.GetComponent<Image>();
        skill.Fill.sprite = _whiteSprite;
        skill.Fill.type = Image.Type.Filled;
        skill.Fill.fillMethod = Image.FillMethod.Horizontal;
        skill.Fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        skill.Fill.fillAmount = 0f;
        skill.Fill.color = fillColor;
        skill.Fill.raycastTarget = false;

        skill.Value = CreateText("Value", row.transform, 22, TextAnchor.MiddleRight);
        Place(skill.Value.rectTransform, 240f, 0f, 200f, 40f);
        skill.Value.horizontalOverflow = HorizontalWrapMode.Overflow;
        return skill;
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

    void TogglePage()
    {
        ShowPage(_page == 1 ? 0 : 1);
    }

    void ShowPage(int page)
    {
        _page = page == 1 ? 1 : 0;
        if (_pageButtonLabel != null)
            _pageButtonLabel.text = _page == 1 ? "キャラ" : "人";
        Refresh();
    }

    void RefreshPeople()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            if (_cells[i].Background != null)
                Destroy(_cells[i].Background.gameObject);
        }
        _cells.Clear();
        if (_selectedPersonId < 1 || _production.GetPerson(_selectedPersonId) == null)
            _selectedPersonId = PersonIds.Protagonist;
        for (int id = 1; id <= PersonIds.Count; id++)
        {
            PersonClass person = _production.GetPerson(id);
            if (person != null)
                _cells.Add(CreatePersonCell(person));
        }
        if (_count != null)
            _count.text = "人 " + PersonIds.Count.ToString();
        ShowDetail();
    }

    CellEntry CreatePersonCell(PersonClass person)
    {
        GameObject cell = new GameObject("CatalogPerson" + person.Id, typeof(RectTransform));
        cell.transform.SetParent(_content, false);
        Image background = cell.AddComponent<Image>();
        background.color = CellFill(person.Id == _selectedPersonId);
        Button button = cell.AddComponent<Button>();
        button.targetGraphic = background;
        int id = person.Id;
        button.onClick.AddListener(() => SelectPerson(id));

        GameObject picture = new GameObject("Picture", typeof(RectTransform));
        picture.transform.SetParent(cell.transform, false);
        RectTransform pictureRect = picture.GetComponent<RectTransform>();
        pictureRect.anchorMin = Vector2.zero;
        pictureRect.anchorMax = Vector2.one;
        pictureRect.offsetMin = new Vector2(FrameSelected, FrameSelected);
        pictureRect.offsetMax = new Vector2(-FrameSelected, -FrameSelected);
        Image image = picture.AddComponent<Image>();
        image.preserveAspect = false;
        image.raycastTarget = false;
        Sprite portrait = PersonPortraits.SpriteOf(person.Id);
        image.sprite = portrait;
        image.color = portrait != null ? Color.white : ControllerProduction.PersonColor(person.Id);
        ControllerProduction.ApplyPersonAttributeFrame(picture.transform, person);
        EnsureWhiteFrame(cell.transform, person.Id == _selectedPersonId ? FrameSelected : FrameThin);
        return new CellEntry { Id = (uint)person.Id, Background = background, Picture = image };
    }

    void SelectPerson(int personId)
    {
        _selectedPersonId = personId;
        RefreshPeople();
    }

    void ShowPersonDetail()
    {
        UsePersonLayout();
        PersonClass person = _production != null ? _production.GetPerson(_selectedPersonId) : null;
        if (_detailImage != null)
        {
            Sprite portrait = person != null ? PersonPortraits.SpriteOf(person.Id) : null;
            _detailImage.sprite = portrait;
            _detailImage.preserveAspect = false;
            _detailImage.color = portrait != null
                ? Color.white
                : (person != null ? ControllerProduction.PersonColor(person.Id) : Color.white);
            ControllerProduction.ApplyPersonAttributeFrame(_detailImage.transform, person);
            CatalogNewMark.Apply(_detailImage.transform, null);
        }
        if (person == null)
        {
            if (_personBoard != null)
                _personBoard.SetActive(false);
            if (_detail != null)
            {
                _detail.gameObject.SetActive(true);
                _detail.alignment = TextAnchor.MiddleLeft;
                _detail.fontSize = 28;
                _detail.lineSpacing = 1f;
                _detail.text = "人を選んでください";
            }
            return;
        }
        if (_detail != null)
            _detail.gameObject.SetActive(false);
        if (_personBoard != null)
            _personBoard.SetActive(true);
        PaintPersonBoard(person);
    }

    void PaintPersonBoard(PersonClass person)
    {
        PersonBalanceConfig config = _production != null ? _production.GetPersonBalance() : null;
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (_personName != null)
            _personName.text = person.Name;
        if (_personSpecialty != null)
            _personSpecialty.text = "得意 " + person.SpecialtyText();
        if (_personRows == null || _personRows.Length < 6)
            return;
        PaintPersonRow(_personRows[0], person.RgbLevel[0], person.RgbExp[0], config.RgbExpExponent, config, 1, person.RgbAmount(0, config).ToString() + "/秒");
        PaintPersonRow(_personRows[1], person.RgbLevel[1], person.RgbExp[1], config.RgbExpExponent, config, 1, person.RgbAmount(1, config).ToString() + "/秒");
        PaintPersonRow(_personRows[2], person.RgbLevel[2], person.RgbExp[2], config.RgbExpExponent, config, 1, person.RgbAmount(2, config).ToString() + "/秒");
        int pixelLevel = person.PixelLevel < 1 ? 1 : person.PixelLevel;
        long pixelExp = person.PixelExp < 0 ? 0 : person.PixelExp;
        long pixelPlain = PersonFormulas.AmountAtLevel(config.PixelBase, pixelLevel, config);
        long pixelSpecialty = PersonFormulas.AmountAtLevel(config.PixelSpecialtyBase, pixelLevel, config);
        string pixelValue = pixelPlain.ToString() + " px";
        if (person.Specialties.Count > 0 && pixelSpecialty != pixelPlain)
            pixelValue += "（得意 " + pixelSpecialty.ToString() + " px）";
        PaintPersonRow(_personRows[3], pixelLevel, pixelExp, config.PixelExpExponent, config, config.PixelExpBaseMultiplier, pixelValue);
        long plain = person.CharacterAmount(false, config);
        long authored = person.CharacterAmount(true, config);
        PaintPersonRow(_personRows[4], person.CharacterLevel, person.CharacterExp, config.CharacterExpExponent, config, config.CharacterExpBaseMultiplier, plain.ToString() + "/秒（作者 " + authored.ToString() + "/秒）");
        int battleLevel = person.BattleLevel < 1 ? 1 : person.BattleLevel;
        double battlePercent = battleLevel * config.BattleLevelBonus * 100.0;
        long battlePoints = (long)System.Math.Round(battlePercent);
        PaintPersonRow(_personRows[5], person.BattleLevel, person.BattleExp, config.BattleExpExponent, config, 1, "+" + battlePoints.ToString() + "%");
    }

    static void PaintPersonRow(PersonSkillRow row, int level, long exp, double exponent, PersonBalanceConfig config, int expBase, string value)
    {
        if (row == null)
            return;
        if (level < 1)
            level = 1;
        if (exp < 0)
            exp = 0;
        long need = PersonFormulas.ExpToNext(level, exponent, config, expBase);
        if (row.Level != null)
            row.Level.text = "Lv " + level.ToString();
        if (row.Value != null)
            row.Value.text = value;
        float ratio = 0f;
        if (need > 0)
            ratio = exp / (float)need;
        if (ratio < 0f)
            ratio = 0f;
        if (ratio > 1f)
            ratio = 1f;
        if (row.Fill != null)
            row.Fill.fillAmount = ratio;
    }

    Text CreatePageButton(string name, string label, Vector2 position, UnityEngine.Events.UnityAction click)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(Panel.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Place(rect, position.x, position.y, 200f, 66f);
        buttonObject.GetComponent<Image>().color = Color.black;
        buttonObject.GetComponent<Button>().onClick.AddListener(click);
        EnsureWhiteFrame(buttonObject.transform, FrameThin);
        Text text = CreateText("Text", buttonObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.text = label;
        return text;
    }

    void BuildScroll()
    {
        GameObject scrollObject = new GameObject("CatalogScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(Panel.transform, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        Place(scrollRect, 0f, 260f, 1040f, 550f);

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
        grid.cellSize = new Vector2(188f, 188f);
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

    static Color CellFill(bool selected)
    {
        return selected ? new Color(0.24f, 0.24f, 0.26f, 1f) : Color.black;
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
}
