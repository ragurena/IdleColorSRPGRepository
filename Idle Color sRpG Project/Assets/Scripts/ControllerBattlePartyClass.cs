using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//バトル編成パネルのコントロール
public class ControllerBattlePartyClass : MonoBehaviour
{
    //マスのボタン名は「ButtonBattlePartyCell」+ x + y (例: ButtonBattlePartyCell13 は x=1, y=3)
    public const string CELL_BUTTON_PREFIX = "ButtonBattlePartyCell";

    CharacterClass[] CharactersAll;
    ControllerProduction ControllerProduction;

    //表示中の編成セット(1～BATTLE_PARTY_SET_NUM)
    int CurrentSetIndex = 1;

    //[0]がセット1のタブ
    [SerializeField] Button[] ButtonBattlePartySets = new Button[Constants.BATTLE_PARTY_SET_NUM];
    [SerializeField] Text TextBattlePartyTotal;
    [SerializeField] Button ButtonClearBattleParty;

    Button[,] ButtonCells = new Button[Constants.BATTLE_FORMATION_SIZE + 1, Constants.BATTLE_FORMATION_SIZE + 1];

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

                memberNum++;
                totalHP += CharactersAll[characterId].Stats[0].HPMax;
                totalATK += CharactersAll[characterId].Stats[0].ATK;
                totalDEF += CharactersAll[characterId].Stats[0].DEF;
            }
        }

        TextBattlePartyTotal.text = memberNum.ToString() + "/" + (Constants.BATTLE_FORMATION_SIZE * Constants.BATTLE_FORMATION_SIZE).ToString() + "人" +
                                    "   HP " + totalHP + "   ATK " + totalATK + "   DEF " + totalDEF;

        ButtonClearBattleParty.interactable = !isActiveSet && memberNum > 0;
    }

    Sprite CreateCharacterSprite(uint argCharacterId)
    {
        Texture2D tex = CharactersAll[argCharacterId].ImageTexture2D;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        return Sprite.Create(tex, new UnityEngine.Rect(0, 0, CharactersAll[argCharacterId].Size, CharactersAll[argCharacterId].Size), new Vector2(0.5f, 0.5f));
    }
}
