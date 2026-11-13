using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 所持アイテムの一覧。経験値玉は同じ大きさのキャラに、1 / 10 / 100 / 1000 個ずつ使う。
public sealed class ItemBoard
{
    static readonly int[] Quantities = { 1, 10, 100, 1000 };
    static readonly Color SelectedCell = new Color(0.62f, 0.84f, 0.66f, 1f);
    static readonly Color QuantityOn = new Color(0.22f, 0.58f, 0.34f, 1f);
    static readonly Color QuantityOff = new Color(0.28f, 0.42f, 0.62f, 1f);
    static readonly Color QuantityLocked = new Color(0.62f, 0.64f, 0.68f, 1f);
    static readonly Color Ink = new Color(0.12f, 0.14f, 0.18f, 1f);
    static readonly Color SubInk = new Color(0.28f, 0.30f, 0.34f, 1f);

    sealed class OrbCell
    {
        public Image Plate;
        public Text Level;
        public Image ExpFill;
    }

    ControllerProduction _game;
    Font _font;
    GameObject _root;
    RectTransform _content;
    GameObject _usePage;
    RectTransform _characterContent;
    Text _useTitle;
    Text _ownedLine;
    Text _detailName;
    Text _detailLevel;
    Text _detailStats;
    Image _portrait;
    Image _barNow;
    Image _barAfter;
    Text _barNowLabel;
    Text _barAfterLabel;
    Button _useButton;
    Image _useButtonImage;
    readonly Button[] _quantityButtons = new Button[4];
    readonly Image[] _quantityImages = new Image[4];
    readonly List<int> _ids = new List<int>();
    readonly List<uint> _characters = new List<uint>();
    readonly Dictionary<uint, Sprite> _sprites = new Dictionary<uint, Sprite>();
    readonly Dictionary<uint, OrbCell> _cells = new Dictionary<uint, OrbCell>();
    int _itemId;
    int _quantity = 1;
    uint _characterId;

    public bool IsOpen
    {
        get { return _root != null && _root.activeSelf; }
    }

    public void Build(Transform parent, Font font, ControllerProduction game)
    {
        _font = font;
        _game = game;
        _root = new GameObject("PanelItem", typeof(RectTransform), typeof(Image));
        _root.transform.SetParent(parent, false);
        _root.GetComponent<Image>().color = new Color(0.93f, 0.94f, 0.96f, 1f);
        RectTransform rootRect = _root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.1f, 0.1f);
        rootRect.anchorMax = new Vector2(0.9f, 0.9f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Text title = CreateText("TextItemTitle", _root.transform, 35, TextAnchor.MiddleCenter);
        title.text = "アイテム";
        title.color = Ink;
        PlaceTop(title.rectTransform, 16f, 0f, 360f, 56f);

        Button close = CreateButton("ButtonItemClose", _root.transform, "閉じる", new Color(0.75f, 0.28f, 0.24f, 1f));
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-16f, -12f);
        closeRect.sizeDelta = new Vector2(140f, 64f);
        close.onClick.AddListener(Close);

        _content = CreateScroll("ScrollItemList", _root.transform, 88f, 16f);
        BuildUsePage();
        _root.SetActive(false);
    }

    public void Open()
    {
        if (_root == null || _game == null)
            return;
        _root.SetActive(true);
        Transform overlay = _root.transform.parent;
        if (overlay != null)
            overlay.SetAsLastSibling();
        _root.transform.SetAsLastSibling();
        ShowList();
    }

    public void Close()
    {
        if (_usePage != null)
            _usePage.SetActive(false);
        if (_root != null)
            _root.SetActive(false);
    }

    public void Refresh()
    {
        if (!IsOpen || _game == null)
            return;
        if (_usePage != null && _usePage.activeSelf)
            FillUse();
        else
            Fill();
    }

    void ShowList()
    {
        if (_usePage != null)
            _usePage.SetActive(false);
        Fill();
    }

    void Fill()
    {
        if (_content == null || _game == null)
            return;
        Clear(_content);
        _game.CollectOwnedItemIds(_ids);
        bool charmArmed = _game.IsEnmusubiArmed();
        bool any = false;
        if (charmArmed || _game.HasHeldItem(ItemIds.EnmusubiOmamori))
        {
            AddCharm(_game.GetItemCount(ItemIds.EnmusubiOmamori), charmArmed);
            any = true;
        }
        for (int i = 0; i < _ids.Count; i++)
        {
            int itemId = _ids[i];
            if (itemId == ItemIds.EnmusubiOmamori || itemId == ItemIds.StageShard)
                continue;
            int count = _game.GetItemCount(itemId);
            if (ItemIds.IsExpOrb(itemId))
                AddOrb(itemId, count);
            else
                AddPlain(itemId, count);
            any = true;
        }
        if (!any)
        {
            Text empty = CreateText("TextItemEmpty", _content, 28, TextAnchor.MiddleCenter);
            empty.text = "所持しているアイテムはありません";
            empty.color = SubInk;
            LayoutElement element = empty.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 120f;
            element.preferredHeight = 120f;
        }
    }

    void AddCharm(int count, bool armed)
    {
        Image plate = CreateImage("ItemEnmusubi", _content, new Color(1f, 0.96f, 0.96f, 1f));
        LayoutElement element = plate.gameObject.AddComponent<LayoutElement>();
        element.minHeight = 176f;
        element.preferredHeight = 176f;

        Image icon = CreateImage("Icon", plate.transform, Color.white);
        icon.preserveAspect = true;
        Sprite sprite = ItemImages.Enmusubi();
        if (sprite != null)
            icon.sprite = sprite;
        else
            icon.color = new Color(0.75f, 0.28f, 0.28f, 1f);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(16f, 0f);
        iconRect.sizeDelta = new Vector2(120f, 120f);

        Text name = CreateText("Name", plate.transform, 30, TextAnchor.MiddleLeft);
        name.text = ItemIds.DisplayName(ItemIds.EnmusubiOmamori);
        name.color = Ink;
        RectTransform nameRect = name.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.offsetMin = new Vector2(152f, -64f);
        nameRect.offsetMax = new Vector2(-156f, -20f);

        Text detail = CreateText("Detail", plate.transform, 22, TextAnchor.UpperLeft);
        detail.text = "所持 " + UiNumber.Format(count) + "\n次に倒した未所持の敵が必ず仲間になる。1回で終わる。";
        detail.color = SubInk;
        RectTransform detailRect = detail.rectTransform;
        detailRect.anchorMin = new Vector2(0f, 0f);
        detailRect.anchorMax = new Vector2(1f, 1f);
        detailRect.offsetMin = new Vector2(152f, 16f);
        detailRect.offsetMax = new Vector2(-156f, -62f);

        bool canUse = !armed && count > 0;
        Button use = CreateButton("ButtonUseEnmusubi", plate.transform, armed ? "効果中" : "使う",
            canUse ? new Color(0.28f, 0.55f, 0.34f, 1f) : new Color(0.45f, 0.48f, 0.52f, 1f));
        RectTransform useRect = use.GetComponent<RectTransform>();
        useRect.anchorMin = new Vector2(1f, 0.5f);
        useRect.anchorMax = new Vector2(1f, 0.5f);
        useRect.pivot = new Vector2(1f, 0.5f);
        useRect.anchoredPosition = new Vector2(-16f, 0f);
        useRect.sizeDelta = new Vector2(124f, 72f);
        use.interactable = canUse;
        if (canUse)
            use.onClick.AddListener(UseCharm);
    }

    void AddOrb(int itemId, int count)
    {
        Image plate = CreateImage("Item" + itemId.ToString(), _content, new Color(0.94f, 0.97f, 1f, 1f));
        LayoutElement element = plate.gameObject.AddComponent<LayoutElement>();
        element.minHeight = 132f;
        element.preferredHeight = 132f;
        Button button = plate.gameObject.AddComponent<Button>();
        button.targetGraphic = plate;
        int id = itemId;
        button.onClick.AddListener(() => OpenOrb(id));

        float textLeft = PlaceListIcon(plate.transform, itemId, 96f);
        Text name = CreateText("Name", plate.transform, 28, TextAnchor.MiddleLeft);
        name.text = ItemIds.DisplayName(itemId);
        name.color = Ink;
        RectTransform nameRect = name.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(textLeft, 0f);
        nameRect.offsetMax = new Vector2(-140f, -8f);

        long each = ItemIds.ExpPerOrb(itemId);
        Text detail = CreateText("Detail", plate.transform, 22, TextAnchor.MiddleLeft);
        detail.text = "所持 " + UiNumber.Format(count) + "\n一辺" + itemId.ToString() + "のキャラに使える。1個で経験値 " + UiNumber.Format(each);
        detail.color = SubInk;
        RectTransform detailRect = detail.rectTransform;
        detailRect.anchorMin = new Vector2(0f, 0f);
        detailRect.anchorMax = new Vector2(1f, 0.5f);
        detailRect.offsetMin = new Vector2(textLeft, 8f);
        detailRect.offsetMax = new Vector2(-140f, 0f);

        Text choose = CreateText("Choose", plate.transform, 26, TextAnchor.MiddleCenter);
        choose.text = "選ぶ";
        choose.color = new Color(0.18f, 0.42f, 0.28f, 1f);
        RectTransform chooseRect = choose.rectTransform;
        chooseRect.anchorMin = new Vector2(1f, 0.5f);
        chooseRect.anchorMax = new Vector2(1f, 0.5f);
        chooseRect.pivot = new Vector2(1f, 0.5f);
        chooseRect.anchoredPosition = new Vector2(-16f, 0f);
        chooseRect.sizeDelta = new Vector2(110f, 64f);
    }

    void AddPlain(int itemId, int count)
    {
        Image plate = CreateImage("Item" + itemId.ToString(), _content, Color.white);
        LayoutElement element = plate.gameObject.AddComponent<LayoutElement>();
        element.minHeight = 96f;
        element.preferredHeight = 96f;

        float textLeft = PlaceListIcon(plate.transform, itemId, 72f);
        Text name = CreateText("Name", plate.transform, 28, TextAnchor.MiddleLeft);
        name.text = ItemIds.DisplayName(itemId);
        name.color = Ink;
        RectTransform nameRect = name.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(textLeft, 0f);
        nameRect.offsetMax = new Vector2(-20f, -8f);

        Text detail = CreateText("Detail", plate.transform, 22, TextAnchor.MiddleLeft);
        detail.text = "所持 " + UiNumber.Format(count);
        detail.color = SubInk;
        RectTransform detailRect = detail.rectTransform;
        detailRect.anchorMin = new Vector2(0f, 0f);
        detailRect.anchorMax = new Vector2(1f, 0.5f);
        detailRect.offsetMin = new Vector2(textLeft, 8f);
        detailRect.offsetMax = new Vector2(-20f, 0f);
    }

    static float PlaceListIcon(Transform parent, int itemId, float size)
    {
        Sprite sprite = ItemImages.SpriteOf(itemId);
        if (sprite == null || parent == null)
            return 20f;
        Image icon = CreateImage("Icon", parent, Color.white);
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        RectTransform rect = icon.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(16f, 0f);
        rect.sizeDelta = new Vector2(size, size);
        return 16f + size + 12f;
    }

    void UseCharm()
    {
        if (_game == null || !_game.TryUseEnmusubi())
            return;
        Fill();
    }

    void BuildUsePage()
    {
        _usePage = new GameObject("PanelExpOrb", typeof(RectTransform), typeof(Image));
        _usePage.transform.SetParent(_root.transform, false);
        _usePage.GetComponent<Image>().color = new Color(0.93f, 0.94f, 0.96f, 1f);
        Stretch(_usePage.GetComponent<RectTransform>());

        _useTitle = CreateText("TextOrbTitle", _usePage.transform, 30, TextAnchor.MiddleCenter);
        _useTitle.color = Ink;
        PlaceTop(_useTitle.rectTransform, 12f, 0f, 420f, 52f);

        Button back = CreateButton("ButtonOrbBack", _usePage.transform, "戻る", new Color(0.45f, 0.48f, 0.52f, 1f));
        RectTransform backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 1f);
        backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition = new Vector2(12f, -12f);
        backRect.sizeDelta = new Vector2(120f, 56f);
        back.onClick.AddListener(ShowList);

        Button close = CreateButton("ButtonOrbClose", _usePage.transform, "閉じる", new Color(0.75f, 0.28f, 0.24f, 1f));
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-12f, -12f);
        closeRect.sizeDelta = new Vector2(120f, 56f);
        close.onClick.AddListener(Close);

        for (int i = 0; i < Quantities.Length; i++)
        {
            int count = Quantities[i];
            Button button = CreateButton("ButtonOrbCount" + count.ToString(), _usePage.transform, count.ToString() + "個", QuantityOff);
            PlaceSlice(button.GetComponent<RectTransform>(), i, Quantities.Length, 76f, 60f);
            int picked = count;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SelectQuantity(picked));
            _quantityButtons[i] = button;
            _quantityImages[i] = button.GetComponent<Image>();
        }

        _ownedLine = CreateText("TextOrbOwned", _usePage.transform, 22, TextAnchor.MiddleLeft);
        _ownedLine.color = SubInk;
        RectTransform ownedRect = _ownedLine.rectTransform;
        ownedRect.anchorMin = new Vector2(0f, 1f);
        ownedRect.anchorMax = new Vector2(1f, 1f);
        ownedRect.pivot = new Vector2(0.5f, 1f);
        ownedRect.anchoredPosition = new Vector2(0f, -144f);
        ownedRect.sizeDelta = new Vector2(-32f, 36f);

        _characterContent = CreateScroll("ScrollOrbCharacters", _usePage.transform, 188f, 408f);

        Image bottom = CreateImage("PanelOrbDetail", _usePage.transform, new Color(0.97f, 0.98f, 1f, 1f));
        RectTransform bottomRect = bottom.rectTransform;
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomRect.sizeDelta = new Vector2(0f, 400f);

        _portrait = CreateImage("Portrait", bottom.transform, new Color(0.9f, 0.91f, 0.93f, 1f));
        _portrait.preserveAspect = true;
        RectTransform portraitRect = _portrait.rectTransform;
        portraitRect.anchorMin = new Vector2(0f, 1f);
        portraitRect.anchorMax = new Vector2(0f, 1f);
        portraitRect.pivot = new Vector2(0f, 1f);
        portraitRect.anchoredPosition = new Vector2(16f, -12f);
        portraitRect.sizeDelta = new Vector2(96f, 96f);

        _detailName = CreateText("Name", bottom.transform, 26, TextAnchor.MiddleLeft);
        _detailName.color = Ink;
        PlaceIn(bottom.transform, _detailName.rectTransform, 120f, 12f, 28f, 40f);

        _detailLevel = CreateText("Level", bottom.transform, 24, TextAnchor.MiddleLeft);
        _detailLevel.color = Ink;
        _detailLevel.supportRichText = true;
        PlaceIn(bottom.transform, _detailLevel.rectTransform, 120f, 52f, 28f, 40f);

        _barNowLabel = CreateText("ExpNowLabel", bottom.transform, 20, TextAnchor.MiddleLeft);
        _barNowLabel.color = SubInk;
        PlaceIn(bottom.transform, _barNowLabel.rectTransform, 16f, 116f, 16f, 24f);
        _barNow = MakeBar(bottom.transform, "ExpNow", 144f, new Color(0.28f, 0.62f, 0.36f, 1f));

        _barAfterLabel = CreateText("ExpAfterLabel", bottom.transform, 20, TextAnchor.MiddleLeft);
        _barAfterLabel.color = SubInk;
        PlaceIn(bottom.transform, _barAfterLabel.rectTransform, 16f, 176f, 16f, 24f);
        _barAfter = MakeBar(bottom.transform, "ExpAfter", 204f, new Color(0.92f, 0.72f, 0.22f, 1f));

        _detailStats = CreateText("Stats", bottom.transform, 22, TextAnchor.UpperLeft);
        _detailStats.color = Ink;
        _detailStats.supportRichText = true;
        PlaceIn(bottom.transform, _detailStats.rectTransform, 16f, 236f, 16f, 92f);

        _useButton = CreateButton("ButtonUseOrb", bottom.transform, "使用", new Color(0.28f, 0.55f, 0.34f, 1f));
        _useButtonImage = _useButton.GetComponent<Image>();
        RectTransform useRect = _useButton.GetComponent<RectTransform>();
        useRect.anchorMin = new Vector2(0f, 0f);
        useRect.anchorMax = new Vector2(1f, 0f);
        useRect.pivot = new Vector2(0.5f, 0f);
        useRect.anchoredPosition = new Vector2(0f, 12f);
        useRect.sizeDelta = new Vector2(-32f, 64f);
        _useButton.transition = Selectable.Transition.None;
        _useButton.onClick.AddListener(UseOrbs);

        _usePage.SetActive(false);
    }

    void OpenOrb(int itemId)
    {
        if (_game == null || !ItemIds.IsExpOrb(itemId))
            return;
        _itemId = itemId;
        _quantity = 1;
        _characterId = 0;
        _usePage.SetActive(true);
        _usePage.transform.SetAsLastSibling();
        FillUse();
    }

    void FillUse()
    {
        if (_game == null || _usePage == null || !ItemIds.IsExpOrb(_itemId))
            return;
        int owned = _game.GetItemCount(_itemId);
        if (owned < 1)
            _quantity = 1;
        else if (_quantity > owned)
            _quantity = LargestQuantity(owned);
        _useTitle.text = ItemIds.DisplayName(_itemId);
        _ownedLine.text = "所持 " + UiNumber.Format(owned) + "    1個で経験値 " + UiNumber.Format(ItemIds.ExpPerOrb(_itemId));
        PaintQuantities(owned);
        _game.CollectOwnedCharactersOfSize(_itemId, _characters);
        if (!ContainsCharacter(_characterId))
            _characterId = _characters.Count > 0 ? _characters[0] : 0;
        FillCharacters();
        PaintPreview();
    }

    void FillCharacters()
    {
        Clear(_characterContent);
        _cells.Clear();
        if (_characters.Count == 0)
        {
            Text empty = CreateText("TextOrbEmpty", _characterContent, 24, TextAnchor.MiddleCenter);
            empty.text = "一辺" + _itemId.ToString() + "の所持キャラはいません";
            empty.color = SubInk;
            LayoutElement element = empty.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 120f;
            element.preferredHeight = 120f;
            return;
        }
        const int columns = 3;
        for (int i = 0; i < _characters.Count; i += columns)
        {
            Image row = CreateImage("Row" + i.ToString(), _characterContent, new Color(1f, 1f, 1f, 0f));
            LayoutElement rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.minHeight = 186f;
            rowElement.preferredHeight = 186f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            row.raycastTarget = false;
            int last = i + columns;
            if (last > _characters.Count)
                last = _characters.Count;
            for (int c = i; c < last; c++)
                AddCharacterCell(row.transform, _characters[c]);
            for (int c = last; c < i + columns; c++)
            {
                Image spacer = CreateImage("Pad", row.transform, new Color(1f, 1f, 1f, 0f));
                spacer.raycastTarget = false;
            }
        }
    }

    void AddCharacterCell(Transform parent, uint characterId)
    {
        CharacterClass character = _game.GetCharacter(characterId);
        Image plate = CreateImage("Character" + characterId.ToString(), parent, characterId == _characterId ? SelectedCell : Color.white);
        Button button = plate.gameObject.AddComponent<Button>();
        button.targetGraphic = plate;
        button.transition = Selectable.Transition.None;
        uint id = characterId;
        button.onClick.AddListener(() => SelectCharacter(id));

        Image icon = CreateImage("Icon", plate.transform, new Color(0.9f, 0.91f, 0.93f, 1f));
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Sprite sprite = SpriteOf(character);
        if (sprite != null)
            icon.sprite = sprite;
        PlaceIn(plate.transform, icon.rectTransform, 8f, 6f, 8f, 92f);

        Text name = CreateText("Name", plate.transform, 18, TextAnchor.MiddleCenter);
        name.text = character != null && !string.IsNullOrEmpty(character.Name) ? character.Name : "キャラ" + characterId.ToString();
        name.color = Ink;
        PlaceIn(plate.transform, name.rectTransform, 4f, 100f, 4f, 40f);

        Text level = CreateText("Level", plate.transform, 16, TextAnchor.MiddleCenter);
        level.color = SubInk;
        PlaceIn(plate.transform, level.rectTransform, 4f, 140f, 4f, 20f);

        Image track = CreateImage("ExpTrack", plate.transform, new Color(0.78f, 0.8f, 0.84f, 1f));
        track.raycastTarget = false;
        PlaceIn(plate.transform, track.rectTransform, 12f, 162f, 12f, 12f);
        Image fill = CreateImage("ExpFill", track.transform, new Color(0.28f, 0.62f, 0.36f, 1f));
        fill.raycastTarget = false;

        OrbCell cell = new OrbCell();
        cell.Plate = plate;
        cell.Level = level;
        cell.ExpFill = fill;
        PaintCellExp(cell, character);
        _cells[characterId] = cell;
    }

    void PaintCellExp(OrbCell cell, CharacterClass character)
    {
        if (cell == null)
            return;
        long level = 1;
        long exp = 0;
        long expMax = 1;
        if (character != null)
        {
            if (character.Level >= 1)
                level = (long)character.Level;
            exp = (long)character.Exp;
            expMax = (long)character.ExpMax;
            if (expMax < 1)
                expMax = 1;
        }
        if (cell.Level != null)
            cell.Level.text = "Lv " + UiNumber.Format(level);
        SetBar(cell.ExpFill, exp, expMax);
    }

    void SelectQuantity(int count)
    {
        if (_game == null || _game.GetItemCount(_itemId) < count)
            return;
        _quantity = count;
        PaintQuantities(_game.GetItemCount(_itemId));
        PaintPreview();
    }

    void SelectCharacter(uint characterId)
    {
        _characterId = characterId;
        foreach (KeyValuePair<uint, OrbCell> pair in _cells)
        {
            if (pair.Value != null && pair.Value.Plate != null)
                pair.Value.Plate.color = pair.Key == characterId ? SelectedCell : Color.white;
        }
        PaintPreview();
    }

    void PaintQuantities(int owned)
    {
        for (int i = 0; i < Quantities.Length; i++)
        {
            bool affordable = owned >= Quantities[i];
            bool selected = Quantities[i] == _quantity;
            if (_quantityButtons[i] != null)
                _quantityButtons[i].interactable = affordable;
            if (_quantityImages[i] != null)
                _quantityImages[i].color = !affordable ? QuantityLocked : (selected ? QuantityOn : QuantityOff);
        }
    }

    void PaintPreview()
    {
        CharacterClass character = _characterId == 0 ? null : _game.GetCharacter(_characterId);
        Sprite sprite = SpriteOf(character);
        _portrait.sprite = sprite;
        _portrait.color = sprite != null ? Color.white : new Color(0.9f, 0.91f, 0.93f, 1f);
        if (character == null)
        {
            _detailName.text = "キャラがいません";
            _detailLevel.text = "同じ大きさの所持キャラに使えます";
            _detailStats.text = "";
            _barNowLabel.text = "";
            _barAfterLabel.text = "";
            SetBar(_barNow, 0, 1);
            SetBar(_barAfter, 0, 1);
            SetUseEnabled(false);
            return;
        }
        _detailName.text = string.IsNullOrEmpty(character.Name) ? "キャラ" + character.ID.ToString() : character.Name;
        ExpOrbForecast forecast = _game.PreviewExpOrb(character.ID, _itemId, _quantity);
        if (forecast.Ok && !forecast.Unchanged)
        {
            _detailLevel.text = LevelLine(forecast);
            _barNowLabel.text = "今  " + UiNumber.Format(forecast.ExpBefore) + " / " + UiNumber.Format(forecast.ExpMaxBefore);
            _barAfterLabel.text = "使うと  " + UiNumber.Format(forecast.ExpAfter) + " / " + UiNumber.Format(forecast.ExpMaxAfter);
            SetBar(_barNow, forecast.ExpBefore, forecast.ExpMaxBefore);
            SetBar(_barAfter, forecast.ExpAfter, forecast.ExpMaxAfter);
            _detailStats.text = StatBlock(forecast);
            SetUseEnabled(true);
            return;
        }
        PaintCurrent(character, forecast.Ok ? "これ以上は経験値が増えません" : "");
        SetUseEnabled(false);
    }

    void PaintCurrent(CharacterClass character, string note)
    {
        long level = character.Level < 1 ? 1 : (long)character.Level;
        long exp = (long)character.Exp;
        long expMax = (long)character.ExpMax;
        if (expMax < 1)
            expMax = 1;
        _detailLevel.text = "Lv " + UiNumber.Format(level);
        _barNowLabel.text = "今  " + UiNumber.Format(exp) + " / " + UiNumber.Format(expMax);
        _barAfterLabel.text = note;
        SetBar(_barNow, exp, expMax);
        SetBar(_barAfter, 0, 1);
        _detailStats.text = "HP " + UiNumber.Format((long)character.Stats[0].HPMax) + "\n"
            + "ATK " + UiNumber.Format((long)character.Stats[0].ATK) + "\n"
            + "DEF " + UiNumber.Format((long)character.Stats[0].DEF) + "\n"
            + "SPD " + UiNumber.Format((int)character.Stats[0].SPD);
    }

    void UseOrbs()
    {
        if (_game == null)
            return;
        ExpOrbForecast forecast;
        if (!_game.TryUseExpOrbs(_characterId, _itemId, _quantity, out forecast))
        {
            PaintPreview();
            return;
        }
        OrbCell used;
        if (_cells.TryGetValue(_characterId, out used))
            PaintCellExp(used, _game.GetCharacter(_characterId));
        int owned = _game.GetItemCount(_itemId);
        if (owned > 0 && _quantity > owned)
            _quantity = LargestQuantity(owned);
        _ownedLine.text = "所持 " + UiNumber.Format(owned) + "    1個で経験値 " + UiNumber.Format(ItemIds.ExpPerOrb(_itemId));
        PaintQuantities(owned);
        PaintPreview();
    }

    void SetUseEnabled(bool enabled)
    {
        _useButton.interactable = enabled;
        if (_useButtonImage != null)
            _useButtonImage.color = enabled ? new Color(0.28f, 0.55f, 0.34f, 1f) : QuantityLocked;
    }

    static string LevelLine(ExpOrbForecast forecast)
    {
        if (forecast.Levels > 0)
            return "Lv " + UiNumber.Format(forecast.LevelBefore) + " → " + UiNumber.Format(forecast.LevelAfter) + " <color=#1b7a32>(+" + forecast.Levels.ToString() + ")</color>";
        return "Lv " + UiNumber.Format(forecast.LevelBefore) + "（レベルは変わらない）";
    }

    static string StatBlock(ExpOrbForecast forecast)
    {
        return StatLine("HP", forecast.HpBefore, forecast.HpAfter) + "\n"
            + StatLine("ATK", forecast.AtkBefore, forecast.AtkAfter) + "\n"
            + StatLine("DEF", forecast.DefBefore, forecast.DefAfter) + "\n"
            + StatLine("SPD", forecast.SpdBefore, forecast.SpdAfter);
    }

    static string StatLine(string label, long before, long after)
    {
        long delta = after - before;
        string sign = delta > 0 ? "+" : "";
        return label + " " + UiNumber.Format(before) + " → " + UiNumber.Format(after) + " <color=#1b7a32>(" + sign + UiNumber.Format(delta) + ")</color>";
    }

    bool ContainsCharacter(uint characterId)
    {
        for (int i = 0; i < _characters.Count; i++)
        {
            if (_characters[i] == characterId)
                return true;
        }
        return false;
    }

    static int LargestQuantity(int owned)
    {
        int picked = 1;
        for (int i = 0; i < Quantities.Length; i++)
        {
            if (Quantities[i] <= owned)
                picked = Quantities[i];
        }
        return picked;
    }

    Sprite SpriteOf(CharacterClass character)
    {
        if (character == null || character.ImageTexture2D == null || character.Size < 1)
            return null;
        Sprite cached;
        if (_sprites.TryGetValue(character.ID, out cached) && cached != null)
            return cached;
        Texture2D tex = character.ImageTexture2D;
        tex.filterMode = FilterMode.Point;
        int size = character.Size;
        if (size > tex.width)
            size = tex.width;
        if (size > tex.height)
            size = tex.height;
        if (size < 1)
            return null;
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        _sprites[character.ID] = sprite;
        return sprite;
    }

    Image MakeBar(Transform parent, string name, float top, Color fillColor)
    {
        Image track = CreateImage(name, parent, new Color(0.78f, 0.8f, 0.84f, 1f));
        PlaceIn(parent, track.rectTransform, 16f, top, 16f, 18f);
        Image fill = CreateImage("Fill", track.transform, fillColor);
        SetBar(fill, 0, 1);
        return fill;
    }

    static void SetBar(Image fill, long value, long max)
    {
        if (fill == null)
            return;
        float rate = 0f;
        if (max > 0 && value > 0)
            rate = (float)((double)value / max);
        if (rate < 0f)
            rate = 0f;
        if (rate > 1f)
            rate = 1f;
        RectTransform rect = fill.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(rate, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 8, 12);
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
        Text text = CreateText("Text", obj.transform, 26, TextAnchor.MiddleCenter);
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
        if (content == null)
            return;
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

    static void PlaceSlice(RectTransform rect, int index, int count, float top, float height)
    {
        float width = 1f / count;
        float x0 = width * index;
        rect.anchorMin = new Vector2(x0 + 0.015f, 1f);
        rect.anchorMax = new Vector2(x0 + width - 0.015f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(0f, height);
    }

    static void PlaceIn(Transform parent, RectTransform rect, float left, float top, float right, float height)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
