using UnityEngine;
using UnityEngine.UI;

//バトルステージ選択パネルのコントロール
public class ControllerBattleStageClass : MonoBehaviour
{
    //マスの名前は「ImageBattleStageCell」+ x + y (例: ImageBattleStageCell13 は x=1, y=3)
    //編成パネルの ButtonBattlePartyCell とは別名。こちらは画像だけで、押しても編成できない
    public const string CELL_IMAGE_PREFIX = "ImageBattleStageCell";

    //ステージ番号は 1～BATTLE_STAGE_NUM。0 は未出撃
    static readonly string[] StageNames = { "", "草原", "森", "洞窟", "遺跡", "城" };
    static readonly ulong[] StageRecommendedHP = { 0, 100, 300, 800, 2000, 5000 };

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
            ControllerProduction.LeaveBattle();
        else
        {
            GetSelectedFloorRange(out int floorFrom, out int floorTo);
            ControllerProduction.EnterBattleStage(CurrentStageIndex, CurrentSetIndex, floorFrom, floorTo);
        }

        Refresh();
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
        if (ControllerProduction.IsBattleFloorRange(activeFloorFrom, activeFloorTo))
            SetFloorSliders(activeFloorFrom, activeFloorTo);
        else
            SetFloorSliders(1, Constants.BATTLE_STAGE_FLOOR_STEP);

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

                memberNum++;
                totalHP += CharactersAll[characterId].Stats[0].HPMax;
                totalATK += CharactersAll[characterId].Stats[0].ATK;
                totalDEF += CharactersAll[characterId].Stats[0].DEF;
            }
        }

        if (TextBattleStagePartyTotal != null)
        {
            TextBattleStagePartyTotal.text = memberNum.ToString() + "/" + (Constants.BATTLE_FORMATION_SIZE * Constants.BATTLE_FORMATION_SIZE).ToString() + "人" +
                "   HP " + totalHP + "   ATK " + totalATK + "   DEF " + totalDEF;
        }

        bool withdrawMode = activeSet != 0 && (activeStage == 0 || activeStage == CurrentStageIndex);
        if (ButtonSortieBattleStage != null)
        {
            ButtonSortieBattleStage.interactable = withdrawMode || (ControllerProduction.IsBattleStageUnlocked(CurrentStageIndex) && memberNum > 0);
            ButtonSortieBattleStage.image.color = withdrawMode ? ColorWithdraw : ColorSortie;
            Text label = ButtonSortieBattleStage.GetComponentInChildren<Text>();
            if (label != null)
                label.text = withdrawMode ? "撤退" : "出撃";
        }

        if (TextBattleFloorRange != null)
        {
            GetSelectedFloorRange(out int floorFrom, out int floorTo);
            TextBattleFloorRange.text = floorFrom.ToString() + "階 〜 " + floorTo.ToString() + "階";

            int activeFloorFrom = ControllerProduction.GetActiveBattleFloorFrom();
            int activeFloorTo = ControllerProduction.GetActiveBattleFloorTo();
            if (activeStage == CurrentStageIndex && activeFloorFrom > 0
                && (activeFloorFrom != floorFrom || activeFloorTo != floorTo))
            {
                TextBattleFloorRange.text += "\n出撃中 " + activeFloorFrom.ToString() + "階 〜 " + activeFloorTo.ToString() + "階";
            }
        }
    }

    void SetFloorSliders(int floorFrom, int floorTo)
    {
        int fromBand = (floorFrom - 1) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
        int toBand = floorTo / Constants.BATTLE_STAGE_FLOOR_STEP;
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
        if (toBand < fromBand)
            toBand = fromBand;

        floorFrom = (fromBand - 1) * Constants.BATTLE_STAGE_FLOOR_STEP + 1;
        floorTo = toBand * Constants.BATTLE_STAGE_FLOOR_STEP;
    }

    bool CanSelectStage(int stageIndex)
    {
        return ControllerProduction.IsBattleStageUnlocked(stageIndex)
            || stageIndex == ControllerProduction.GetActiveBattleStage();
    }

    Sprite CreateCharacterSprite(uint argCharacterId)
    {
        Texture2D tex = CharactersAll[argCharacterId].ImageTexture2D;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        return Sprite.Create(tex, new UnityEngine.Rect(0, 0, CharactersAll[argCharacterId].Size, CharactersAll[argCharacterId].Size), new Vector2(0.5f, 0.5f));
    }
}
