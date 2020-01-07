using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;

static class Constants
{
    public const int CHARACTERS_ALL_NUM = 10;
    public const int CHARACTERS_HELP_PRODUCTION_NUM = 3;
}

//生産画面のコントロール
public class ControllerProduction : MonoBehaviour
{
    ModelProduction ModelProduction;
    CharacterClass[] CharactersAll = new CharacterClass[Constants.CHARACTERS_ALL_NUM + 1];
    uint[] CharactersIDHelpProductionR = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] CharactersIDHelpProductionG = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] CharactersIDHelpProductionB = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];

    //現在の全カラーの個数(CurColors[0,0,0]が黒、CurColors[255,0,0]が赤)
    ulong[,,] CurPixels = new ulong[256,256,256];

    //現在のRGB値
    ulong CurR = 0;
    ulong CurG = 0;
    ulong CurB = 0;

    [SerializeField] Text TextR;
    [SerializeField] Text TextG;
    [SerializeField] Text TextB;
    [SerializeField] Slider SliderR;
    [SerializeField] Slider SliderG;
    [SerializeField] Slider SliderB;

    //最大値
    ulong MaxR = 1024;
    ulong MaxG = 1024;
    ulong MaxB = 1024;

    //最大値を上げるコスト
    ulong CostMaxRUp = 512;
    ulong CostMaxGUp = 512;
    ulong CostMaxBUp = 512;

    [SerializeField] Text TextCostMaxRUp;
    [SerializeField] Text TextCostMaxGUp;
    [SerializeField] Text TextCostMaxBUp;
    [SerializeField] Slider SliderCostMaxRUp;
    [SerializeField] Slider SliderCostMaxGUp;
    [SerializeField] Slider SliderCostMaxBUp;

    [SerializeField] Button ButtonMaxRUp;
    [SerializeField] Button ButtonMaxGUp;
    [SerializeField] Button ButtonMaxBUp;


    //増加値
    ulong IncreaseValueR = 1;
    ulong IncreaseValueG = 1;
    ulong IncreaseValueB = 1;

    [SerializeField] Text TextIncreaseValueRLeft;
    [SerializeField] Text TextIncreaseValueRRight;
    [SerializeField] Text TextIncreaseValueGLeft;
    [SerializeField] Text TextIncreaseValueGRight;
    [SerializeField] Text TextIncreaseValueBLeft;
    [SerializeField] Text TextIncreaseValueBRight;

    //増加値を上げるコスト
    ulong CostIncreaseValueRUp = 16;
    ulong CostIncreaseValueGUp = 16;
    ulong CostIncreaseValueBUp = 16;

    [SerializeField] Text TextCostIncreaseValueRUp;
    [SerializeField] Text TextCostIncreaseValueGUp;
    [SerializeField] Text TextCostIncreaseValueBUp;
    [SerializeField] Slider SliderCostIncreaseValueRUp;
    [SerializeField] Slider SliderCostIncreaseValueGUp;
    [SerializeField] Slider SliderCostIncreaseValueBUp;

    [SerializeField] Button ButtonIncreaseValueRUp;
    [SerializeField] Button ButtonIncreaseValueGUp;
    [SerializeField] Button ButtonIncreaseValueBUp;

    //パネル
    [SerializeField] GameObject PanelSelectCharacter;
    [SerializeField] Image ImageSelectCharacter;
    [SerializeField] Text TextSelectCharacterCreateR;
    [SerializeField] Text TextSelectCharacterCreateG;
    [SerializeField] Text TextSelectCharacterCreateB;

    Button ButtonTmp = null;
    uint CharacterIDTmp = 0;



    ////モンスター
    //[SerializeField] Image ImageRedSlime8;


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("ControllerProduction Start");

        SaveClass SC = new SaveClass();
        SC.Load(ref CurR, ref CurG, ref CurB);



        ModelProduction = GetComponent<ModelProduction>();
        for (int i = 0; i < Constants.CHARACTERS_ALL_NUM + 1; i++)
        {
            CharactersAll[i] = new CharacterClass();
        }

        //キャラ生成
        Debug.Log("キャラ生成");
        CharactersAll[1].MakeCharacter(Resources.Load("Character/RedSlime8", typeof(Texture2D)) as Texture2D, 1, "LittleRedSlime");
        CharactersAll[2].MakeCharacter(Resources.Load("Character/GreenSlime8", typeof(Texture2D)) as Texture2D, 2, "LittleGreenSlime");
        CharactersAll[3].MakeCharacter(Resources.Load("Character/BlueSlime8", typeof(Texture2D)) as Texture2D, 3, "LittleBlueSlime");
        CharactersAll[4].MakeCharacter(Resources.Load("Character/WhiteSlime8", typeof(Texture2D)) as Texture2D, 4, "LittleWhiteSlime");
        CharactersAll[5].MakeCharacter(Resources.Load("Character/RBlackCat8", typeof(Texture2D)) as Texture2D, 5, "LittleRBlackCat");
        CharactersAll[8].MakeCharacter(Resources.Load("Character/WhiteCat8", typeof(Texture2D)) as Texture2D, 8, "LittleWhiteCat");

        //UIの更新
        UpdateProductionScene();
    }

    public void PushButtonSave()
    {
        SaveClass SC = new SaveClass();
        SC.Save(CurR, CurG, CurB);
    }

    private float TimeOut = 1;
    private float TimeElapsed = 0;
    // Update is called once per frame
    void Update()
    {
        //Debug.Log("ControllerProduction Update");

        TimeElapsed += Time.deltaTime;

        if (TimeElapsed >= TimeOut)
        {
            TimeElapsed = 0;

            ulong IncreaseValueRHelp = CharactersAll[CharactersIDHelpProductionR[1]].Stats[0].RCreates +
                                       CharactersAll[CharactersIDHelpProductionR[2]].Stats[0].RCreates +
                                       CharactersAll[CharactersIDHelpProductionR[3]].Stats[0].RCreates;
            ModelProduction.Increase(ref CurR, IncreaseValueRHelp, MaxR);
            UpdateProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);

            ulong IncreaseValueGHelp = CharactersAll[CharactersIDHelpProductionG[1]].Stats[0].GCreates +
                                       CharactersAll[CharactersIDHelpProductionG[2]].Stats[0].GCreates +
                                       CharactersAll[CharactersIDHelpProductionG[3]].Stats[0].GCreates;
            ModelProduction.Increase(ref CurG, IncreaseValueGHelp, MaxG);
            UpdateProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);

            ulong IncreaseValueBHelp = CharactersAll[CharactersIDHelpProductionB[1]].Stats[0].BCreates +
                                       CharactersAll[CharactersIDHelpProductionB[2]].Stats[0].BCreates +
                                       CharactersAll[CharactersIDHelpProductionB[3]].Stats[0].BCreates;
            ModelProduction.Increase(ref CurB, IncreaseValueBHelp, MaxB);
            UpdateProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
        }

    }


    // ButtonRが押されたら
    public void PushButtonR()
    {
        Debug.Log("ButtonRが押された");
        ModelProduction.Increase(ref CurR, IncreaseValueR, MaxR);
        UpdateProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }
    // ButtonGが押されたら
    public void PushButtonG()
    {
        Debug.Log("ButtonGが押された");
        ModelProduction.Increase(ref CurG, IncreaseValueG, MaxG);
        UpdateProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }
    // ButtonBが押されたら
    public void PushButtonB()
    {
        Debug.Log("ButtonBが押された");
        ModelProduction.Increase(ref CurB, IncreaseValueB, MaxB);
        UpdateProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }

    //ButtonIncreaseValueRUpが押されたら
    public void PushButtonIncreaseValueRUp()
    {
        Debug.Log("ButtonIncreaseValueRUpが押された");
        ModelProduction.UpgradeValue(ref CurR, ref CostIncreaseValueRUp, ref IncreaseValueR, ModelProduction.TypeUpgrade.INCREASE);
        UpdateProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }
    //ButtonIncreaseValueGUpが押されたら
    public void PushButtonIncreaseValueGUp()
    {
        Debug.Log("ButtonIncreaseValueGUpが押された");
        ModelProduction.UpgradeValue(ref CurG, ref CostIncreaseValueGUp, ref IncreaseValueG, ModelProduction.TypeUpgrade.INCREASE);
        UpdateProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }
    //ButtonIncreaseValueBUpが押されたら
    public void PushButtonIncreaseValueBUp()
    {
        Debug.Log("ButtonIncreaseValueBUpが押された");
        ModelProduction.UpgradeValue(ref CurB, ref CostIncreaseValueBUp, ref IncreaseValueB, ModelProduction.TypeUpgrade.INCREASE);
        UpdateProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }

    //ButtonMaxRUpが押されたら
    public void PushButtonMaxRUp()
    {
        Debug.Log("ButtonMaxRUpが押された");
        ModelProduction.UpgradeValue(ref CurR, ref CostMaxRUp, ref MaxR, ModelProduction.TypeUpgrade.MAX);
        UpdateProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }

    //ButtonMaxGUpが押されたら
    public void PushButtonMaxGUp()
    {
        Debug.Log("ButtonMaxRUpが押された");
        ModelProduction.UpgradeValue(ref CurG, ref CostMaxGUp, ref MaxG, ModelProduction.TypeUpgrade.MAX);
        UpdateProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }

    //ButtonMaxBUpが押されたら
    public void PushButtonMaxBUp()
    {
        Debug.Log("ButtonMaxRUpが押された");
        ModelProduction.UpgradeValue(ref CurB, ref CostMaxBUp, ref MaxB, ModelProduction.TypeUpgrade.MAX);
        UpdateProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }

    ///////////////////////////////////////////////////
    //キャラクターセレクト
    //RGB生産のヘルプキャラクター選択ボタンが押されたら
    public void PushButtonProductionHelpCharacter(string ButtonName)
    {
        //どのボタンで呼び出されたか保存
        ButtonTmp = GameObject.Find(ButtonName).GetComponent<Button>();

        ShowPanelSelectCharacter();
    }

    //キャラクターセレクトの戻るボタンが押されたら
    public void PushButtonBackProductionHelpCharacter()
    {
        NotShowPanelSelectCharacter();
    }

    //キャラクターセレクトのキャラクターボタンが押されたら
    public void PushButtonProductionHelpCharacterSelect(uint CharacterID)
    {
        Debug.Log("SelectCharacterID : " + CharacterID);
        ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharacterID].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharacterID].Size, CharactersAll[CharacterID].Size), new Vector2(0.5f, 0.5f));
        CharacterIDTmp = CharacterID;
        Button ButtonSelect = GameObject.Find("ButtonConfirmSelectLeft").GetComponent<Button>();
        ButtonSelect.interactable = true;
        ButtonSelect = GameObject.Find("ButtonConfirmSelectRight").GetComponent<Button>();
        ButtonSelect.interactable = true;
    }

    //キャラクターセレクトの決定ボタンが押されたら
    public void PushButtonConfirmSelectProductionHelpCharacter()
    {
        ButtonTmp.image.sprite = ImageSelectCharacter.sprite;
        ButtonTmp.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        ButtonTmp.GetComponentInChildren<Text>().text = "";

        //HelpProductionキャラクターの管理
        if (ButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter")) 
        {
            if(ButtonTmp.name.EndsWith("1"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionR[1]].Whereabouts = Place.None;

                CharactersIDHelpProductionR[1] = CharacterIDTmp;
            }
            else
            if (ButtonTmp.name.EndsWith("2"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionR[2]].Whereabouts = Place.None;

                CharactersIDHelpProductionR[2] = CharacterIDTmp;
            }
            else
            if (ButtonTmp.name.EndsWith("3"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionR[3]].Whereabouts = Place.None;

                CharactersIDHelpProductionR[3] = CharacterIDTmp;
            }
        }
        else
        if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
        {
            if (ButtonTmp.name.EndsWith("1"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionG[1]].Whereabouts = Place.None;

                CharactersIDHelpProductionG[1] = CharacterIDTmp;
            }
            else
            if (ButtonTmp.name.EndsWith("2"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionG[2]].Whereabouts = Place.None;

                CharactersIDHelpProductionG[2] = CharacterIDTmp;
            }
            else
            if (ButtonTmp.name.EndsWith("3"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionG[3]].Whereabouts = Place.None;

                CharactersIDHelpProductionG[3] = CharacterIDTmp;
            }
        }
        else
        if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
        {
            if (ButtonTmp.name.EndsWith("1"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionB[1]].Whereabouts = Place.None;

                CharactersIDHelpProductionB[1] = CharacterIDTmp;
            }
            else
            if (ButtonTmp.name.EndsWith("2"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionB[2]].Whereabouts = Place.None;

                CharactersIDHelpProductionB[2] = CharacterIDTmp;
            }
            else
            if (ButtonTmp.name.EndsWith("3"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionB[3]].Whereabouts = Place.None;

                CharactersIDHelpProductionB[3] = CharacterIDTmp;
            }
        }

        //居場所変更
        CharactersAll[CharacterIDTmp].Whereabouts = Place.CreateR;

        NotShowPanelSelectCharacter();
    }

    //キャラクターセレクトの外すボタンが押されたら
    public void PushButtonRemoveCharacterSelect()
    {
        ButtonTmp.image.sprite = null;
        ButtonTmp.GetComponentInChildren<Text>().text = "+";

        //HelpProductionキャラクターの管理
        if (ButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter"))
        {
            ButtonTmp.image.color = new Color(50/255f, 0.0f, 0.0f, 1.0f);

            if (ButtonTmp.name.EndsWith("1"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionR[1]].Whereabouts = Place.None;

                CharactersIDHelpProductionR[1] = 0;
            }
            else
            if (ButtonTmp.name.EndsWith("2"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionR[2]].Whereabouts = Place.None;

                CharactersIDHelpProductionR[2] = 0;
            }
            else
            if (ButtonTmp.name.EndsWith("3"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionR[3]].Whereabouts = Place.None;

                CharactersIDHelpProductionR[3] = 0;
            }
        }
        else
        if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
        {
            ButtonTmp.image.color = new Color(0.0f, 50/255f, 0.0f, 1.0f);

            if (ButtonTmp.name.EndsWith("1"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionG[1]].Whereabouts = Place.None;

                CharactersIDHelpProductionG[1] = 0;
            }
            else
            if (ButtonTmp.name.EndsWith("2"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionG[2]].Whereabouts = Place.None;

                CharactersIDHelpProductionG[2] = 0;
            }
            else
            if (ButtonTmp.name.EndsWith("3"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionG[3]].Whereabouts = Place.None;

                CharactersIDHelpProductionG[3] = 0;
            }
        }
        else
        if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
        {
            ButtonTmp.image.color = new Color(0.0f, 0.0f, 50/255f, 1.0f);

            if (ButtonTmp.name.EndsWith("1"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionB[1]].Whereabouts = Place.None;

                CharactersIDHelpProductionB[1] = 0;
            }
            else
            if (ButtonTmp.name.EndsWith("2"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionB[2]].Whereabouts = Place.None;

                CharactersIDHelpProductionB[2] = 0;
            }
            else
            if (ButtonTmp.name.EndsWith("3"))
            {
                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionB[3]].Whereabouts = Place.None;

                CharactersIDHelpProductionB[3] = 0;
            }
        }

        NotShowPanelSelectCharacter();
    }

    //キャラクターセレクトパネルの表示
    public void ShowPanelSelectCharacter()
    {
        //PanelSelectCharacterの表示
        CanvasGroup PanelSelectCharacterCanvasGroup = PanelSelectCharacter.GetComponent<CanvasGroup>();
        PanelSelectCharacterCanvasGroup.alpha = 1;
        PanelSelectCharacterCanvasGroup.interactable = true;
        PanelSelectCharacterCanvasGroup.blocksRaycasts = true;

        //選択ボタンのenable設定
        Button ButtonSelect = GameObject.Find("ButtonConfirmSelectLeft").GetComponent<Button>();
        ButtonSelect.interactable = false;
        ButtonSelect = GameObject.Find("ButtonConfirmSelectRight").GetComponent<Button>();
        ButtonSelect.interactable = false;

        //外すボタンのenable設定
        Button ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
        ButtonRemove.interactable = false;
        ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
        ButtonRemove.interactable = false;

        //選択キャラクターの画像とステータスの表示
        if (ButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter"))
        {
            if (ButtonTmp.name.EndsWith("1"))
            {
                if (CharactersIDHelpProductionR[1] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionR[1]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[1]].Size, CharactersAll[CharactersIDHelpProductionR[1]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacterCreateR.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionR[1]].Stats[0].RCreates;
                    TextSelectCharacterCreateG.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionR[1]].Stats[0].GCreates;
                    TextSelectCharacterCreateB.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionR[1]].Stats[0].BCreates;

                    //外すボタンのenable設定
                    ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                    ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                }
            }
            else
            if (ButtonTmp.name.EndsWith("2"))
            {
                if (CharactersIDHelpProductionR[2] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionR[2]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[2]].Size, CharactersAll[CharactersIDHelpProductionR[2]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacterCreateR.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionR[2]].Stats[0].RCreates;
                    TextSelectCharacterCreateG.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionR[2]].Stats[0].GCreates;
                    TextSelectCharacterCreateB.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionR[2]].Stats[0].BCreates;

                    //外すボタンのenable設定
                    ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                    ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                }
            }
            else
            if (ButtonTmp.name.EndsWith("3"))
            {
                if (CharactersIDHelpProductionR[3] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionR[3]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[3]].Size, CharactersAll[CharactersIDHelpProductionR[3]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacterCreateR.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionR[3]].Stats[0].RCreates;
                    TextSelectCharacterCreateG.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionR[3]].Stats[0].GCreates;
                    TextSelectCharacterCreateB.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionR[3]].Stats[0].BCreates;

                    //外すボタンのenable設定
                    ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                    ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                }
            }
        }
        else
        if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
        {
            if (ButtonTmp.name.EndsWith("1"))
            {
                if (CharactersIDHelpProductionG[1] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionG[1]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[1]].Size, CharactersAll[CharactersIDHelpProductionG[1]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacterCreateR.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionG[1]].Stats[0].RCreates;
                    TextSelectCharacterCreateG.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionG[1]].Stats[0].GCreates;
                    TextSelectCharacterCreateB.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionG[1]].Stats[0].BCreates;

                    //外すボタンのenable設定
                    ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                    ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                }
            }
            else
            if (ButtonTmp.name.EndsWith("2"))
            {
                if (CharactersIDHelpProductionG[2] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionG[2]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[2]].Size, CharactersAll[CharactersIDHelpProductionG[2]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacterCreateR.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionG[2]].Stats[0].RCreates;
                    TextSelectCharacterCreateG.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionG[2]].Stats[0].GCreates;
                    TextSelectCharacterCreateB.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionG[2]].Stats[0].BCreates;

                    //外すボタンのenable設定
                    ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                    ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                }
            }
            else
            if (ButtonTmp.name.EndsWith("3"))
            {
                if (CharactersIDHelpProductionG[3] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionG[3]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[3]].Size, CharactersAll[CharactersIDHelpProductionG[3]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacterCreateR.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionG[3]].Stats[0].RCreates;
                    TextSelectCharacterCreateG.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionG[3]].Stats[0].GCreates;
                    TextSelectCharacterCreateB.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionG[3]].Stats[0].BCreates;

                    //外すボタンのenable設定
                    ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                    ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                }
            }
        }
        else
        if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
        {
            if (ButtonTmp.name.EndsWith("1"))
            {
                if (CharactersIDHelpProductionB[1] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionB[1]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[1]].Size, CharactersAll[CharactersIDHelpProductionB[1]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacterCreateR.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionB[1]].Stats[0].RCreates;
                    TextSelectCharacterCreateG.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionB[1]].Stats[0].GCreates;
                    TextSelectCharacterCreateB.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionB[1]].Stats[0].BCreates;

                    //外すボタンのenable設定
                    ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                    ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                }
            }
            else
            if (ButtonTmp.name.EndsWith("2"))
            {
                if (CharactersIDHelpProductionB[2] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionB[2]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[2]].Size, CharactersAll[CharactersIDHelpProductionB[2]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacterCreateR.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionB[2]].Stats[0].RCreates;
                    TextSelectCharacterCreateG.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionB[2]].Stats[0].GCreates;
                    TextSelectCharacterCreateB.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionB[2]].Stats[0].BCreates;

                    //外すボタンのenable設定
                    ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                    ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                }
            }
            else
            if (ButtonTmp.name.EndsWith("3"))
            {
                if (CharactersIDHelpProductionB[3] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionB[3]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[3]].Size, CharactersAll[CharactersIDHelpProductionB[3]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacterCreateR.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionB[3]].Stats[0].RCreates;
                    TextSelectCharacterCreateG.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionB[3]].Stats[0].GCreates;
                    TextSelectCharacterCreateB.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionB[3]].Stats[0].BCreates;

                    //外すボタンのenable設定
                    ButtonRemove = GameObject.Find("ButtonRemoveLeft").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                    ButtonRemove = GameObject.Find("ButtonRemoveRight").GetComponent<Button>();
                    ButtonRemove.interactable = true;
                }
            }
        }

        //キャラクターボタン生成
        GameObject[] ArrayButtonHelpCharacter = new GameObject[Constants.CHARACTERS_ALL_NUM + 1];
        for (int indexCharacter = 0; indexCharacter < Constants.CHARACTERS_ALL_NUM + 1; indexCharacter++)
        {
            if (CharactersAll[indexCharacter].OwnedNumCur != 0 && CharactersAll[indexCharacter].Whereabouts == Place.None)
            {
                //プレハブのインスタンス化
                ArrayButtonHelpCharacter[indexCharacter] = Instantiate((GameObject)Resources.Load("PrefabButtonCharacterImage"), GameObject.Find("Content").transform) as GameObject;
                
                //textの指定
                if (ButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter"))
                {
                    ArrayButtonHelpCharacter[indexCharacter].GetComponentInChildren<Text>().text = CharactersAll[indexCharacter].Stats[0].RCreates.ToString();
                }
                else
                if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
                {
                    ArrayButtonHelpCharacter[indexCharacter].GetComponentInChildren<Text>().text = CharactersAll[indexCharacter].Stats[0].GCreates.ToString();
                }
                else
                if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
                {
                    ArrayButtonHelpCharacter[indexCharacter].GetComponentInChildren<Text>().text = CharactersAll[indexCharacter].Stats[0].BCreates.ToString();
                }

                //spriteの指定
                ArrayButtonHelpCharacter[indexCharacter].GetComponentInChildren<Image>().sprite = Sprite.Create(CharactersAll[indexCharacter].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[indexCharacter].Size, CharactersAll[indexCharacter].Size), new Vector2(0.5f, 0.5f));
                
                //クリックイベントを追加
                uint CharacterID = (uint)(indexCharacter);//匿名メソッドの外部変数のキャプチャの関係で、別の変数に代入
                ArrayButtonHelpCharacter[indexCharacter].GetComponent<Button>().onClick.AddListener(() => PushButtonProductionHelpCharacterSelect(CharacterID));
            }
        }
    }

    //キャラクターセレクトパネルの非表示
    public void NotShowPanelSelectCharacter()
    {
        //どのボタンで呼び出されたか削除
        ButtonTmp = null;
        //どのキャラクターが選ばれたか削除
        CharacterIDTmp = 0;

        //キャラクターボタンの削除
        foreach (Transform child in GameObject.Find("Content").transform)
        {
            Destroy(child.gameObject);
        }

        //ImageSelectCharacterの画像を削除
        ImageSelectCharacter.sprite = null;

        //PanelSelectCharacterの非表示
        CanvasGroup PanelSelectCharacterCanvasGroup = PanelSelectCharacter.GetComponent<CanvasGroup>();
        PanelSelectCharacterCanvasGroup.alpha = 0;
        PanelSelectCharacterCanvasGroup.interactable = false;
        PanelSelectCharacterCanvasGroup.blocksRaycasts = false;
    }


    ////////////////////////////////////////////////
    //View

    //グラフィックをデータと同期させる
    public void UpdateProductionScene()
    {
        UpdateProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
        UpdateProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
        UpdateProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }

    public void UpdateProductionOneColor(ulong ColorValue, ulong MaxValue, Text TextColorValue, Slider SliderColorValue, ulong IncreaseValue, ulong CostIncreaseUp, Text TextIncreaseValueLeft, Text TextIncreaseValueRight, Text TextCostIncreaseValueUp, Slider SliderCostIncrease, Button ButtonIncreaseValueUp, ulong CostMaxValueUp, Text TextCostMaxValueUp, Slider SliderCostMaxValueUp, Button ButtonMaxValueUp)
    {
        TextColorValue.text = string.Format("{0} / {1}", ColorValue, MaxValue);
        SliderColorValue.maxValue = MaxValue;
        SliderColorValue.minValue = 0;
        SliderColorValue.value = ColorValue;

        TextIncreaseValueLeft.text = string.Format("+{0}", IncreaseValue);
        TextIncreaseValueRight.text = string.Format("+{0}", IncreaseValue);
        TextCostIncreaseValueUp.text = string.Format("コスト : {0}", CostIncreaseUp);
        SliderCostIncrease.maxValue = CostIncreaseUp;
        SliderCostIncrease.minValue = 0;
        SliderCostIncrease.value = ColorValue;
        if (ColorValue < CostIncreaseUp)
        {
            ButtonIncreaseValueUp.interactable = false;
        }
        else
        {
            ButtonIncreaseValueUp.interactable = true;
        }

        TextCostMaxValueUp.text = string.Format("コスト : {0}", CostMaxValueUp);
        SliderCostMaxValueUp.maxValue = CostMaxValueUp;
        SliderCostMaxValueUp.minValue = 0;
        SliderCostMaxValueUp.value = ColorValue;
        if (ColorValue < CostMaxValueUp)
        {
            ButtonMaxValueUp.interactable = false;
        }
        else
        {
            ButtonMaxValueUp.interactable = true;
        }

    }




}