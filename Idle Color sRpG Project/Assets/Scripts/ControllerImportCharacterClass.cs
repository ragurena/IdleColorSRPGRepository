using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// PNG から自作キャラを読み込む画面。
public class ControllerImportCharacterClass : MonoBehaviour
{
    ControllerProduction _production;
    Font _font;
    Text _count;
    Text _sizes;
    Text _message;
    InputField _name;
    GameObject _confirm;
    Image _confirmImage;
    Text _confirmStats;
    Text _confirmMessage;
    Sprite _confirmSprite;
    RectTransform _content;
    bool _picking;
    readonly List<GameObject> _cells = new List<GameObject>();

    public GameObject Panel { get; private set; }

    public void Initialize(ControllerProduction production, GameObject sizeSource)
    {
        _production = production;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        PngFilePicker.Ensure();
        Build(sizeSource);
    }

    public void Open()
    {
        Refresh();
    }

    public void Close()
    {
        DiscardPending();
    }

    public void Refresh()
    {
        if (_count != null && _production != null)
            _count.text = "自作 " + _production.ImportedCharacterCount().ToString() + " / " + ImportSizeRules.MaxCount.ToString();
        if (_sizes != null && _production != null)
            _sizes.text = ImportSizeRules.Describe(_production.GetClearedBattleStage());
        RebuildCells();
    }

    public void PushPick()
    {
        if (_picking || _production == null)
            return;
        if (_confirm != null && _confirm.activeSelf)
            return;
        if (_production.ImportedCharacterCount() >= ImportSizeRules.MaxCount)
        {
            SetMessage("自作キャラは 500 体までです。", false);
            return;
        }

        _picking = true;
        PngFilePicker.Pick(OnPicked);
    }

    public void PushConfirmName()
    {
        if (_production == null)
            return;
        string name = _name != null ? _name.text : "";
        string message;
        bool added = _production.CommitPreparedImport(name, out message);
        if (!added)
        {
            SetConfirmMessage(message, false);
            return;
        }
        HideConfirm();
        SetMessage(message, true);
        Refresh();
    }

    public void PushCancelImport()
    {
        DiscardPending();
        SetMessage("読み込みをやめました。", false);
    }

    void OnPicked(string path)
    {
        _picking = false;
        if (_production == null)
            return;
        if (path == null)
        {
            SetMessage("この環境ではファイルを選べません。", false);
            return;
        }
        if (string.IsNullOrEmpty(path))
        {
            SetMessage("ファイルを選びませんでした。", false);
            return;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (IOException)
        {
            SetMessage("ファイルを読めませんでした。", false);
            return;
        }

        string message;
        bool prepared = _production.TryPrepareImport(bytes, out message);
        if (!prepared)
        {
            SetMessage(message, false);
            return;
        }

        ShowConfirm(NameFromFile(path));
    }

    void ShowConfirm(string fileName)
    {
        CharacterClass character = _production != null ? _production.PreparedImportCharacter() : null;
        if (character == null || _confirm == null)
        {
            DiscardPending();
            SetMessage("画像をキャラにできませんでした。", false);
            return;
        }

        if (_name != null)
            _name.text = fileName ?? "";
        if (_confirmStats != null)
            _confirmStats.text = FormatStats(character);
        if (_confirmImage != null)
        {
            if (_confirmSprite != null)
                Destroy(_confirmSprite);
            _confirmSprite = SpriteOf(character);
            _confirmImage.sprite = _confirmSprite;
            _confirmImage.color = Color.white;
        }
        SetConfirmMessage("", true);
        _confirm.SetActive(true);
    }

    void HideConfirm()
    {
        if (_confirm != null)
            _confirm.SetActive(false);
        if (_confirmImage != null)
            _confirmImage.sprite = null;
        if (_confirmSprite != null)
        {
            Destroy(_confirmSprite);
            _confirmSprite = null;
        }
    }

    void DiscardPending()
    {
        HideConfirm();
        if (_production != null)
            _production.DiscardPreparedImport();
    }

    static string NameFromFile(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path ?? "");
        if (string.IsNullOrWhiteSpace(name))
            return "";
        name = name.Trim().Replace("\r", "").Replace("\n", "");
        if (name.Length > 24)
            name = name.Substring(0, 24);
        return name;
    }

    static string FormatStats(CharacterClass character)
    {
        if (character == null || character.Stats == null || character.Stats[0] == null)
            return "ステータスを読めませんでした。";
        StatisticsClass stats = character.Stats[0];
        return "一辺 " + character.Size.ToString() + "\n"
            + TypeName(character.CharacterType) + "    弱点 " + TypeName(character.WeaknessType) + "\n"
            + "HP " + stats.HPMax.ToString() + "\n"
            + "ATK " + stats.ATK.ToString() + "    DEF " + stats.DEF.ToString() + "\n"
            + "SPD " + stats.SPD.ToString() + "    LUC " + stats.LUC.ToString() + "\n"
            + "R " + stats.RCreates.ToString() + "    G " + stats.GCreates.ToString() + "    B " + stats.BCreates.ToString();
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

    void SetMessage(string message, bool ok)
    {
        PaintMessage(_message, message, ok);
    }

    void SetConfirmMessage(string message, bool ok)
    {
        PaintMessage(_confirmMessage, message, ok);
    }

    static void PaintMessage(Text label, string message, bool ok)
    {
        if (label == null)
            return;
        label.text = message ?? "";
        label.color = ok ? new Color(0.1f, 0.35f, 0.15f, 1f) : new Color(0.7f, 0.12f, 0.12f, 1f);
    }

    void RebuildCells()
    {
        if (_content == null || _production == null)
            return;
        for (int i = 0; i < _cells.Count; i++)
        {
            if (_cells[i] != null)
                Destroy(_cells[i]);
        }
        _cells.Clear();
        _production.ForEachImportedCharacter(character =>
        {
            if (character == null)
                return;
            _cells.Add(CreateCell(character));
        });
    }

    GameObject CreateCell(CharacterClass character)
    {
        GameObject cell = new GameObject("ImportCell" + character.ID, typeof(RectTransform), typeof(Image));
        cell.transform.SetParent(_content, false);
        cell.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.93f, 1f);

        GameObject picture = new GameObject("Picture", typeof(RectTransform));
        picture.transform.SetParent(cell.transform, false);
        RectTransform pictureRect = picture.GetComponent<RectTransform>();
        pictureRect.anchorMin = new Vector2(0.15f, 0.38f);
        pictureRect.anchorMax = new Vector2(0.85f, 0.94f);
        pictureRect.offsetMin = Vector2.zero;
        pictureRect.offsetMax = Vector2.zero;
        Image image = picture.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.sprite = SpriteOf(character);

        Text label = CreateText("Label", cell.transform, 18, TextAnchor.UpperCenter);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0.36f);
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, -2f);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.text = character.Name + "\n" + character.Size.ToString();
        return cell;
    }

    static Sprite SpriteOf(CharacterClass character)
    {
        Texture2D tex = character.ImageTexture2D;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");
        tex.filterMode = FilterMode.Point;
        int size = character.Size > 0 ? character.Size : tex.width;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void Build(GameObject sizeSource)
    {
        if (Panel != null || sizeSource == null)
            return;

        Panel = new GameObject("PanelImportCharacter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
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

        Text title = CreateText("TextImportTitle", Panel.transform, 40, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 0f, 640f, 1000f, 56f);
        title.text = "キャラを読み込む";

        _count = CreateText("TextImportCount", Panel.transform, 28, TextAnchor.MiddleCenter);
        Place(_count.rectTransform, 0f, 585f, 1000f, 40f);

        _sizes = CreateText("TextImportSizes", Panel.transform, 22, TextAnchor.UpperLeft);
        Place(_sizes.rectTransform, 0f, 470f, 980f, 170f);
        _sizes.horizontalOverflow = HorizontalWrapMode.Wrap;
        _sizes.verticalOverflow = VerticalWrapMode.Overflow;

        GameObject pickObject = new GameObject("ButtonPickPng", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        pickObject.transform.SetParent(Panel.transform, false);
        Place(pickObject.GetComponent<RectTransform>(), 0f, 320f, 420f, 90f);
        pickObject.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.75f, 1f);
        Button pick = pickObject.GetComponent<Button>();
        pick.targetGraphic = pickObject.GetComponent<Image>();
        pick.onClick.AddListener(PushPick);
        Text pickLabel = CreateText("Text", pickObject.transform, 32, TextAnchor.MiddleCenter);
        Stretch(pickLabel.rectTransform);
        pickLabel.text = "PNGを選ぶ";
        pickLabel.color = Color.white;

        _message = CreateText("TextImportMessage", Panel.transform, 26, TextAnchor.MiddleCenter);
        Place(_message.rectTransform, 0f, 165f, 980f, 80f);
        _message.horizontalOverflow = HorizontalWrapMode.Wrap;
        _message.verticalOverflow = VerticalWrapMode.Overflow;

        BuildScroll();
        BuildConfirm();
    }

    void BuildConfirm()
    {
        _confirm = new GameObject("PanelImportConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _confirm.transform.SetParent(Panel.transform, false);
        Stretch(_confirm.GetComponent<RectTransform>());
        _confirm.GetComponent<Image>().color = new Color(0.93f, 0.93f, 0.96f, 1f);
        _confirm.GetComponent<Image>().raycastTarget = true;

        Text title = CreateText("TextImportConfirmTitle", _confirm.transform, 36, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 0f, 620f, 1000f, 56f);
        title.text = "読み込んだキャラ";

        GameObject picture = new GameObject("ImageImportConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        picture.transform.SetParent(_confirm.transform, false);
        Place(picture.GetComponent<RectTransform>(), 0f, 400f, 280f, 280f);
        _confirmImage = picture.GetComponent<Image>();
        _confirmImage.preserveAspect = true;
        _confirmImage.raycastTarget = false;

        _confirmStats = CreateText("TextImportConfirmStats", _confirm.transform, 28, TextAnchor.UpperCenter);
        Place(_confirmStats.rectTransform, 0f, 40f, 900f, 300f);
        _confirmStats.horizontalOverflow = HorizontalWrapMode.Wrap;
        _confirmStats.verticalOverflow = VerticalWrapMode.Overflow;

        Text prompt = CreateText("TextImportConfirmPrompt", _confirm.transform, 26, TextAnchor.MiddleCenter);
        Place(prompt.rectTransform, 0f, -160f, 960f, 110f);
        prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
        prompt.verticalOverflow = VerticalWrapMode.Overflow;
        prompt.text = "このキャラ名でよろしいですか？\n違う名前も設定できます。";

        BuildNameField(_confirm.transform);

        GameObject okObject = new GameObject("ButtonImportConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        okObject.transform.SetParent(_confirm.transform, false);
        Place(okObject.GetComponent<RectTransform>(), -200f, -360f, 360f, 84f);
        okObject.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.75f, 1f);
        Button ok = okObject.GetComponent<Button>();
        ok.targetGraphic = okObject.GetComponent<Image>();
        ok.onClick.AddListener(PushConfirmName);
        Text okLabel = CreateText("Text", okObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(okLabel.rectTransform);
        okLabel.text = "この名前で読み込む";
        okLabel.color = Color.white;

        GameObject cancelObject = new GameObject("ButtonImportCancel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cancelObject.transform.SetParent(_confirm.transform, false);
        Place(cancelObject.GetComponent<RectTransform>(), 200f, -360f, 280f, 84f);
        cancelObject.GetComponent<Image>().color = new Color(0.55f, 0.55f, 0.58f, 1f);
        Button cancel = cancelObject.GetComponent<Button>();
        cancel.targetGraphic = cancelObject.GetComponent<Image>();
        cancel.onClick.AddListener(PushCancelImport);
        Text cancelLabel = CreateText("Text", cancelObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(cancelLabel.rectTransform);
        cancelLabel.text = "やめる";
        cancelLabel.color = Color.white;

        _confirmMessage = CreateText("TextImportConfirmMessage", _confirm.transform, 24, TextAnchor.MiddleCenter);
        Place(_confirmMessage.rectTransform, 0f, -450f, 960f, 70f);
        _confirmMessage.horizontalOverflow = HorizontalWrapMode.Wrap;

        _confirm.SetActive(false);
    }

    void BuildNameField(Transform parent)
    {
        GameObject fieldObject = new GameObject("InputFieldImportCharacterName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
        fieldObject.transform.SetParent(parent, false);
        Place(fieldObject.GetComponent<RectTransform>(), 0f, -260f, 760f, 80f);
        Image background = fieldObject.GetComponent<Image>();
        background.color = Color.white;

        Text text = CreateText("Text", fieldObject.transform, 32, TextAnchor.MiddleLeft);
        Place(text.rectTransform, 8f, 0f, 720f, 70f);
        text.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        text.supportRichText = false;

        Text placeholder = CreateText("Placeholder", fieldObject.transform, 28, TextAnchor.MiddleLeft);
        Place(placeholder.rectTransform, 8f, 0f, 720f, 70f);
        placeholder.text = "名前";
        placeholder.color = new Color(0.55f, 0.55f, 0.58f, 1f);
        placeholder.fontStyle = FontStyle.Italic;

        _name = fieldObject.GetComponent<InputField>();
        _name.textComponent = text;
        _name.placeholder = placeholder;
        _name.lineType = InputField.LineType.SingleLine;
        _name.characterLimit = 24;
        _name.targetGraphic = background;
    }

    void BuildScroll()
    {
        GameObject scrollObject = new GameObject("ImportScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(Panel.transform, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        Place(scrollRect, 0f, -220f, 1040f, 680f);

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
        grid.cellSize = new Vector2(220f, 240f);
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
