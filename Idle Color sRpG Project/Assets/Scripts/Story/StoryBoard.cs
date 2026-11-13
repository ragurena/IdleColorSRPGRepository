using UnityEngine;
using UnityEngine.UI;

// 章の一覧と、1ページずつの閲覧。判定と保存は StoryState に任せる。
public sealed class StoryBoard
{
    static readonly Color Ink = new Color(0.96f, 0.96f, 0.94f, 1f);
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.62f);
    static readonly Color Plate = new Color(0.93f, 0.94f, 0.96f, 1f);
    static readonly Color ButtonFill = new Color(0.12f, 0.14f, 0.18f, 0.9f);

    ControllerProduction _game;
    Font _font;
    GameObject _root;
    GameObject _listPage;
    GameObject _reader;
    RectTransform _listContent;
    Image _background;
    Image _dim;
    Text _heading;
    Text _body;
    Text _pageCount;
    Text _lock;
    Button _prev;
    Button _next;
    StoryChapterDef _chapter;
    int _pageIndex = -1;

    public bool IsOpen
    {
        get { return _root != null && _root.activeSelf; }
    }

    public void Build(Transform parent, Font font, ControllerProduction game)
    {
        _font = font;
        _game = game;
        _root = new GameObject("PanelStory", typeof(RectTransform), typeof(Image));
        _root.transform.SetParent(parent, false);
        _root.GetComponent<Image>().color = Plate;
        Stretch(_root.GetComponent<RectTransform>());
        BuildList();
        BuildReader();
        _root.SetActive(false);
    }

    public void Open()
    {
        if (_root == null || _game == null)
            return;
        _game.CatchUpStory();
        _root.SetActive(true);
        Transform overlay = _root.transform.parent;
        if (overlay != null)
            overlay.SetAsLastSibling();
        _root.transform.SetAsLastSibling();
        ShowList();
    }

    public void Close()
    {
        if (_reader != null)
            _reader.SetActive(false);
        if (_root != null)
            _root.SetActive(false);
        _chapter = null;
        _pageIndex = -1;
    }

    void BuildList()
    {
        _listPage = new GameObject("PageStoryList", typeof(RectTransform));
        _listPage.transform.SetParent(_root.transform, false);
        Stretch(_listPage.GetComponent<RectTransform>());

        Text title = CreateText("TextStoryTitle", _listPage.transform, 35, TextAnchor.MiddleCenter);
        title.text = "ストーリー";
        title.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        PlaceTop(title.rectTransform, 16f, 0f, 360f, 56f);

        Button close = CreateButton("ButtonStoryClose", _listPage.transform, "閉じる", new Color(0.75f, 0.28f, 0.24f, 1f));
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-16f, -12f);
        closeRect.sizeDelta = new Vector2(140f, 64f);
        close.onClick.AddListener(Close);

        _listContent = CreateScroll("ScrollStoryList", _listPage.transform, 88f, 16f);
    }

    void BuildReader()
    {
        _reader = new GameObject("PageStoryReader", typeof(RectTransform));
        _reader.transform.SetParent(_root.transform, false);
        Stretch(_reader.GetComponent<RectTransform>());

        _background = CreateImage("ImageStoryBackground", _reader.transform, StoryCatalog.CommonBackground);
        Stretch(_background.rectTransform);
        _background.raycastTarget = false;

        _dim = CreateImage("ImageStoryDim", _reader.transform, Dim);
        Stretch(_dim.rectTransform);
        _dim.raycastTarget = false;

        _heading = CreateText("TextStoryHeading", _reader.transform, 36, TextAnchor.MiddleCenter);
        _heading.color = Ink;
        _heading.raycastTarget = false;
        _heading.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform headingRect = _heading.rectTransform;
        headingRect.anchorMin = new Vector2(0.5f, 1f);
        headingRect.anchorMax = new Vector2(0.5f, 1f);
        headingRect.pivot = new Vector2(0.5f, 1f);
        headingRect.anchoredPosition = new Vector2(0f, -24f);
        headingRect.sizeDelta = new Vector2(640f, 72f);
        Outline headingOutline = _heading.gameObject.AddComponent<Outline>();
        headingOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        headingOutline.effectDistance = new Vector2(1.5f, -1.5f);

        _body = CreateText("TextStoryBody", _reader.transform, 40, TextAnchor.MiddleCenter);
        _body.color = Ink;
        _body.lineSpacing = 1.2f;
        _body.alignByGeometry = true;
        _body.supportRichText = false;
        _body.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform bodyRect = _body.rectTransform;
        bodyRect.anchorMin = new Vector2(0.08f, 0.22f);
        bodyRect.anchorMax = new Vector2(0.92f, 0.82f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        Outline bodyOutline = _body.gameObject.AddComponent<Outline>();
        bodyOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        bodyOutline.effectDistance = new Vector2(1.5f, -1.5f);

        _lock = CreateText("TextStoryLock", _reader.transform, 28, TextAnchor.MiddleCenter);
        _lock.color = Ink;
        _lock.lineSpacing = 1.05f;
        RectTransform lockRect = _lock.rectTransform;
        lockRect.anchorMin = new Vector2(0.06f, 0f);
        lockRect.anchorMax = new Vector2(0.94f, 0f);
        lockRect.pivot = new Vector2(0.5f, 0f);
        lockRect.offsetMin = new Vector2(0f, 128f);
        lockRect.offsetMax = new Vector2(0f, 224f);
        Outline lockOutline = _lock.gameObject.AddComponent<Outline>();
        lockOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        lockOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button back = CreateButton("ButtonStoryBack", _reader.transform, "章へ", ButtonFill);
        RectTransform backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 1f);
        backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition = new Vector2(24f, -24f);
        backRect.sizeDelta = new Vector2(140f, 72f);
        back.onClick.AddListener(ShowList);

        _pageCount = CreateText("TextStoryPage", _reader.transform, 30, TextAnchor.MiddleCenter);
        _pageCount.color = Ink;
        _pageCount.raycastTarget = false;
        RectTransform pageRect = _pageCount.rectTransform;
        pageRect.anchorMin = new Vector2(0.5f, 0f);
        pageRect.anchorMax = new Vector2(0.5f, 0f);
        pageRect.pivot = new Vector2(0.5f, 0f);
        pageRect.anchoredPosition = new Vector2(0f, 28f);
        pageRect.sizeDelta = new Vector2(280f, 84f);
        Outline pageOutline = _pageCount.gameObject.AddComponent<Outline>();
        pageOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        pageOutline.effectDistance = new Vector2(1.5f, -1.5f);

        _prev = CreateButton("ButtonStoryPrev", _reader.transform, "前へ", ButtonFill);
        PlaceCorner(_prev.GetComponent<RectTransform>(), true);
        _prev.onClick.AddListener(PushPrev);

        _next = CreateButton("ButtonStoryNext", _reader.transform, "次へ", ButtonFill);
        PlaceCorner(_next.GetComponent<RectTransform>(), false);
        _next.onClick.AddListener(PushNext);

        _reader.SetActive(false);
    }

    void ShowList()
    {
        _chapter = null;
        _pageIndex = -1;
        if (_reader != null)
            _reader.SetActive(false);
        if (_listPage != null)
            _listPage.SetActive(true);
        Fill();
        if (_game != null)
            _game.RefreshStoryMarks();
    }

    void Fill()
    {
        if (_listContent == null || _game == null || _game.Story == null)
            return;
        Clear(_listContent);
        StoryChapterDef[] chapters = StoryCatalog.All;
        if (chapters == null || chapters.Length == 0)
        {
            Text empty = CreateText("TextStoryEmpty", _listContent, 28, TextAnchor.MiddleCenter);
            empty.text = "まだストーリーはありません";
            empty.color = new Color(0.28f, 0.30f, 0.34f, 1f);
            LayoutElement element = empty.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 120f;
            element.preferredHeight = 120f;
            return;
        }
        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i] != null)
                AddChapter(chapters[i]);
        }
    }

    void AddChapter(StoryChapterDef chapter)
    {
        bool open = _game.Story.ChapterUnlocked(_game, chapter);
        bool fresh = _game.Story.ChapterHasNew(_game, chapter);
        string note = _game.Story.BannerNote(_game, chapter);

        Image plate = CreateImage("Chapter_" + chapter.Id.ToString(), _listContent, chapter.BackgroundColor);
        LayoutElement element = plate.gameObject.AddComponent<LayoutElement>();
        element.minHeight = 240f;
        element.preferredHeight = 240f;
        Button button = plate.gameObject.AddComponent<Button>();
        button.targetGraphic = plate;
        int chapterId = chapter.Id;
        button.onClick.AddListener(() => OpenChapter(chapterId));

        Sprite banner = StoryImages.SpriteOf(chapter.BannerFile);
        if (banner != null)
        {
            plate.sprite = banner;
            plate.color = open ? Color.white : new Color(0.55f, 0.55f, 0.58f, 1f);
            plate.preserveAspect = false;
        }
        else if (!open)
            plate.color = new Color(chapter.BackgroundColor.r * 0.55f, chapter.BackgroundColor.g * 0.55f, chapter.BackgroundColor.b * 0.55f, 1f);

        Image bar = CreateImage("Bar", plate.transform, new Color(0f, 0f, 0f, 0.62f));
        RectTransform barRect = bar.rectTransform;
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(0f, string.IsNullOrEmpty(note) ? 72f : 108f);
        barRect.anchoredPosition = Vector2.zero;
        bar.raycastTarget = false;

        Text title = CreateText("Title", bar.transform, 32, TextAnchor.MiddleLeft);
        title.text = chapter.Title;
        title.color = Color.white;
        title.fontStyle = FontStyle.Bold;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, string.IsNullOrEmpty(note) ? 0f : 0.4f);
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(20f, 0f);
        titleRect.offsetMax = new Vector2(-20f, 0f);

        if (!string.IsNullOrEmpty(note))
        {
            Text detail = CreateText("Note", bar.transform, 24, TextAnchor.MiddleLeft);
            detail.text = note;
            detail.color = new Color(1f, 0.9f, 0.75f, 1f);
            RectTransform detailRect = detail.rectTransform;
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = new Vector2(1f, 0.48f);
            detailRect.offsetMin = new Vector2(20f, 8f);
            detailRect.offsetMax = new Vector2(-20f, 0f);
        }

        if (fresh)
            AddNewMark(plate.transform);
    }

    void AddNewMark(Transform parent)
    {
        Text mark = CreateText("StoryNewMark", parent, 31, TextAnchor.UpperRight);
        mark.text = "New!";
        mark.fontStyle = FontStyle.Bold;
        mark.color = new Color(1f, 0.16f, 0.16f, 1f);
        mark.horizontalOverflow = HorizontalWrapMode.Overflow;
        mark.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = mark.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-12f, -8f);
        rect.sizeDelta = new Vector2(140f, 44f);
        Outline outline = mark.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2.1f, -2.1f);
    }

    void OpenChapter(int chapterId)
    {
        if (_game == null || _game.Story == null)
            return;
        StoryChapterDef chapter = StoryCatalog.Find(chapterId);
        if (chapter == null || !_game.Story.ChapterUnlocked(_game, chapter))
            return;
        int index = _game.Story.StartIndex(chapter);
        if (index < 0)
            return;
        ShowReader(chapter, index);
    }

    void ShowReader(StoryChapterDef chapter, int index)
    {
        _chapter = chapter;
        if (_listPage != null)
            _listPage.SetActive(false);
        if (_reader != null)
            _reader.SetActive(true);
        ShowPage(index);
    }

    void ShowPage(int index)
    {
        if (_chapter == null || _chapter.Pages == null || _game == null || _game.Story == null)
            return;
        if (index < 0 || index >= _chapter.Pages.Length)
            return;
        StoryPageDef page = _chapter.Pages[index];
        if (page == null || !_game.Story.IsOpen(page.Id))
            return;
        _pageIndex = index;
        _game.NoteStoryPage(_chapter.Id, page);
        Paint(page);
    }

    void Paint(StoryPageDef page)
    {
        Canvas.ForceUpdateCanvases();
        Color color = _chapter != null ? _chapter.BackgroundColor : StoryCatalog.CommonBackground;
        Sprite sprite = StoryImages.SpriteOf(page.BackgroundFile);
        if (sprite != null)
        {
            _background.sprite = sprite;
            _background.color = Color.white;
            _background.preserveAspect = true;
            FitCover(_background.rectTransform, sprite);
        }
        else
        {
            _background.sprite = null;
            _background.color = color;
            _background.preserveAspect = false;
            Stretch(_background.rectTransform);
        }

        if (_heading != null)
            _heading.text = _chapter != null ? _chapter.Title : "";
        if (_pageCount != null)
        {
            int total = _chapter != null && _chapter.Pages != null ? _chapter.Pages.Length : 0;
            _pageCount.text = page.Number.ToString() + " / " + total.ToString();
        }

        bool still = page.Still;
        if (_dim != null)
            _dim.gameObject.SetActive(!still);
        if (_body != null)
        {
            _body.gameObject.SetActive(!still);
            if (!still)
            {
                _body.text = page.Text;
                _body.fontSize = BodySize();
            }
        }

        bool hasPrev = _pageIndex > 0 && _chapter.Pages[_pageIndex - 1] != null && _game.Story.IsOpen(_chapter.Pages[_pageIndex - 1].Id);
        if (_prev != null)
            _prev.gameObject.SetActive(hasPrev);

        StoryPageDef next = _pageIndex + 1 < _chapter.Pages.Length ? _chapter.Pages[_pageIndex + 1] : null;
        bool nextOpen = next != null && _game.Story.IsOpen(next.Id);
        if (_next != null)
            _next.gameObject.SetActive(nextOpen);
        if (_lock != null)
        {
            bool showLock = next != null && !nextOpen;
            _lock.gameObject.SetActive(showLock);
            if (showLock)
                _lock.text = StoryCatalog.UnlockLabel(next.UnlockStage, next.UnlockFloor);
        }
    }

    void PushPrev()
    {
        if (_chapter == null || _pageIndex <= 0)
            return;
        ShowPage(_pageIndex - 1);
    }

    void PushNext()
    {
        if (_chapter == null || _chapter.Pages == null)
            return;
        ShowPage(_pageIndex + 1);
    }

    int BodySize()
    {
        float width = 1080f;
        if (_root != null)
        {
            RectTransform rect = _root.GetComponent<RectTransform>();
            if (rect != null && rect.rect.width >= 8f)
                width = rect.rect.width;
        }
        float size = width / 1080f * 40f;
        if (size < 28f)
            size = 28f;
        if (size > 48f)
            size = 48f;
        return Mathf.RoundToInt(size);
    }

    void FitCover(RectTransform rect, Sprite sprite)
    {
        RectTransform view = _reader != null ? _reader.GetComponent<RectTransform>() : null;
        float viewW = view != null ? view.rect.width : 0f;
        float viewH = view != null ? view.rect.height : 0f;
        if (viewW < 8f)
            viewW = 1080f;
        if (viewH < 8f)
            viewH = 1920f;
        float spriteW = sprite.rect.width;
        float spriteH = sprite.rect.height;
        if (spriteW < 1f)
            spriteW = 1f;
        if (spriteH < 1f)
            spriteH = 1f;
        float spriteAspect = spriteW / spriteH;
        float viewAspect = viewW / viewH;
        float width;
        float height;
        if (spriteAspect > viewAspect)
        {
            height = viewH;
            width = height * spriteAspect;
        }
        else
        {
            width = viewW;
            height = width / spriteAspect;
        }
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
    }

    RectTransform CreateScroll(string name, Transform parent, float top, float bottom)
    {
        GameObject scrollObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);
        scrollObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(0f, bottom);
        scrollRect.offsetMax = new Vector2(0f, -top);
        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        Stretch(viewport.GetComponent<RectTransform>());
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.padding = new RectOffset(20, 20, 12, 24);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;
        return contentRect;
    }

    Button CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        Text text = CreateText("Text", obj.transform, 28, TextAnchor.MiddleCenter);
        text.text = label;
        text.color = Color.white;
        text.raycastTarget = false;
        Stretch(text.rectTransform);
        return obj.GetComponent<Button>();
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

    static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    static void Clear(RectTransform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
    }

    static void PlaceTop(RectTransform rect, float yFromTop, float x, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -yFromTop);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void PlaceCorner(RectTransform rect, bool left)
    {
        rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
        rect.anchorMax = new Vector2(left ? 0f : 1f, 0f);
        rect.pivot = new Vector2(left ? 0f : 1f, 0f);
        rect.anchoredPosition = new Vector2(left ? 24f : -24f, 28f);
        rect.sizeDelta = new Vector2(180f, 84f);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
