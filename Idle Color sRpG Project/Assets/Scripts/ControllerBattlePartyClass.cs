using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//バトル編成パネルのコントロール
public class ControllerBattlePartyClass : MonoBehaviour
{
    //マスのボタン名は「ButtonBattlePartyCell」+ x + y (例: ButtonBattlePartyCell13 は x=1, y=3)
    public const string CELL_BUTTON_PREFIX = "ButtonBattlePartyCell";
    public const string PERSON_BUTTON_PREFIX = "ButtonBattlePartyPerson";

    CharacterClass[] CharactersAll;
    ControllerProduction ControllerProduction;

    //表示中の編成セット(1～BATTLE_PARTY_SET_NUM)
    int CurrentSetIndex = 1;

    //[0]がセット1のタブ
    [SerializeField] Button[] ButtonBattlePartySets = new Button[Constants.BATTLE_PARTY_SET_NUM];
    [SerializeField] Text TextBattlePartyTotal;
    [SerializeField] Button ButtonClearBattleParty;

    Button[,] ButtonCells = new Button[Constants.BATTLE_FORMATION_SIZE + 1, Constants.BATTLE_FORMATION_SIZE + 1];
    Button[] ButtonPeople = new Button[Constants.BATTLE_PERSON_SLOT_NUM + 1];
    bool PersonColumnShifted;

    Color ColorSetTabNormal = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    Color ColorSetTabSelected = new Color(1.0f, 0.85f, 0.4f, 1.0f);
    Color ColorCellEmpty = new Color(0.2f, 0.2f, 0.2f, 1.0f);

    public void Initialize(ref CharacterClass[] argCharactersAll, ControllerProduction argControllerProduction)
    {
        CharactersAll = argCharactersAll;
        ControllerProduction = argControllerProduction;

        for (int i = 0; i < ButtonBattlePartySets.Length; i++)
        {
            int setIndex = i + 1;//匿名メソッドの外部変数のキャプチャの関係で、別の変数に代入
            ButtonBattlePartySets[i].onClick.AddListener(() => PushButtonSelectBattlePartySet(setIndex));
        }

        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                string cellButtonName = CELL_BUTTON_PREFIX + x.ToString() + y.ToString();
                GameObject cellObject = GameObject.Find(cellButtonName);
                if (cellObject == null)
                {
                    Debug.LogWarning(cellButtonName + " が見つかりません");
                    continue;
                }
                ButtonCells[x, y] = cellObject.GetComponent<Button>();
                ButtonCells[x, y].onClick.AddListener(() => ControllerProduction.PushButtonSelectCharacter(cellButtonName));
            }
        }

        CreatePersonSlots();

        ButtonClearBattleParty.onClick.AddListener(PushButtonClearBattleParty);
    }

    public int GetCurrentSetIndex()
    {
        return CurrentSetIndex;
    }

    public static bool IsCellButton(Button argButton)
    {
        return argButton != null && argButton.name.StartsWith(CELL_BUTTON_PREFIX);
    }

    public static bool IsPersonButton(Button argButton)
    {
        return argButton != null && argButton.name.StartsWith(PERSON_BUTTON_PREFIX);
    }

    public static int PersonSlot(Button argButton)
    {
        if (!IsPersonButton(argButton) || argButton.name.Length < 1)
            return 0;
        return argButton.name[argButton.name.Length - 1] - '0';
    }

    public static void GetCellPosition(Button argButton, out int x, out int y)
    {
        string position = argButton.name.Substring(argButton.name.Length - 2, 2);
        x = position[0] - '0';
        y = position[1] - '0';
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //ボタン

    //セットのタブが押されたら
    public void PushButtonSelectBattlePartySet(int argSetIndex)
    {
        CurrentSetIndex = argSetIndex;
        Refresh();
    }

    //クリアボタンが押されたら
    public void PushButtonClearBattleParty()
    {
        ControllerProduction.ClearBattlePartySet(CurrentSetIndex);
        Refresh();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //表示

    //パネルを表示するときに呼ぶ。出撃中のセットがあればそのセットを開く
    public void Open()
    {
        int activeSet = ControllerProduction.GetActiveBattlePartySet();
        if (activeSet != 0)
            CurrentSetIndex = activeSet;
        Refresh();
    }

    public void Refresh()
    {
        int activeSet = ControllerProduction.GetActiveBattlePartySet();
        //出撃中のセットは SetBattlePartyCharacter で弾かれるので、マスも押せなくする
        bool isActiveSet = activeSet == CurrentSetIndex;

        for (int i = 0; i < ButtonBattlePartySets.Length; i++)
        {
            int setIndex = i + 1;
            ButtonBattlePartySets[i].image.color = (setIndex == CurrentSetIndex) ? ColorSetTabSelected : ColorSetTabNormal;
            ButtonBattlePartySets[i].GetComponentInChildren<Text>().text =
                "セット" + setIndex.ToString() + (setIndex == activeSet ? "\n出撃中" : "");
        }

        int memberNum = 0;
        ulong totalHP = 0;
        ulong totalATK = 0;
        ulong totalDEF = 0;

        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                if (ButtonCells[x, y] == null)
                    continue;

                uint characterId = ControllerProduction.GetBattlePartyCharacter(CurrentSetIndex, x, y);
                Button cellButton = ButtonCells[x, y];
                Text cellText = cellButton.GetComponentInChildren<Text>();
                cellButton.interactable = !isActiveSet;

                if (characterId == 0)
                {
                    cellButton.image.sprite = null;
                    cellButton.image.color = ColorCellEmpty;
                    cellText.text = "+";
                    continue;
                }

                cellButton.image.sprite = CreateCharacterSprite(characterId);
                cellButton.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                cellText.text = "";

                CharacterClass member = ImportedCharacters.FindAny(CharactersAll, characterId);
                if (member == null || member.Stats == null || member.Stats[0] == null)
                    continue;
                memberNum++;
                totalHP += (ulong)ControllerProduction.PreviewStat(member, CurrentSetIndex, (long)member.Stats[0].HPMax, BattleCharacterFactory.ToAttribute(member.CharacterType));
                totalATK += (ulong)ControllerProduction.PreviewStat(member, CurrentSetIndex, (long)member.Stats[0].ATK, BattleCharacterFactory.ToAttribute(member.CharacterType));
                totalDEF += (ulong)ControllerProduction.PreviewStat(member, CurrentSetIndex, (long)member.Stats[0].DEF, BattleCharacterFactory.ToAttribute(member.CharacterType));
            }
        }

        int personNum = RefreshPeople(isActiveSet);

        TextBattlePartyTotal.text = memberNum.ToString() + "/" + (Constants.BATTLE_FORMATION_SIZE * Constants.BATTLE_FORMATION_SIZE).ToString()
                                    + "   人 " + personNum.ToString() + "/" + Constants.BATTLE_PERSON_SLOT_NUM.ToString()
                                    + "   HP " + UiNumber.Format(totalHP) + "   ATK " + UiNumber.Format(totalATK) + "   DEF " + UiNumber.Format(totalDEF);

        ButtonClearBattleParty.interactable = !isActiveSet && (memberNum > 0 || personNum > 0);
        PlacePersonSlots();
    }

    Sprite CreateCharacterSprite(uint argCharacterId)
    {
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, argCharacterId);
        Texture2D tex = character != null ? character.ImageTexture2D : null;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        return Sprite.Create(tex, new UnityEngine.Rect(0, 0, character.Size, character.Size), new Vector2(0.5f, 0.5f));
    }

    void CreatePersonSlots()
    {
        Button sample = ButtonCells[1, 1];
        if (sample == null)
        {
            Debug.LogWarning("編成の左列がないので、人の枠を置けません");
            return;
        }
        RectTransform panel = sample.transform.parent.parent as RectTransform;
        for (int slot = 1; slot <= Constants.BATTLE_PERSON_SLOT_NUM; slot++)
        {
            string name = PERSON_BUTTON_PREFIX + slot.ToString();
            Transform existing = panel != null ? panel.Find(name) : null;
            if (existing == null)
                existing = sample.transform.parent.Find(name);
            GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            if (panel != null)
                buttonObject.transform.SetParent(panel, false);
            Button button = buttonObject.GetComponent<Button>();
            Image image = buttonObject.GetComponent<Image>();
            button.targetGraphic = image;
            image.color = ColorCellEmpty;
            image.raycastTarget = true;
            if (buttonObject.GetComponentInChildren<Text>() == null)
            {
                GameObject textObject = new GameObject("Text", typeof(RectTransform));
                textObject.transform.SetParent(buttonObject.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                Text text = textObject.AddComponent<Text>();
                Text sampleText = sample.GetComponentInChildren<Text>();
                text.font = sampleText != null && sampleText.font != null ? sampleText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 22;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 10;
                text.resizeTextMaxSize = 28;
                text.text = "人";
                text.raycastTarget = false;
            }
            int captured = slot;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ControllerProduction.PushButtonSelectCharacter(PERSON_BUTTON_PREFIX + captured.ToString()));
            ButtonPeople[slot] = button;
        }
        StartCoroutine(PlacePersonSlotsAfterLayout());
    }

    IEnumerator PlacePersonSlotsAfterLayout()
    {
        yield return null;
        PlacePersonSlots();
    }

    void PlacePersonSlots()
    {
        Button sample = ButtonCells[1, 1];
        if (sample == null || ButtonPeople[1] == null)
            return;
        RectTransform formation = sample.transform.parent as RectTransform;
        RectTransform panel = formation.parent as RectTransform;
        if (panel == null)
            return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(formation);
        if (panel.rect.width < 8f || sample.GetComponent<RectTransform>().rect.width < 8f)
            return;

        GridLayoutGroup grid = formation.GetComponent<GridLayoutGroup>();
        float cellSize = grid != null ? grid.cellSize.y : sample.GetComponent<RectTransform>().rect.height;
        if (cellSize < 8f)
            cellSize = 280f;
        float gap = 16f;
        Vector2 firstLeft = LeftCenterInPanel(panel, sample.GetComponent<RectTransform>());
        float panelHalf = panel.rect.width * 0.5f;
        float panelLeft = -panelHalf + 12f;
        float panelRight = panelHalf - 12f;
        float formationRight = formation.anchoredPosition.x + formation.rect.width * 0.5f;
        float roomRight = panelRight - formationRight;
        float roomLeft = firstLeft.x - gap - panelLeft;
        if (roomRight < 0f)
            roomRight = 0f;
        if (roomLeft < 0f)
            roomLeft = 0f;
        float slotSize = cellSize;
        if (slotSize > roomLeft + roomRight - gap)
            slotSize = roomLeft + roomRight - gap;
        if (slotSize < 96f)
            slotSize = 96f;

        if (!PersonColumnShifted)
        {
            float shift = slotSize + gap - roomLeft;
            if (shift < 0f)
                shift = 0f;
            if (shift > roomRight)
                shift = roomRight;
            if (shift > 0.5f)
            {
                formation.anchoredPosition += new Vector2(shift, 0f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(formation);
            }
            PersonColumnShifted = true;
        }

        for (int slot = 1; slot <= Constants.BATTLE_PERSON_SLOT_NUM; slot++)
        {
            Button button = ButtonPeople[slot];
            Button cellButton = ButtonCells[1, slot];
            if (button == null || cellButton == null)
                continue;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.SetParent(panel, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(slotSize, slotSize);
            Vector2 leftCenter = LeftCenterInPanel(panel, cellButton.GetComponent<RectTransform>());
            rect.anchoredPosition = new Vector2(leftCenter.x - gap, leftCenter.y);
            rect.SetAsLastSibling();
        }
    }

    static Vector2 LeftCenterInPanel(RectTransform panel, RectTransform cell)
    {
        Vector3[] corners = new Vector3[4];
        cell.GetWorldCorners(corners);
        Vector3 leftCenter = (corners[0] + corners[1]) * 0.5f;
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        Camera camera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panel,
            RectTransformUtility.WorldToScreenPoint(camera, leftCenter),
            camera,
            out local);
        return local;
    }

    int RefreshPeople(bool isActiveSet)
    {
        int count = 0;
        for (int slot = 1; slot <= Constants.BATTLE_PERSON_SLOT_NUM; slot++)
        {
            Button button = ButtonPeople[slot];
            if (button == null)
                continue;
            button.interactable = !isActiveSet;
            uint personId = ControllerProduction.GetBattlePartyPerson(CurrentSetIndex, slot);
            PersonClass person = ControllerProduction.GetPerson((int)personId);
            if (person == null)
            {
                button.image.sprite = null;
                button.image.color = ColorCellEmpty;
                ControllerProduction.HidePersonPortrait(button);
                Text empty = button.GetComponentInChildren<Text>();
                if (empty != null)
                {
                    empty.text = "人";
                    empty.color = Color.white;
                }
                continue;
            }
            count++;
            ControllerProduction.PaintPerson(button, person);
        }
        return count;
    }
}
