using UnityEngine;
using UnityEngine.UI;

//バトルステージ選択パネルのコントロール
public class ControllerBattleStageClass : MonoBehaviour
{
    //マスの名前は「ImageBattleStageCell」+ x + y (例: ImageBattleStageCell13 は x=1, y=3)
    //編成パネルの ButtonBattlePartyCell とは別名。こちらは画像だけで、押しても編成できない
    public const string CELL_IMAGE_PREFIX = "ImageBattleStageCell";

    //ステージ番号は 1～BATTLE_STAGE_NUM。0 は未出撃
    static readonly string[] StageNames = { "", "草原", "森", "洞窟", "遺跡", "城", "ギャラリー" };
    static readonly ulong[] StageRecommendedHP = { 0, 100, 300, 800, 2000, 5000 };

    public static string GetStageName(int stageIndex)
    {
        if (stageIndex < 1 || stageIndex >= StageNames.Length)
            return "ステージ" + stageIndex.ToString();
        return StageNames[stageIndex];
    }

    CharacterClass[] CharactersAll;
    ControllerProduction ControllerProduction;

    int CurrentStageIndex = 1;
    int CurrentSetIndex = 1;

    //[0]がステージ1、セット1
    [SerializeField] Button[] ButtonBattleStages = new Button[Constants.BATTLE_STAGE_NUM];
    [SerializeField] Button[] ButtonBattleStageSets = new Button[Constants.BATTLE_PARTY_SET_NUM];
    [SerializeField] Text TextBattleStagePartyTotal;
    [SerializeField] Text TextBattleFloorRange;
    [SerializeField] Slider SliderBattleFloorFrom;
    [SerializeField] Slider SliderBattleFloorTo;
    [SerializeField] Button ButtonSortieBattleStage;
    [SerializeField] Button ButtonBattleRepeat;
    Button ButtonGalleryStage;

    const int RepeatUnlockStage = 1;
    const int RepeatUnlockFloor = 50;

    bool RepeatBattle;
    Color RepeatLabelColor = Color.black;
    bool RepeatLabelColorStored;
    bool FloorFromSliderMeasured;
    float FloorFromSliderFullWidth;
    float FloorFromSliderLeft;

    Image[,] CellImages = new Image[Constants.BATTLE_FORMATION_SIZE + 1, Constants.BATTLE_FORMATION_SIZE + 1];

    Color ColorButtonNormal = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    Color ColorButtonSelected = new Color(1.0f, 0.85f, 0.4f, 1.0f);
    Color ColorStageLocked = new Color(0.55f, 0.55f, 0.55f, 1.0f);
    Color ColorCellEmpty = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    Color ColorSortie = new Color(0.55f, 0.85f, 0.55f, 1.0f);
    Color ColorWithdraw = new Color(0.95f, 0.55f, 0.45f, 1.0f);

    public void Initialize(ref CharacterClass[] argCharactersAll, ControllerProduction argControllerProduction)
    {
        CharactersAll = argCharactersAll;
        ControllerProduction = argControllerProduction;

        for (int i = 0; i < ButtonBattleStages.Length; i++)
        {
            if (ButtonBattleStages[i] == null)
            {
                Debug.LogWarning("ButtonBattleStages の要素 " + i + " が割り当てられていません");
                continue;
            }
            int stageIndex = i + 1;
            ButtonBattleStages[i].onClick.AddListener(() => PushButtonSelectBattleStage(stageIndex));
        }

        for (int i = 0; i < ButtonBattleStageSets.Length; i++)
        {
            if (ButtonBattleStageSets[i] == null)
            {
                Debug.LogWarning("ButtonBattleStageSets の要素 " + i + " が割り当てられていません");
                continue;
            }
            int setIndex = i + 1;
            ButtonBattleStageSets[i].onClick.AddListener(() => PushButtonSelectBattleStageSet(setIndex));
        }

        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                string cellName = CELL_IMAGE_PREFIX + x.ToString() + y.ToString();
                GameObject cellObject = GameObject.Find(cellName);
                if (cellObject == null)
                {
                    Debug.LogWarning(cellName + " が見つかりません");
                    continue;
                }
                CellImages[x, y] = cellObject.GetComponent<Image>();
                CellImages[x, y].raycastTarget = false;
                CellImages[x, y].type = Image.Type.Simple;
                CellImages[x, y].preserveAspect = true;
            }
        }

        if (ButtonSortieBattleStage != null)
            ButtonSortieBattleStage.onClick.AddListener(PushButtonSortieOrWithdraw);

        if (ButtonBattleRepeat == null)
        {
            GameObject repeatObject = GameObject.Find("ButtonBattleRepeat");
            if (repeatObject != null)
                ButtonBattleRepeat = repeatObject.GetComponent<Button>();
        }
        if (ButtonBattleRepeat != null)
            ButtonBattleRepeat.onClick.AddListener(PushButtonBattleRepeat);
        else
            Debug.LogWarning("ButtonBattleRepeat が見つかりません");

        EnsureGalleryButton();

        SetupFloorSlider(SliderBattleFloorFrom, true);
        SetupFloorSlider(SliderBattleFloorTo, false);
    }

    void SetupFloorSlider(Slider slider, bool fromSlider)
    {
        if (slider == null)
        {
            Debug.LogWarning(fromSlider ? "SliderBattleFloorFrom が割り当てられていません" : "SliderBattleFloorTo が割り当てられていません");
            return;
        }

        slider.wholeNumbers = true;
        slider.minValue = 1;
        slider.maxValue = Constants.BATTLE_STAGE_FLOOR_MAX / Constants.BATTLE_STAGE_FLOOR_STEP;
        slider.onValueChanged.AddListener(_ => OnFloorSliderChanged(fromSlider));
        if (fromSlider)
            RememberFloorFromSliderSize();
    }

    void RememberFloorFromSliderSize()
    {
        if (FloorFromSliderMeasured || SliderBattleFloorFrom == null)
            return;
        RectTransform rect = SliderBattleFloorFrom.GetComponent<RectTransform>();
        float width = rect.rect.width;
        if (width <= 1f)
            width = rect.sizeDelta.x;
        FloorFromSliderFullWidth = width;
        FloorFromSliderLeft = rect.anchoredPosition.x - width * rect.pivot.x;
        FloorFromSliderMeasured = true;
    }

    void OnFloorSliderChanged(bool fromMoved)
    {
        if (SliderBattleFloorFrom != null && SliderBattleFloorTo != null)
        {
            int fromBand = Mathf.RoundToInt(SliderBattleFloorFrom.value);
            int toBand = Mathf.RoundToInt(SliderBattleFloorTo.value);
            if (fromBand > toBand)
            {
                if (fromMoved)
                    SliderBattleFloorTo.SetValueWithoutNotify(fromBand);
                else
                    SliderBattleFloorFrom.SetValueWithoutNotify(toBand);
            }
        }

        Refresh();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //ボタン

    public void PushButtonSelectBattleStage(int argStageIndex)
    {
        if (!CanSelectStage(argStageIndex))
            return;
        CurrentStageIndex = argStageIndex;
        Refresh();
    }

    public void PushButtonSelectBattleStageSet(int argSetIndex)
    {
        if (argSetIndex < 1 || argSetIndex > Constants.BATTLE_PARTY_SET_NUM)
            return;
        CurrentSetIndex = argSetIndex;
        Refresh();
    }

    //見ているステージが出撃中なら撤退する。別のステージなら、今のセットを撤退してから選んでいるセットを出撃させる
    public void PushButtonSortieOrWithdraw()
    {
        int activeSet = ControllerProduction.GetActiveBattlePartySet();
        int activeStage = ControllerProduction.GetActiveBattleStage();
        if (activeSet != 0 && (activeStage == 0 || activeStage == CurrentStageIndex))
        {
            if (!ControllerProduction.RequestBattleWithdraw())
            {
                ControllerProduction.LeaveBattle();
                ControllerProduction.SaveGame();
            }
        }
        else
        {
            GetSelectedFloorRange(out int floorFrom, out int floorTo);
            if (ControllerProduction.EnterBattleStage(CurrentStageIndex, CurrentSetIndex, floorFrom, floorTo))
            {
                if (!ControllerProduction.BeginAutoBattle())
                    ControllerProduction.LeaveBattle();
            }
        }

        Refresh();
    }

    public void PushButtonBattleRepeat()
    {
        if (!IsRepeatUnlocked())
            return;
        RepeatBattle = !RepeatBattle;
        Refresh();
    }

    public bool IsRepeatOn()
    {
        return RepeatBattle && IsRepeatUnlocked();
    }

    bool IsRepeatUnlocked()
    {
        return ControllerProduction != null
            && ControllerProduction.GetHighestClearedBattleFloor(RepeatUnlockStage) >= RepeatUnlockFloor;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //表示

    public void Open()
    {
        int activeStage = ControllerProduction.GetActiveBattleStage();
        if (activeStage != 0)
            CurrentStageIndex = activeStage;

        int activeSet = ControllerProduction.GetActiveBattlePartySet();
        if (activeSet != 0)
            CurrentSetIndex = activeSet;

        int activeFloorFrom = ControllerProduction.GetActiveBattleFloorFrom();
        int activeFloorTo = ControllerProduction.GetActiveBattleFloorTo();
        int windowStart = ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex);
        if (activeStage == CurrentStageIndex && ControllerProduction.IsSortieFloorRange(activeStage, activeFloorFrom, activeFloorTo))
            SetFloorSliders(activeFloorFrom, activeFloorTo);
        else
            SetFloorSliders(windowStart, windowStart + Constants.BATTLE_STAGE_FLOOR_STEP - 1);

        Refresh();
    }

    public void Refresh()
    {
        int activeStage = ControllerProduction.GetActiveBattleStage();
        int activeSet = ControllerProduction.GetActiveBattlePartySet();
        int clearedStage = ControllerProduction.GetClearedBattleStage();

        if (!CanSelectStage(CurrentStageIndex))
        {
            int openedStage = clearedStage + 1;
            if (openedStage > Constants.BATTLE_STAGE_NUM)
                openedStage = Constants.BATTLE_STAGE_NUM;
            CurrentStageIndex = openedStage;
        }

        ApplyFloorFromLimit();

        for (int i = 0; i < ButtonBattleStages.Length; i++)
        {
            if (ButtonBattleStages[i] == null)
                continue;

            int stageIndex = i + 1;
            if (stageIndex > Constants.BATTLE_STAGE_NUM)
                break;

            bool unlocked = ControllerProduction.IsBattleStageUnlocked(stageIndex);
            ButtonBattleStages[i].interactable = unlocked || stageIndex == activeStage;
            Text label = ButtonBattleStages[i].GetComponentInChildren<Text>();

            if (!unlocked && stageIndex != activeStage)
            {
                ButtonBattleStages[i].image.color = ColorStageLocked;
                if (label != null)
                    label.text = StageNames[stageIndex] + "\n未開放";
                continue;
            }

            ButtonBattleStages[i].image.color = (stageIndex == CurrentStageIndex) ? ColorButtonSelected : ColorButtonNormal;
            if (label == null)
                continue;

            label.text = StageNames[stageIndex] + "\n推奨HP " + StageRecommendedHP[stageIndex];
            if (stageIndex == activeStage)
                label.text += "\n出撃中";
            else if (stageIndex <= clearedStage)
                label.text += "\nクリア";
        }

        PaintGalleryButton(activeStage);

        for (int i = 0; i < ButtonBattleStageSets.Length; i++)
        {
            if (ButtonBattleStageSets[i] == null)
                continue;

            int setIndex = i + 1;
            ButtonBattleStageSets[i].image.color = (setIndex == CurrentSetIndex) ? ColorButtonSelected : ColorButtonNormal;
            Text label = ButtonBattleStageSets[i].GetComponentInChildren<Text>();
            if (label == null)
                continue;

            label.text = "セット" + setIndex.ToString() + (setIndex == activeSet ? "\n出撃中" : "");
        }

        int memberNum = 0;
        ulong totalHP = 0;
        ulong totalATK = 0;
        ulong totalDEF = 0;

        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                uint characterId = ControllerProduction.GetBattlePartyCharacter(CurrentSetIndex, x, y);
                if (CellImages[x, y] != null)
                {
                    if (characterId == 0)
                    {
                        CellImages[x, y].sprite = null;
                        CellImages[x, y].color = ColorCellEmpty;
                    }
                    else
                    {
                        CellImages[x, y].sprite = CreateCharacterSprite(characterId);
                        CellImages[x, y].color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                    }
                }

                if (characterId == 0)
                    continue;

                CharacterClass member = ImportedCharacters.FindAny(CharactersAll, characterId);
                if (member == null || member.Stats == null || member.Stats[0] == null)
                    continue;
                memberNum++;
                totalHP += member.Stats[0].HPMax;
                totalATK += member.Stats[0].ATK;
                totalDEF += member.Stats[0].DEF;
            }
        }

        if (TextBattleStagePartyTotal != null)
        {
            TextBattleStagePartyTotal.text = memberNum.ToString() + "/" + (Constants.BATTLE_FORMATION_SIZE * Constants.BATTLE_FORMATION_SIZE).ToString() + "人\n"
                + "HP " + totalHP + "\n"
                + "ATK " + totalATK + "\n"
                + "DEF " + totalDEF;
            TextBattleStagePartyTotal.horizontalOverflow = HorizontalWrapMode.Overflow;
            TextBattleStagePartyTotal.verticalOverflow = VerticalWrapMode.Overflow;
        }

        bool withdrawMode = activeSet != 0 && (activeStage == 0 || activeStage == CurrentStageIndex);
        if (ButtonSortieBattleStage != null)
        {
            ButtonSortieBattleStage.interactable = withdrawMode || (ControllerProduction.CanEnterStage(CurrentStageIndex) && memberNum > 0);
            ButtonSortieBattleStage.image.color = withdrawMode ? ColorWithdraw : ColorSortie;
            Text label = ButtonSortieBattleStage.GetComponentInChildren<Text>();
            if (label != null)
                label.text = withdrawMode ? "撤退" : "出撃";
        }

        if (ButtonBattleRepeat != null)
        {
            bool repeatOpen = IsRepeatUnlocked();
            if (!repeatOpen)
                RepeatBattle = false;
            ButtonBattleRepeat.interactable = repeatOpen;
            Text repeatLabel = ButtonBattleRepeat.GetComponentInChildren<Text>();
            if (repeatLabel != null && !RepeatLabelColorStored)
            {
                RepeatLabelColor = repeatLabel.color;
                RepeatLabelColorStored = true;
            }

            if (!repeatOpen)
            {
                ButtonBattleRepeat.image.color = ColorStageLocked;
                if (repeatLabel != null)
                {
                    repeatLabel.color = new Color(0.28f, 0.28f, 0.28f, 1f);
                    repeatLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    repeatLabel.verticalOverflow = VerticalWrapMode.Overflow;
                    repeatLabel.text = "繰り返し\n" + GetStageName(RepeatUnlockStage) + "の" + RepeatUnlockFloor.ToString() + "階をクリアすると開く";
                }
            }
            else
            {
                ButtonBattleRepeat.image.color = RepeatBattle ? ColorButtonSelected : ColorButtonNormal;
                if (repeatLabel != null)
                {
                    repeatLabel.color = RepeatLabelColor;
                    repeatLabel.text = RepeatBattle ? "繰り返し\nON" : "繰り返し\nOFF";
                }
            }
        }

        if (TextBattleFloorRange != null)
        {
            GetSelectedFloorRange(out int floorFrom, out int floorTo);
            TextBattleFloorRange.text = floorFrom.ToString() + "階 〜 " + floorTo.ToString() + "階";
            int maxFrom = ControllerProduction.GetMaxBattleFloorFrom(CurrentStageIndex);
            int windowStart = ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex);
            int lastBandStart = windowStart + Constants.BATTLE_STAGE_FLOOR_MAX - Constants.BATTLE_STAGE_FLOOR_STEP;
            if (maxFrom < lastBandStart)
            {
                int highest = ControllerProduction.GetHighestClearedBattleFloor(CurrentStageIndex);
                if (highest <= 0)
                    TextBattleFloorRange.text += "\n（開始は1階まで）";
                else
                    TextBattleFloorRange.text += "\n（" + highest.ToString() + "階クリア、開始は" + maxFrom.ToString() + "階まで）";
            }

            TextBattleFloorRange.horizontalOverflow = HorizontalWrapMode.Overflow;
            TextBattleFloorRange.verticalOverflow = VerticalWrapMode.Overflow;

            int activeFloorFrom = ControllerProduction.GetActiveBattleFloorFrom();
            int activeFloorTo = ControllerProduction.GetActiveBattleFloorTo();
            if (activeStage == CurrentStageIndex && activeFloorFrom > 0
                && (activeFloorFrom != floorFrom || activeFloorTo != floorTo))
            {
                TextBattleFloorRange.text += "\n出撃中 " + activeFloorFrom.ToString() + "階 〜 " + activeFloorTo.ToString() + "階";
            }
        }
    }

    void ApplyFloorFromLimit()
    {
        if (SliderBattleFloorFrom == null || ControllerProduction == null)
            return;

        int maxFrom = ControllerProduction.GetMaxBattleFloorFrom(CurrentStageIndex);
        int windowStart = ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex);
        int maxBand = (maxFrom - windowStart) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
        if (maxBand < 1)
            maxBand = 1;

        int fromBand = Mathf.RoundToInt(SliderBattleFloorFrom.value);
        if (fromBand > maxBand)
            fromBand = maxBand;
        SliderBattleFloorFrom.maxValue = maxBand;
        SliderBattleFloorFrom.SetValueWithoutNotify(fromBand);
        ResizeFloorFromSlider(maxBand);

        if (SliderBattleFloorTo == null)
            return;
        int toBand = Mathf.RoundToInt(SliderBattleFloorTo.value);
        fromBand = Mathf.RoundToInt(SliderBattleFloorFrom.value);
        if (toBand < fromBand)
            SliderBattleFloorTo.SetValueWithoutNotify(fromBand);
    }

    //選べる開始階が浅いときは、バーの長さもその分だけ短くする。左端は動かさない
    void ResizeFloorFromSlider(int maxBand)
    {
        RememberFloorFromSliderSize();
        if (!FloorFromSliderMeasured || SliderBattleFloorFrom == null)
            return;

        int totalBands = Constants.BATTLE_STAGE_FLOOR_MAX / Constants.BATTLE_STAGE_FLOOR_STEP;
        if (totalBands < 1)
            totalBands = 1;
        if (maxBand < 1)
            maxBand = 1;
        if (maxBand > totalBands)
            maxBand = totalBands;

        RectTransform rect = SliderBattleFloorFrom.GetComponent<RectTransform>();
        float width = FloorFromSliderFullWidth * maxBand / totalBands;
        if (width < 1f)
            width = 1f;
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(FloorFromSliderLeft + width * rect.pivot.x, rect.anchoredPosition.y);
    }

    void SetFloorSliders(int floorFrom, int floorTo)
    {
        int windowStart = ControllerProduction != null ? ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex) : 1;
        int fromBand = (floorFrom - windowStart) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
        int toBand = (floorTo - windowStart) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
        if (SliderBattleFloorFrom != null)
            SliderBattleFloorFrom.SetValueWithoutNotify(fromBand);
        if (SliderBattleFloorTo != null)
            SliderBattleFloorTo.SetValueWithoutNotify(toBand);
    }

    void GetSelectedFloorRange(out int floorFrom, out int floorTo)
    {
        int fromBand = SliderBattleFloorFrom != null ? Mathf.RoundToInt(SliderBattleFloorFrom.value) : 1;
        int toBand = SliderBattleFloorTo != null ? Mathf.RoundToInt(SliderBattleFloorTo.value) : 1;
        if (fromBand < 1)
            fromBand = 1;
        int windowStart = 1;
        if (ControllerProduction != null)
        {
            windowStart = ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex);
            int maxFrom = ControllerProduction.GetMaxBattleFloorFrom(CurrentStageIndex);
            int maxBand = (maxFrom - windowStart) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
            if (fromBand > maxBand)
                fromBand = maxBand;
        }
        if (toBand < fromBand)
            toBand = fromBand;

        floorFrom = windowStart + (fromBand - 1) * Constants.BATTLE_STAGE_FLOOR_STEP;
        floorTo = windowStart + toBand * Constants.BATTLE_STAGE_FLOOR_STEP - 1;
    }

    void EnsureGalleryButton()
    {
        Button source = null;
        for (int i = 0; i < ButtonBattleStages.Length; i++)
        {
            if (ButtonBattleStages[i] != null)
                source = ButtonBattleStages[i];
        }
        if (source == null)
        {
            Debug.LogWarning("ギャラリーのボタンを置く元になるステージボタンがありません");
            return;
        }

        Transform parent = source.transform.parent;
        Transform existing = parent != null ? parent.Find("ButtonBattleStageGallery") : null;
        if (existing != null)
            ButtonGalleryStage = existing.GetComponent<Button>();

        if (ButtonGalleryStage == null)
        {
            GameObject clone = Instantiate(source.gameObject, parent);
            clone.name = "ButtonBattleStageGallery";
            RectTransform sourceRect = source.GetComponent<RectTransform>();
            RectTransform rect = clone.GetComponent<RectTransform>();
            float gap = sourceRect.sizeDelta.x;
            if (gap < 1f)
                gap = 160f;
            rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(gap + 12f, 0f);
            ButtonGalleryStage = clone.GetComponent<Button>();
        }

        if (ButtonGalleryStage == null)
            return;
        ButtonGalleryStage.onClick = new Button.ButtonClickedEvent();
        ButtonGalleryStage.onClick.AddListener(() => PushButtonSelectBattleStage(Constants.BATTLE_STAGE_GALLERY));
    }

    void PaintGalleryButton(int activeStage)
    {
        if (ButtonGalleryStage == null || ControllerProduction == null)
            return;

        bool prairieCleared = ControllerProduction.GetClearedBattleStage() >= 1;
        bool hasImported = ImportedCharacters.Count > 0;
        bool open = prairieCleared && hasImported;
        bool sortieing = activeStage == Constants.BATTLE_STAGE_GALLERY;
        ButtonGalleryStage.interactable = open || sortieing;

        Text label = ButtonGalleryStage.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        if (!open && !sortieing)
        {
            ButtonGalleryStage.image.color = ColorStageLocked;
            if (label == null)
                return;
            if (!prairieCleared)
                label.text = "ギャラリー\n草原をクリアすると開く";
            else
                label.text = "ギャラリー\n読み込んだキャラがいない";
            return;
        }

        ButtonGalleryStage.image.color = (CurrentStageIndex == Constants.BATTLE_STAGE_GALLERY) ? ColorButtonSelected : ColorButtonNormal;
        if (label == null)
            return;

        int start = ControllerProduction.GetBattleFloorWindowStart(Constants.BATTLE_STAGE_GALLERY);
        int end = start + Constants.BATTLE_STAGE_FLOOR_MAX - 1;
        label.text = "ギャラリー\n" + start.ToString() + "〜" + end.ToString() + "階";
        if (sortieing)
            label.text += "\n出撃中";
    }

    bool CanSelectStage(int stageIndex)
    {
        return ControllerProduction.CanEnterStage(stageIndex)
            || stageIndex == ControllerProduction.GetActiveBattleStage();
    }

    Sprite CreateCharacterSprite(uint argCharacterId)
    {
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, argCharacterId);
        Texture2D tex = character != null ? character.ImageTexture2D : null;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        return Sprite.Create(tex, new UnityEngine.Rect(0, 0, character.Size, character.Size), new Vector2(0.5f, 0.5f));
    }
}
