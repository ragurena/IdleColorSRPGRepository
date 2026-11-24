using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// バトルのステージ画面から開く共有ステージ。作成、読み込み、出撃。
public class ShareStageBoard : MonoBehaviour
{
    public const string CellPrefix = "ButtonShareStageCell";
    const int MaxSelect = 9;
    const float FormationCell = 200f;
    const float FormationGap = 12f;
    const float FormationTop = 430f;

    static ShareStageBoard _board;

    ControllerProduction _production;
    ControllerBattleStageClass _stage;
    Font _font;
    GameObject _root;
    Text _title;
    Text _body;
    Text _status;
    InputField _author;
    InputField _url;
    Button _createButton;
    Text _countLabel;
    RectTransform _list;
    readonly List<uint> _selected = new List<uint>();
    readonly Dictionary<uint, Sprite> _cellSprites = new Dictionary<uint, Sprite>();
    GameObject _formation;
    GameObject _previewFormation;
    Button[,] _cells;
    uint[,] _cellIds;
    readonly List<CharacterClass> _picked = new List<CharacterClass>();
    int _background;
    string _mineStageId = "";
    PiquaDocument _draft;
    byte[] _draftZip;
    string _selectedKey = "";
    bool _awaitReward;
    bool _busy;
    int _listRows = -1;
    float _contentHeight = -1f;
    RectTransform _contentSized;
    readonly List<ShareListCard> _listCards = new List<ShareListCard>();
    readonly Dictionary<string, Sprite> _portraitSprites = new Dictionary<string, Sprite>();

    public static void Ensure(ControllerBattleStageClass stage, ControllerProduction production)
    {
        if (_board != null || stage == null || production == null)
            return;
        GameObject panel = GameObject.Find("PanelBattleStage");
        if (panel == null)
            return;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        GameObject sizeSource = GameObject.Find("PanelRGBProduction");
        if (sizeSource == null)
            sizeSource = GameObject.Find("PanelImportCharacter");
        Transform parent = sizeSource != null ? sizeSource.transform.parent : panel.transform.parent;
        if (parent == null)
            parent = panel.transform;
        GameObject host = new GameObject("ShareStageBoard", typeof(RectTransform));
        host.transform.SetParent(parent, false);
        RectTransform hostRect = host.GetComponent<RectTransform>();
        if (sizeSource != null)
        {
            RectTransform sourceRect = sizeSource.GetComponent<RectTransform>();
            hostRect.anchorMin = sourceRect.anchorMin;
            hostRect.anchorMax = sourceRect.anchorMax;
            hostRect.pivot = sourceRect.pivot;
            hostRect.anchoredPosition = sourceRect.anchoredPosition;
            hostRect.sizeDelta = sourceRect.sizeDelta;
            hostRect.localScale = Vector3.one;
        }
        else
        {
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = Vector2.zero;
            hostRect.offsetMax = Vector2.zero;
        }
        _board = host.AddComponent<ShareStageBoard>();
        _board._stage = stage;
        _board._production = production;
        _board._font = font;
        _board.Build(panel.transform);
    }

    public static void HideIfAny()
    {
        if (_board != null)
            _board.Close();
    }

    void Build(Transform panel)
    {
        Button open = CreateButton("ButtonSharedStageTab", panel, "共有\nステージ", new Color(0.16f, 0.38f, 0.92f, 1f));
        if (_stage != null)
            _stage.PlaceSharedStageButton(open);

        _root = new GameObject("PanelShareStage", typeof(RectTransform), typeof(Image));
        _root.transform.SetParent(transform, false);
        Stretch(_root.GetComponent<RectTransform>());
        Image shade = _root.GetComponent<Image>();
        shade.sprite = WhiteSprite();
        shade.type = Image.Type.Simple;
        shade.color = new Color(0.05f, 0.06f, 0.09f, 1f);
        _title = CreateText(_root.transform, "共有ステージ", 40, TextAnchor.MiddleCenter);
        Place(_title.rectTransform, 12f, 72f, 24f, 24f);

        _body = CreateText(_root.transform, "", 28, TextAnchor.UpperLeft);
        Place(_body.rectTransform, 96f, 110f, 24f, 24f);
        _status = CreateText(_root.transform, "", 26, TextAnchor.UpperLeft);
        _status.color = new Color(1f, 0.86f, 0.45f, 1f);
        Place(_status.rectTransform, 210f, 70f, 24f, 24f);

        GameObject scroll = new GameObject("ShareList", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(RectMask2D));
        scroll.transform.SetParent(_root.transform, false);
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        RectTransform scrollRect = scroll.GetComponent<RectTransform>();
        Pin(scrollRect, 290f, 24f, 16f, 16f);
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(scroll.transform, false);
        _list = content.GetComponent<RectTransform>();
        _list.anchorMin = new Vector2(0f, 1f);
        _list.anchorMax = new Vector2(1f, 1f);
        _list.pivot = new Vector2(0.5f, 1f);
        ScrollRect scroller = scroll.GetComponent<ScrollRect>();
        scroller.content = _list;
        scroller.horizontal = false;
        scroller.vertical = true;
        scroller.viewport = scrollRect;
        _root.SetActive(false);
    }

    void OpenHome()
    {
        transform.SetAsLastSibling();
        if (_production != null && _production.IsBattleRunning())
        {
            _root.SetActive(true);
            ShowMessage("戦闘中は共有ステージを開けません。");
            return;
        }
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
        SetListTop(290f);
        ShowHome();
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);
        _busy = false;
    }

    public static void ShowList(Transform content, string selectedKey, string activeKey, bool scrollTop)
    {
        if (_board != null)
            _board.FillList(content, selectedKey, activeKey, scrollTop);
    }

    public static void HideList()
    {
        if (_board != null)
            _board.ClearListView();
    }

    void ReturnToList()
    {
        HideFields();
        if (_url != null)
            _url.gameObject.SetActive(false);
        Close();
        if (_production != null)
            _production.PushButtonSelectSceneBattleStage();
    }

    void Present()
    {
        if (_root == null)
            return;
        _root.SetActive(true);
        transform.SetAsLastSibling();
        GameObject bar = GameObject.Find("PanelSelectScene");
        if (bar != null)
            bar.transform.SetAsLastSibling();
    }

    void OpenCreate()
    {
        Present();
        ShowCreate();
    }

    public static void OpenLoadScreen()
    {
        if (_board != null)
            _board.OpenLoad();
    }

    void OpenLoad()
    {
        Present();
        ShowLoad();
    }

    void FillList(Transform content, string selectedKey, string activeKey, bool scrollTop)
    {
        if (content == null)
            return;
        List<ShareListRow> rows = new List<ShareListRow>();
        ShareStageLibrary.ForEachPlayable(stage =>
        {
            int from = ShareStageRules.WindowStart(stage.ClearedFloor);
            int to = ShareStageRules.WindowEnd(stage.ClearedFloor);
            string label = ShownName(stage.Name, stage.AuthorName) + "\n" + from.ToString() + "〜" + to.ToString() + "階\n"
                + stage.AuthorName + "  星 " + stage.Stars.ToString();
            if (stage.Key == activeKey)
                label += "\n出撃中";
            rows.Add(new ShareListRow
            {
                Kind = 3,
                Key = stage.Key,
                Label = label,
                Stage = stage,
                Selected = stage.Key == selectedKey
            });
        });

        bool laidOut = content.GetComponent<LayoutGroup>() != null;
        RectTransform sample = SampleCard(content);
        Font font = FontOf(content);
        const float rowHeight = 188f;
        for (int i = 0; i < rows.Count; i++)
        {
            ShareListCard card = CardAt(content, i, font);
            BindCard(card, rows[i]);
            card.Button.gameObject.SetActive(true);
            card.Button.transform.SetSiblingIndex(i);
            if (!laidOut)
                PlaceCard(card.Button.GetComponent<RectTransform>(), sample, i, rowHeight);
        }
        for (int i = rows.Count; i < _listCards.Count; i++)
        {
            if (_listCards[i].Button != null)
                _listCards[i].Button.gameObject.SetActive(false);
        }

        FitContent(content as RectTransform, rows.Count, rowHeight);
        if (_listRows != rows.Count && content is RectTransform rect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        _listRows = rows.Count;
        if (!scrollTop)
            return;
        Canvas.ForceUpdateCanvases();
        ScrollRect scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    void ClearListView()
    {
        for (int i = 0; i < _listCards.Count; i++)
        {
            if (_listCards[i].Button != null)
                _listCards[i].Button.gameObject.SetActive(false);
        }
        if (_contentSized != null && _contentHeight >= 0f && _contentSized.GetComponent<ContentSizeFitter>() == null)
            _contentSized.sizeDelta = new Vector2(_contentSized.sizeDelta.x, _contentHeight);
        _listRows = -1;
    }

    void BindCard(ShareListCard card, ShareListRow row)
    {
        card.Label.text = row.Label;
        bool stageCard = row.Kind == 3 || row.Kind == 4;
        Sprite background = stageCard && row.Stage != null ? BattleStageCatalog.BackgroundByName(row.Stage.Background) : null;
        bool showFace = background != null;
        card.Face.gameObject.SetActive(showFace);
        card.Face.sprite = background;
        card.Button.image.color = row.Selected ? new Color(1f, 0.85f, 0.4f, 1f) : Color.white;
        card.Label.color = showFace ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
        card.Outline.enabled = showFace;
        card.Button.interactable = row.Kind != 0;

        Sprite[] pics = new Sprite[card.Portraits.Length];
        int shown = 0;
        if (stageCard && row.Stage != null)
        {
            for (int i = 0; i < row.Stage.Characters.Count && shown < pics.Length; i++)
            {
                Sprite sprite = Portrait(row.Stage.Characters[i].PngHash);
                if (sprite == null)
                    continue;
                pics[shown] = sprite;
                shown++;
            }
        }
        const float size = 56f;
        const float gap = 4f;
        float x = -(8f + shown * size + Mathf.Max(0, shown - 1) * gap);
        for (int i = 0; i < card.Portraits.Length; i++)
        {
            Image pic = card.Portraits[i];
            if (i >= shown)
            {
                pic.enabled = false;
                pic.sprite = null;
                continue;
            }
            pic.sprite = pics[i];
            pic.enabled = true;
            pic.color = Color.white;
            pic.rectTransform.anchoredPosition = new Vector2(x + size * 0.5f, 8f + size * 0.5f);
            x += size + gap;
        }
        float pad = shown > 0 ? 8f + shown * size + Mathf.Max(0, shown - 1) * gap + 12f : 16f;
        card.Label.rectTransform.offsetMax = new Vector2(-pad, -12f);

        string key = row.Key;
        card.Press = () =>
        {
            if (_stage != null)
                _stage.SelectSharedStage(key, null);
        };
    }

    ShareListCard CardAt(Transform content, int index, Font font)
    {
        while (_listCards.Count <= index)
            _listCards.Add(CreateListCard(content, font));
        ShareListCard card = _listCards[index];
        if (card.Button.transform.parent != content)
            card.Button.transform.SetParent(content, false);
        return card;
    }

    ShareListCard CreateListCard(Transform parent, Font font)
    {
        ShareListCard card = new ShareListCard();
        GameObject obj = new GameObject("ShareStageCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = Color.white;
        Button button = obj.GetComponent<Button>();
        LayoutElement element = obj.GetComponent<LayoutElement>();
        element.minHeight = 188f;
        element.preferredHeight = 188f;
        element.flexibleWidth = 1f;
        card.Button = button;
        GameAudio.BindUi(button);
        button.onClick.AddListener(() =>
        {
            if (card.Press != null)
                card.Press();
        });

        GameObject faceObject = new GameObject("StageBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        faceObject.transform.SetParent(obj.transform, false);
        Image face = faceObject.GetComponent<Image>();
        face.raycastTarget = false;
        face.color = new Color(0.38f, 0.38f, 0.38f, 1f);
        RectTransform faceRect = face.rectTransform;
        faceRect.anchorMin = Vector2.zero;
        faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = new Vector2(8f, 8f);
        faceRect.offsetMax = new Vector2(-8f, -8f);
        card.Face = face;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(obj.transform, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font != null ? font : _font;
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = 32;
        text.raycastTarget = false;
        text.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 12f);
        textRect.offsetMax = new Vector2(-16f, -12f);
        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.enabled = false;
        card.Label = text;
        card.Outline = outline;

        for (int i = 0; i < card.Portraits.Length; i++)
        {
            GameObject portrait = new GameObject("Portrait" + i.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            portrait.transform.SetParent(obj.transform, false);
            Image pic = portrait.GetComponent<Image>();
            pic.preserveAspect = true;
            pic.raycastTarget = false;
            RectTransform rect = pic.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(56f, 56f);
            rect.localScale = new Vector3(-1f, 1f, 1f);
            pic.enabled = false;
            card.Portraits[i] = pic;
        }
        return card;
    }

    Sprite Portrait(string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return null;
        Sprite sprite;
        if (_portraitSprites.TryGetValue(hash, out sprite))
            return sprite;
        sprite = null;
        string path = ShareStageLibrary.PngPath(hash);
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
            }
            else
                Destroy(texture);
        }
        _portraitSprites[hash] = sprite;
        return sprite;
    }

    void FitContent(RectTransform content, int rows, float rowHeight)
    {
        if (content == null || content.GetComponent<ContentSizeFitter>() != null)
            return;
        if (_contentSized != content)
        {
            _contentSized = content;
            _contentHeight = content.sizeDelta.y;
        }
        float spacing = 0f;
        VerticalLayoutGroup vertical = content.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
            spacing = vertical.spacing;
        float height = rows * rowHeight + Mathf.Max(0, rows - 1) * spacing;
        if (height < _contentHeight)
            height = _contentHeight;
        content.sizeDelta = new Vector2(content.sizeDelta.x, height);
    }

    static RectTransform SampleCard(Transform content)
    {
        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (child.name.StartsWith("ShareStageCard"))
                continue;
            return child as RectTransform;
        }
        return null;
    }

    Font FontOf(Transform content)
    {
        Text[] texts = content.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || texts[i].font == null)
                continue;
            if (texts[i].transform.IsChildOf(transform))
                continue;
            return texts[i].font;
        }
        return _font;
    }

    static void PlaceCard(RectTransform rect, RectTransform sample, int index, float height)
    {
        if (rect == null)
            return;
        if (sample != null)
        {
            rect.anchorMin = sample.anchorMin;
            rect.anchorMax = sample.anchorMax;
            rect.pivot = sample.pivot;
            float width = sample.sizeDelta.x;
            if (width < 1f)
                width = rect.sizeDelta.x;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = sample.anchoredPosition + new Vector2(0f, -(height + 12f) * index);
            return;
        }
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = new Vector2(0f, -(height + 12f) * index);
    }

    sealed class ShareListCard
    {
        public Button Button;
        public Text Label;
        public Image Face;
        public Outline Outline;
        public readonly Image[] Portraits = new Image[9];
        public Action Press;
    }

    struct ShareListRow
    {
        public int Kind;
        public string Key;
        public string Label;
        public SharedStageRecord Stage;
        public bool Selected;
    }

    void ShowHome()
    {
        HideFields();
        SetListTop(290f);
        _title.text = "共有ステージ";
        _body.text = "作ったステージをファイルにして渡せます。読み込んだステージは、この一覧から出撃します。";
        _status.text = "";
        ClearList();
        AddButton("ステージを作る", ShowCreate);
        AddButton("ステージを読み込む", ShowLoad);
        ShareStageLibrary.ForEachPlayable(stage =>
        {
            string mark = stage.ClearedFloor >= ShareStageRules.FloorCount ? "300階クリア  " : stage.ClearedFloor.ToString() + "階  ";
            string label = ShownName(stage.Name, stage.AuthorName) + "\n" + stage.AuthorName + "  " + mark + "星 " + stage.Stars.ToString() + "  更新" + stage.Revision.ToString();
            SharedStageRecord captured = stage;
            AddButton(label, () => ShowDetail(captured));
        });
        ShareStageLibrary.ForEachMine(stage =>
        {
            SharedStageRecord captured = stage;
            AddButton("作り直す  " + ShownName(stage.Name, stage.AuthorName) + "  更新" + stage.Revision.ToString(), () => ShowCreateFrom(captured));
        });
        ResizeList();
    }

    void ShowDetail(SharedStageRecord stage)
    {
        HideFields();
        SetListTop(290f);
        _selectedKey = stage.Key;
        int from = ShareStageRules.WindowStart(stage.ClearedFloor);
        int to = ShareStageRules.WindowEnd(stage.ClearedFloor);
        StringBuilder text = new StringBuilder();
        text.Append(ShownName(stage.Name, stage.AuthorName)).Append("\n投稿者 ").Append(stage.AuthorName);
        text.Append("\n").Append(stage.Comment);
        text.Append("\nキャラ ").Append(stage.Characters.Count.ToString()).Append("体");
        text.Append("\n").Append(from.ToString()).Append("〜").Append(to.ToString()).Append("階");
        text.Append("\n更新 ").Append(stage.UpdatedAt).Append("  #").Append(stage.Revision.ToString());
        text.Append(stage.ClearedFloor >= ShareStageRules.FloorCount ? "\n300階クリア" : "\n攻略 " + stage.ClearedFloor.ToString() + "階");
        text.Append("\n星 ").Append(stage.Stars.ToString());
        text.Append("\n").Append(ShareStageRules.BackgroundLabel(stage.Background));
        for (int i = 0; i < stage.Characters.Count; i++)
            text.Append("\n").Append(stage.Characters[i].Name).Append("  Lv").Append(stage.Characters[i].Level.ToString());
        _body.text = text.ToString();
        _status.text = "編成セット " + (_stage != null ? _stage.SelectedPartySet().ToString() : "1") + " で出撃します。";
        ClearList();
        AddButton("出撃する", SortieSelected);
        AddButton("一覧へ戻る", ReturnToList);
        ResizeList();
    }

    void SortieSelected()
    {
        if (_production == null || string.IsNullOrEmpty(_selectedKey))
            return;
        int setIndex = _stage != null ? _stage.SelectedPartySet() : 1;
        if (!_production.EnterSharedBattle(_selectedKey, setIndex))
        {
            _status.text = "出撃できません。編成にキャラがいるか確認してください。";
            return;
        }
        Close();
        if (!_production.BeginAutoBattle())
            _production.LeaveBattle();
    }

    void ShowCreate()
    {
        ShowCreateFrom(null);
    }

    void ShowCreateFrom(SharedStageRecord mine)
    {
        _mineStageId = mine != null ? mine.StageId : "";
        _selected.Clear();
        _title.text = mine != null ? "ステージを更新" : "ステージを作る";
        _body.text = "自分が作者のキャラを、マスに入れます。1〜9体。";
        ShareStageLibrary.EnsureUser();
        EnsureFields();
        _author.text = !string.IsNullOrEmpty(ShareStageLibrary.AuthorName)
            ? ShareStageLibrary.AuthorName
            : (mine != null ? mine.AuthorName : "");
        _background = BackgroundIndex(mine != null ? mine.Background : "");
        ClearCells();
        if (mine != null)
        {
            int placed = 0;
            for (int i = 0; i < mine.SourceIds.Count && placed < MaxSelect; i++)
            {
                CharacterClass character = ImportedCharacters.Find(mine.SourceIds[i]);
                if (!IsOwnCharacter(character))
                    continue;
                int x = placed % 3 + 1;
                int y = 3 - placed / 3;
                _cellIds[x, y] = character.ID;
                placed++;
            }
        }
        ShowFields();
        RebuildCreateList();
    }

    public static bool IsCell(Button button)
    {
        return button != null && button.name.StartsWith(CellPrefix);
    }

    public static bool IsOwnCharacter(CharacterClass character)
    {
        if (character == null)
            return false;
        if (character.ID < ImportedCharacters.FirstId || character.ID >= ShareStageLibrary.CompanionFirstId)
            return false;
        return character.AuthorPersonId == PersonIds.Protagonist;
    }

    public static void ApplyPickedCell(Button cell, uint characterId)
    {
        if (_board != null)
            _board.ApplyPicked(cell, characterId);
    }

    void EnsureFields()
    {
        if (_author != null)
            return;
        _author = CreateField("ユーザー名", 290f);
        _author.onEndEdit.AddListener(RememberAuthor);
        _countLabel = CreateText(_root.transform, "", 28, TextAnchor.MiddleLeft);
        Place(_countLabel.rectTransform, 372f, 36f, 24f, 24f);
        _url = CreateField("ダウンロードURL", 290f);
        _url.gameObject.SetActive(false);
        _createButton = CreateButton("ButtonShareCreate", _root.transform, "作成", Color.white);
        RectTransform createRect = _createButton.GetComponent<RectTransform>();
        createRect.anchorMin = new Vector2(0f, 0f);
        createRect.anchorMax = new Vector2(1f, 0f);
        createRect.pivot = new Vector2(0.5f, 0f);
        createRect.offsetMin = new Vector2(24f, 156f);
        createRect.offsetMax = new Vector2(-24f, 268f);
        _createButton.onClick.AddListener(CreateStage);
        if (_production != null)
            _production.StyleRepresentativeButton(_createButton, Color.white, true);
        Text createLabel = _createButton.GetComponentInChildren<Text>();
        if (createLabel != null)
        {
            createLabel.color = Color.white;
            createLabel.transform.SetAsLastSibling();
        }
        _createButton.gameObject.SetActive(false);
    }

    void RebuildCreateList()
    {
        EnsureFormation();
        PaintFormation();
        SyncSelected();
        ClearList();
        if (_countLabel != null)
            _countLabel.text = "選択 " + _selected.Count.ToString() + " / 9";
        AddButton("背景  " + ShareStageRules.BackgroundLabel(ShareStageRules.Backgrounds[_background]), CycleBackground);
        AddButton("一覧へ戻る", ReturnToList);
        ResizeList();
        _status.text = "";
        LayoutCreateForm();
    }

    void LayoutCreateForm()
    {
        const float buttonBottom = 156f;
        const float buttonHeight = 112f;
        const float gap = 56f;
        if (_createButton != null)
        {
            RectTransform createRect = _createButton.GetComponent<RectTransform>();
            createRect.anchorMin = new Vector2(0f, 0f);
            createRect.anchorMax = new Vector2(1f, 0f);
            createRect.pivot = new Vector2(0.5f, 0f);
            createRect.offsetMin = new Vector2(24f, buttonBottom);
            createRect.offsetMax = new Vector2(-24f, buttonBottom + buttonHeight);
            _createButton.transform.SetAsLastSibling();
        }
        Canvas.ForceUpdateCanvases();
        RectTransform rootRect = _root != null ? _root.GetComponent<RectTransform>() : null;
        float height = rootRect != null ? rootRect.rect.height : 0f;
        if (height < 400f)
            height = 1600f;
        float cell = FormationCell;
        float span = cell * 3f + FormationGap * 2f;
        float room = height - (buttonBottom + buttonHeight) - gap - FormationTop;
        if (room < span)
        {
            cell = Mathf.Floor((room - FormationGap * 2f) / 3f);
            if (cell < 108f)
                cell = 108f;
            span = cell * 3f + FormationGap * 2f;
        }
        if (_formation != null)
        {
            RectTransform rect = _formation.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(span, span);
            GridLayoutGroup layout = _formation.GetComponent<GridLayoutGroup>();
            if (layout != null)
                layout.cellSize = new Vector2(cell, cell);
        }
        SetListTop(FormationTop + span + 16f, buttonBottom + buttonHeight + 12f);
    }

    void EnsureFormation()
    {
        if (_formation != null)
            return;
        _cells = new Button[4, 4];
        _cellIds = new uint[4, 4];
        GameObject obj = new GameObject("ShareStageFormation", typeof(RectTransform), typeof(GridLayoutGroup));
        obj.transform.SetParent(_root.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        float span = FormationCell * 3f + FormationGap * 2f;
        rect.sizeDelta = new Vector2(span, span);
        rect.anchoredPosition = new Vector2(0f, -FormationTop);
        GridLayoutGroup layout = obj.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(FormationCell, FormationCell);
        layout.spacing = new Vector2(FormationGap, FormationGap);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        for (int y = 3; y >= 1; y--)
        {
            for (int x = 1; x <= 3; x++)
            {
                int cellX = x;
                int cellY = y;
                Button button = CreateButton(CellPrefix + x.ToString() + y.ToString(), obj.transform, "+", new Color(0.2f, 0.2f, 0.2f, 1f));
                Image image = button.GetComponent<Image>();
                image.sprite = WhiteSprite();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                button.onClick.AddListener(() => PickCell(cellX, cellY));
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.fontSize = 64;
                    label.color = Color.white;
                    label.alignment = TextAnchor.MiddleCenter;
                }
                _cells[x, y] = button;
            }
        }
        _formation = obj;
    }

    void ClearCells()
    {
        EnsureFormation();
        for (int x = 1; x <= 3; x++)
        {
            for (int y = 1; y <= 3; y++)
                _cellIds[x, y] = 0;
        }
        SyncSelected();
    }

    void PickCell(int x, int y)
    {
        if (_production == null || _cells == null || _cells[x, y] == null)
            return;
        _production.OpenShareStageCharacterPick(_cells[x, y], _cellIds[x, y]);
    }

    void ApplyPicked(Button cell, uint characterId)
    {
        if (!TryCellPosition(cell, out int x, out int y))
            return;
        if (characterId != 0 && !IsOwnCharacter(ImportedCharacters.Find(characterId)))
            return;
        if (characterId != 0)
        {
            for (int cx = 1; cx <= 3; cx++)
            {
                for (int cy = 1; cy <= 3; cy++)
                {
                    if ((cx != x || cy != y) && _cellIds[cx, cy] == characterId)
                        _cellIds[cx, cy] = 0;
                }
            }
        }
        _cellIds[x, y] = characterId;
        SyncSelected();
        PaintFormation();
        if (_countLabel != null)
            _countLabel.text = "選択 " + _selected.Count.ToString() + " / 9";
    }

    static bool TryCellPosition(Button cell, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (!IsCell(cell) || cell.name.Length < CellPrefix.Length + 2)
            return false;
        string tail = cell.name.Substring(CellPrefix.Length);
        if (tail.Length < 2)
            return false;
        x = tail[0] - '0';
        y = tail[1] - '0';
        return x >= 1 && x <= 3 && y >= 1 && y <= 3;
    }

    void SyncSelected()
    {
        _selected.Clear();
        if (_cellIds == null)
            return;
        for (int y = 3; y >= 1; y--)
        {
            for (int x = 1; x <= 3; x++)
            {
                if (_cellIds[x, y] != 0)
                    _selected.Add(_cellIds[x, y]);
            }
        }
    }

    void PaintFormation()
    {
        if (_cells == null)
            return;
        for (int x = 1; x <= 3; x++)
        {
            for (int y = 1; y <= 3; y++)
                PaintCell(x, y);
        }
    }

    void PaintCell(int x, int y)
    {
        Button cell = _cells[x, y];
        if (cell == null)
            return;
        Image image = cell.GetComponent<Image>();
        Text label = cell.GetComponentInChildren<Text>();
        uint id = _cellIds[x, y];
        if (id == 0 || !IsOwnCharacter(ImportedCharacters.Find(id)))
        {
            _cellIds[x, y] = 0;
            image.sprite = WhiteSprite();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            if (label != null)
            {
                label.text = "+";
                label.transform.SetAsLastSibling();
            }
            return;
        }
        CharacterClass character = ImportedCharacters.Find(id);
        Sprite sprite = SpriteFor(character);
        image.sprite = sprite != null ? sprite : WhiteSprite();
        image.color = Color.white;
        image.preserveAspect = true;
        if (label != null)
            label.text = "";
    }

    void CycleBackground()
    {
        _background++;
        if (_background >= ShareStageRules.Backgrounds.Length)
            _background = 0;
        RebuildCreateList();
    }

    void Preview()
    {
        _picked.Clear();
        for (int i = 0; i < _selected.Count; i++)
        {
            CharacterClass character = ImportedCharacters.Find(_selected[i]);
            if (character != null)
                _picked.Add(character);
        }
        PiquaDocument document;
        string error;
        if (!ShareStageLibrary.TryBuild("", _author.text, "", 0, 0, 0, ShareStageRules.Backgrounds[_background], _mineStageId, _picked, out document, out error))
        {
            _status.text = error;
            return;
        }
        _draft = document;
        _draftZip = PiquaCodec.Build(document);
        HideFields();
        ShowPreviewFormation();
        const float gridTop = 100f;
        const float cell = 72f;
        const float gap = 8f;
        float span = cell * 3f + gap * 2f;
        float statusTop = gridTop + span + 16f;
        _body.text = "";
        Place(_status.rectTransform, statusTop, 88f, 24f, 24f);
        SetListTop(statusTop + 100f);
        _title.text = "プレビュー";
        StringBuilder text = new StringBuilder();
        text.Append(ShownName(document.StageName, document.AuthorName));
        text.Append("\n投稿者 ").Append(document.AuthorName);
        if (!string.IsNullOrEmpty(document.Comment))
            text.Append("\n").Append(document.Comment);
        text.Append("\nキャラ ").Append(document.Characters.Count.ToString()).Append("体");
        text.Append("\n更新番号 ").Append(document.Revision.ToString());
        for (int i = 0; i < document.Characters.Count; i++)
            text.Append("\n").Append(document.Characters[i].Name).Append("  Lv").Append(document.Characters[i].Level.ToString());
        ClearList();
        AddCopy(text.ToString(), Color.white);
        AddButton("端末に保存", SaveDraft);
        AddButton("アップロードして共有", UploadDraft);
        AddButton("戻る", () =>
        {
            _title.text = string.IsNullOrEmpty(_mineStageId) ? "ステージを作る" : "ステージを更新";
            _body.text = "自分が作者のキャラを、マスに入れます。1〜9体。";
            _status.text = "";
            ShowFields();
            RebuildCreateList();
        });
        ResizeList();
        _status.text = "この内容は、今のキャラのコピーです。あとから育てても、出したファイルは変わりません。";
    }

    void EnsurePreviewFormation()
    {
        if (_previewFormation != null)
            return;
        const float cell = 72f;
        const float gap = 8f;
        float span = cell * 3f + gap * 2f;
        GameObject obj = new GameObject("ShareStagePreviewFormation", typeof(RectTransform), typeof(GridLayoutGroup));
        obj.transform.SetParent(_root.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(span, span);
        rect.anchoredPosition = new Vector2(0f, -100f);
        GridLayoutGroup layout = obj.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(cell, cell);
        layout.spacing = new Vector2(gap, gap);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        for (int y = 3; y >= 1; y--)
        {
            for (int x = 1; x <= 3; x++)
            {
                GameObject cellObject = new GameObject("PreviewCell" + x.ToString() + y.ToString(), typeof(RectTransform), typeof(Image));
                cellObject.transform.SetParent(obj.transform, false);
                Image image = cellObject.GetComponent<Image>();
                image.sprite = WhiteSprite();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                image.raycastTarget = false;
            }
        }
        _previewFormation = obj;
        obj.SetActive(false);
    }

    void ShowPreviewFormation()
    {
        EnsurePreviewFormation();
        _previewFormation.SetActive(true);
        _previewFormation.transform.SetAsLastSibling();
        int index = 0;
        for (int y = 3; y >= 1; y--)
        {
            for (int x = 1; x <= 3; x++)
            {
                Image image = _previewFormation.transform.GetChild(index).GetComponent<Image>();
                index++;
                uint id = _cellIds != null ? _cellIds[x, y] : 0;
                CharacterClass character = id == 0 ? null : ImportedCharacters.Find(id);
                if (!IsOwnCharacter(character))
                {
                    image.sprite = WhiteSprite();
                    image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    continue;
                }
                Sprite sprite = SpriteFor(character);
                image.sprite = sprite != null ? sprite : WhiteSprite();
                image.color = Color.white;
                image.preserveAspect = true;
            }
        }
    }

    void HidePreviewFormation()
    {
        if (_previewFormation != null)
            _previewFormation.SetActive(false);
    }

    Sprite SpriteFor(CharacterClass character)
    {
        if (character == null)
            return null;
        Sprite sprite;
        if (_cellSprites.TryGetValue(character.ID, out sprite) && sprite != null)
            return sprite;
        Texture2D texture = character.ImageTexture2D;
        if (texture == null)
            return null;
        texture.filterMode = FilterMode.Point;
        sprite = Sprite.Create(texture, new Rect(0f, 0f, character.Size, character.Size), new Vector2(0.5f, 0.5f));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        _cellSprites[character.ID] = sprite;
        return sprite;
    }

    void SaveDraft()
    {
        if (_draftZip == null || _busy)
            return;
        string fileName = SafeFile(_draft.StageName) + ".piqua";
        ShareStageFiles.SaveFile(_draftZip, fileName, ok =>
        {
            if (!ok)
            {
                _status.text = "保存を中止しました。";
                return;
            }
            ShareStageLibrary.RememberMine(_draft, _picked);
            if (_production != null)
                _production.SaveGame();
            _status.text = "保存しました。";
        });
    }

    void UploadDraft()
    {
        if (_draft == null || _draftZip == null || _busy)
            return;
        if (!ShareStageConfig.HasWorker)
        {
            _status.text = "アップロード先が未設定です。端末への保存はできます。";
            return;
        }
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            _status.text = "ネットワークに接続できません。";
            return;
        }
        StartCoroutine(Upload());
    }

    IEnumerator Upload()
    {
        _busy = true;
        _status.text = "アップロードしています…";
        string url = ShareStageConfig.WorkerBaseUrl + "/v1/files";
        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(_draftZip);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            request.timeout = 60;
            yield return request.SendWebRequest();
            _busy = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                _status.text = UploadFailure(request);
                yield break;
            }
            ShareStageLibrary.RememberMine(_draft, _picked);
            if (_production != null)
                _production.SaveGame();
            string download = ShareStageConfig.FileUrl(_draft.FileId);
            _status.text = "アップロードしました。共有シートを開きます。";
            yield return CaptureAndShare(download);
        }
    }

    IEnumerator CaptureAndShare(string download)
    {
        string text = ShareText(_draft, download);
        string image = ShareStageFiles.CacheFile("card.png");
        yield return DrawCard(_draft, download, image);
        ShareStageFiles.Share(text, image);
        _awaitReward = true;
        _status.text = "投稿できたら、ゲームに戻って報酬を押してください。投稿したかどうかは、アプリからは確認できません。報酬は1日1回です。";
        ClearList();
        AddButton("共有したので報酬を受け取る", ClaimReward);
        AddButton("一覧へ戻る", ReturnToList);
        ResizeList();
    }

    void ClaimReward()
    {
        if (_production == null || _draft == null)
            return;
        _status.text = _production.GrantShareReward(_draft);
        _awaitReward = false;
    }

    void ShowLoad()
    {
        EnsureFields();
        HideFields();
        SetListTop(420f);
        _url.gameObject.SetActive(true);
        _url.text = "";
        _title.text = "ステージを読み込む";
        _body.text = "ダウンロードした .piqua を選ぶか、共有された URL を貼ります。";
        _status.text = "";
        ClearList();
        AddButton("ファイルを選ぶ", PickFile);
        AddButton("URLから読み込む", DownloadUrl);
        AddButton("ステージを作る", ShowCreate);
        ResizeList();
    }

    void PickFile()
    {
        if (_busy)
            return;
        ShareStageFiles.Pick(path =>
        {
            if (string.IsNullOrEmpty(path))
            {
                _status.text = "ファイル選択を中止しました。";
                return;
            }
            AcceptFile(path);
        });
    }

    void AcceptFile(string path)
    {
        try
        {
            FileInfo info = new FileInfo(path);
            if (!info.Exists || info.Length > ShareStageRules.MaxZipBytes)
            {
                _status.text = "ファイル容量が上限を超えています。";
                return;
            }
            AcceptBytes(File.ReadAllBytes(path));
        }
        catch (Exception)
        {
            _status.text = "ファイルが破損しています。";
        }
    }

    void DownloadUrl()
    {
        if (_busy || _url == null)
            return;
        string url = (_url.text ?? "").Trim();
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _status.text = "URLが正しくありません。";
            return;
        }
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            _status.text = "ネットワークに接続できません。";
            return;
        }
        StartCoroutine(Download(url));
    }

    IEnumerator Download(string url)
    {
        _busy = true;
        _status.text = "ダウンロードしています…";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 60;
            yield return request.SendWebRequest();
            _busy = false;
            if (request.responseCode == 404)
            {
                _status.text = "ファイルが見つかりません。";
                yield break;
            }
            if (request.result != UnityWebRequest.Result.Success)
            {
                _status.text = request.responseCode == 0 ? "ダウンロードに失敗しました。" : ServerError(request, "ダウンロードに失敗しました。");
                yield break;
            }
            byte[] body = request.downloadHandler.data;
            if (body == null || body.Length > ShareStageRules.MaxZipBytes)
            {
                _status.text = "ファイル容量が上限を超えています。";
                yield break;
            }
            AcceptBytes(body);
        }
    }

    void AcceptBytes(byte[] body)
    {
        if (_production != null && _production.IsBattleRunning())
        {
            _status.text = "戦闘中は読み込めません。";
            return;
        }
        PiquaDocument document;
        string error;
        if (!PiquaCodec.TryParse(body, out document, out error))
        {
            _status.text = error;
            return;
        }
        if (!ShareStageLibrary.TryAccept(document, _production != null ? _production.BattleBalancePublic() : null, out error))
        {
            _status.text = error;
            return;
        }
        if (_production != null)
            _production.SaveGame();
        _url.gameObject.SetActive(false);
        StartCoroutine(PlayLoaded(document, error));
    }

    IEnumerator PlayLoaded(PiquaDocument document, string note)
    {
        _title.text = ShownName(document.StageName, document.AuthorName);
        _body.text = "";
        _status.text = "";
        ClearList();
        GameObject flash = new GameObject("ShareLoadFx", typeof(RectTransform), typeof(Image));
        flash.transform.SetParent(_root.transform, false);
        Stretch(flash.GetComponent<RectTransform>());
        Image glow = flash.GetComponent<Image>();
        glow.color = new Color(document.MainR / 255f, document.MainG / 255f, document.MainB / 255f, 0.15f);
        Text heading = CreateText(flash.transform, ShownName(document.StageName, document.AuthorName), 48, TextAnchor.MiddleCenter);
        Place(heading.rectTransform, 180f, 100f, 24f, 24f);
        bool skip = false;
        Button skipButton = CreateButton("ButtonShareSkip", flash.transform, "スキップ", new Color(0.2f, 0.2f, 0.24f, 1f));
        RectTransform skipRect = skipButton.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 0f);
        skipRect.anchorMax = new Vector2(1f, 0f);
        skipRect.pivot = new Vector2(1f, 0f);
        skipRect.anchoredPosition = new Vector2(-24f, 24f);
        skipRect.sizeDelta = new Vector2(220f, 88f);
        skipButton.onClick.AddListener(() => skip = true);
        for (int i = 0; i < document.Characters.Count; i++)
        {
            Text line = CreateText(flash.transform, document.Characters[i].Name, 36, TextAnchor.MiddleCenter);
            Place(line.rectTransform, 320f + i * 64f, 56f, 24f, 24f);
            float until = Time.unscaledTime + 0.35f;
            while (Time.unscaledTime < until && !skip)
                yield return null;
            if (skip)
                break;
        }
        if (!skip)
        {
            Text done = CreateText(flash.transform, "STAGE LOADED!", 42, TextAnchor.MiddleCenter);
            Place(done.rectTransform, 980f, 80f, 24f, 24f);
            float until = Time.unscaledTime + 0.8f;
            while (Time.unscaledTime < until && !skip)
                yield return null;
        }
        Destroy(flash);
        _status.text = note;
        if (_url != null)
            _url.gameObject.SetActive(false);
        HideFields();
        Close();
        if (_stage != null)
            _stage.SelectSharedStage(document.AuthorId + "_" + document.StageId, note);
        if (_production != null)
            _production.OpenBattleStage();
    }

    static string UploadFailure(UnityWebRequest request)
    {
        if (request.responseCode == 0)
            return "アップロードに失敗しました。";
        if (request.responseCode == 413)
            return "ファイル容量が上限を超えています。";
        if (request.responseCode == 429)
            return "きょうのアップロード回数を超えています。";
        return ServerError(request, "サーバーで受け付けられませんでした。");
    }

    static string ServerError(UnityWebRequest request, string fallback)
    {
        string body = request.downloadHandler != null ? request.downloadHandler.text : "";
        const string key = "\"error\":\"";
        int start = string.IsNullOrEmpty(body) ? -1 : body.IndexOf(key, StringComparison.Ordinal);
        if (start < 0)
            return fallback;
        start += key.Length;
        int end = body.IndexOf('"', start);
        if (end <= start || end - start > 80)
            return fallback;
        return body.Substring(start, end - start);
    }

    IEnumerator DrawCard(PiquaDocument document, string download, string path)
    {
        GameObject root = new GameObject("ShareCardCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        Camera camera = new GameObject("ShareCardCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(document.MainR / 255f, document.MainG / 255f, document.MainB / 255f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 540f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.enabled = false;
        canvas.worldCamera = camera;
        canvas.planeDistance = 10f;
        RectTransform canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1080f, 1080f);
        Texture2D logo = LoadLogoTexture();
        if (logo == null)
        {
            byte[] remote = null;
            string streaming = Application.streamingAssetsPath + "/title/titlelogo.png";
            using (UnityWebRequest request = UnityWebRequest.Get(streaming))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    remote = request.downloadHandler.data;
            }
            logo = TextureFromPng(remote);
        }
        if (logo != null)
        {
            RawImage mark = new GameObject("CardLogo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            mark.transform.SetParent(root.transform, false);
            mark.texture = logo;
            mark.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            mark.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            mark.rectTransform.pivot = new Vector2(0.5f, 1f);
            mark.rectTransform.sizeDelta = new Vector2(320f, 72f);
            mark.rectTransform.anchoredPosition = new Vector2(0f, -12f);
        }
        Text title = CreateText(root.transform, ShownName(document.StageName, document.AuthorName), 64, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 92f, 64f, 40f, 40f);
        Text by = CreateText(root.transform, document.AuthorName, 36, TextAnchor.MiddleCenter);
        Place(by.rectTransform, 160f, 44f, 40f, 40f);
        int count = document.Characters.Count;
        int columns = count <= 1 ? 1 : count <= 3 ? count : 3;
        int rows = count <= 3 ? 1 : count <= 6 ? 2 : 3;
        float cell = rows == 1 ? (count == 1 ? 460f : 250f) : rows == 2 ? 240f : 180f;
        float gap = 16f;
        float block = rows * cell + (rows - 1) * gap;
        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            float x = (column - (columns - 1) * 0.5f) * (cell + gap);
            float y = (block - cell) * 0.5f - row * (cell + gap) - 20f;
            RawImage picture = new GameObject("CardFace", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            picture.transform.SetParent(root.transform, false);
            Texture source = _picked.Count > i && _picked[i].ImageTexture2D != null ? _picked[i].ImageTexture2D : Texture2D.whiteTexture;
            if (source != Texture2D.whiteTexture)
                source.filterMode = FilterMode.Point;
            picture.texture = source;
            picture.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            picture.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            picture.rectTransform.sizeDelta = new Vector2(cell, cell);
            picture.rectTransform.anchoredPosition = new Vector2(x, y);
        }
        Text footer = CreateText(root.transform, count.ToString() + "体  ピクあ！で遊べます", 32, TextAnchor.MiddleCenter);
        Place(footer.rectTransform, 900f, 50f, 40f, 40f);
        string shortUrl = string.IsNullOrEmpty(download) ? "" : download;
        if (shortUrl.Length > 48)
            shortUrl = shortUrl.Substring(0, 48) + "…";
        Text link = CreateText(root.transform, shortUrl, 22, TextAnchor.MiddleCenter);
        Place(link.rectTransform, 960f, 40f, 24f, 24f);
        yield return null;
        RenderTexture target = new RenderTexture(1080, 1080, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D image = new Texture2D(1080, 1080, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0f, 0f, 1080f, 1080f), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        try
        {
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        catch (Exception exception)
        {
            Debug.LogWarning("紹介画像を保存できませんでした。 " + exception.Message);
        }
        camera.targetTexture = null;
        Destroy(image);
        Destroy(target);
        if (logo != null)
            Destroy(logo);
        Destroy(camera.gameObject);
        Destroy(root);
        yield return null;
    }

    static Texture2D LoadLogoTexture()
    {
#if UNITY_EDITOR
        string editor = Path.Combine(Application.dataPath, "Images", "title", "titlelogo.png");
        if (File.Exists(editor))
            return TextureFromPng(File.ReadAllBytes(editor));
#endif
        string streaming = Application.streamingAssetsPath + "/title/titlelogo.png";
        if (!streaming.Contains("://") && File.Exists(streaming))
            return TextureFromPng(File.ReadAllBytes(streaming));
        return null;
    }

    static Texture2D TextureFromPng(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            return null;
        }
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    static string ShareText(PiquaDocument document, string download)
    {
        StringBuilder text = new StringBuilder();
        text.Append("ピクあ！の共有ステージ「").Append(ShownName(document.StageName, document.AuthorName)).Append("」\n");
        text.Append("投稿者: ").Append(document.AuthorName).Append("\n");
        if (!string.IsNullOrEmpty(document.Comment))
            text.Append(document.Comment).Append("\n");
        text.Append("キャラ ").Append(document.Characters.Count.ToString()).Append("体 / 300階\n");
        if (!string.IsNullOrEmpty(download))
            text.Append(download).Append("\n");
        text.Append("#ピクあ");
        return text.ToString();
    }

    void ShowMessage(string message)
    {
        _title.text = "共有ステージ";
        _body.text = message;
        _status.text = "";
        ClearList();
        AddButton("閉じる", Close);
        ResizeList();
    }

    void HideFields()
    {
        SetActive(_author, false);
        SetActive(_countLabel != null ? _countLabel.gameObject : null, false);
        SetActive(_url, false);
        SetActive(_createButton, false);
        if (_formation != null)
            _formation.SetActive(false);
        HidePreviewFormation();
        if (_status != null)
            Place(_status.rectTransform, 210f, 70f, 24f, 24f);
    }

    void ShowFields()
    {
        SetActive(_author, true);
        SetActive(_countLabel != null ? _countLabel.gameObject : null, true);
        SetActive(_url, false);
        SetActive(_createButton, true);
        if (_createButton != null)
            _createButton.transform.SetAsLastSibling();
        if (_formation != null)
            _formation.SetActive(true);
        HidePreviewFormation();
        if (_status != null)
            Place(_status.rectTransform, 210f, 70f, 24f, 24f);
    }

    void RememberAuthor(string name)
    {
        ShareStageLibrary.RememberAuthorName(name);
        if (_production != null)
            _production.SaveGame();
    }

    void CreateStage()
    {
        if (_author != null)
            RememberAuthor(_author.text);
        Preview();
    }

    static string ShownName(string name, string author)
    {
        if (!string.IsNullOrEmpty(name))
            return name;
        return string.IsNullOrEmpty(author) ? "ななし" : author;
    }

    static void SetActive(Component component, bool active)
    {
        if (component != null)
            component.gameObject.SetActive(active);
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    void SetListTop(float top)
    {
        SetListTop(top, 24f);
    }

    void SetListTop(float top, float bottom)
    {
        if (_list == null || _list.parent == null)
            return;
        RectTransform scroll = _list.parent as RectTransform;
        if (scroll == null)
            return;
        Pin(scroll, top, bottom, 16f, 16f);
    }

    InputField CreateField(string placeholder, float top)
    {
        GameObject row = new GameObject(placeholder, typeof(RectTransform), typeof(Image), typeof(InputField));
        row.transform.SetParent(_root.transform, false);
        Place(row.GetComponent<RectTransform>(), top, 72f, 24f, 24f);
        row.GetComponent<Image>().color = Color.white;
        Text text = CreateText(row.transform, "", 28, TextAnchor.MiddleLeft);
        text.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(16f, 0f);
        text.rectTransform.offsetMax = new Vector2(-16f, 0f);
        InputField field = row.GetComponent<InputField>();
        field.textComponent = text;
        field.targetGraphic = row.GetComponent<Image>();
        field.characterLimit = placeholder == "コメント" ? ShareStageRules.MaxCommentChars : placeholder == "ダウンロードURL" ? 200 : ShareStageRules.MaxNameChars;
        if (placeholder == "投稿者名")
            field.characterLimit = ShareStageRules.MaxAuthorChars;
        Text hint = CreateText(row.transform, placeholder, 24, TextAnchor.MiddleLeft);
        hint.color = new Color(0.45f, 0.45f, 0.5f, 1f);
        Stretch(hint.rectTransform);
        hint.rectTransform.offsetMin = new Vector2(16f, 0f);
        field.placeholder = hint;
        return field;
    }

    void AddCopy(string value, Color color)
    {
        int lines = 1;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\n')
                lines++;
        }
        GameObject row = new GameObject("ShareCopy", typeof(RectTransform));
        row.transform.SetParent(_list, false);
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        float height = lines * 36f + 8f;
        rect.sizeDelta = new Vector2(-16f, height);
        rect.anchoredPosition = new Vector2(0f, -ListHeight());
        Text text = CreateText(row.transform, value, 26, TextAnchor.UpperLeft);
        text.color = color;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(8f, 0f);
        text.rectTransform.offsetMax = new Vector2(-8f, 0f);
    }

    void AddButton(string label, Action onClick)
    {
        Button button = CreateButton("ShareRow" + _list.childCount.ToString(), _list, label, new Color(0.16f, 0.18f, 0.24f, 1f));
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        float height = label.IndexOf('\n') >= 0 ? 140f : 96f;
        rect.sizeDelta = new Vector2(-16f, height);
        rect.anchoredPosition = new Vector2(0f, -ListHeight());
        button.onClick.AddListener(() => onClick());
        if (_production != null)
            _production.StyleRepresentativeButton(button, Color.white, true);
        Text labelText = button.GetComponentInChildren<Text>();
        if (labelText != null)
        {
            labelText.color = Color.white;
            labelText.transform.SetAsLastSibling();
        }
    }

    float ListHeight()
    {
        float height = 0f;
        for (int i = 0; i < _list.childCount; i++)
        {
            RectTransform child = _list.GetChild(i) as RectTransform;
            if (child != null)
                height += child.sizeDelta.y + 12f;
        }
        return height;
    }

    void ResizeList()
    {
        _list.sizeDelta = new Vector2(0f, ListHeight() + 24f);
    }

    void ClearList()
    {
        for (int i = _list.childCount - 1; i >= 0; i--)
            Destroy(_list.GetChild(i).gameObject);
    }

    Button CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = color;
        Button button = buttonObject.GetComponent<Button>();
        Text text = CreateText(buttonObject.transform, label, 28, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(12f, 8f);
        text.rectTransform.offsetMax = new Vector2(-12f, -8f);
        GameAudio.BindUi(button);
        return button;
    }

    Text CreateText(Transform parent, string value, int size, TextAnchor anchor)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    static Sprite _whiteSprite;

    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;
        Texture2D texture = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return _whiteSprite;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Pin(RectTransform rect, float top, float bottom, float left, float right)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    static void Place(RectTransform rect, float top, float height, float left, float right)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -(top + height));
        rect.offsetMax = new Vector2(-right, -top);
    }

    static int BackgroundIndex(string name)
    {
        for (int i = 0; i < ShareStageRules.Backgrounds.Length; i++)
        {
            if (ShareStageRules.Backgrounds[i] == name)
                return i;
        }
        return 0;
    }

    static string SafeFile(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "stage";
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
        if (string.IsNullOrEmpty(name))
            return "stage";
        return name;
    }

    void OnApplicationPause(bool pause)
    {
        if (!pause)
            OfferReward();
    }

    void OnApplicationFocus(bool focus)
    {
        if (focus)
            OfferReward();
    }

    void OfferReward()
    {
        if (!_awaitReward || _root == null || !_root.activeSelf)
            return;
        _status.text = "Xへの投稿完了は確認できません。投稿したあとに、下のボタンを押すと報酬です。1日1回までです。";
    }
}
