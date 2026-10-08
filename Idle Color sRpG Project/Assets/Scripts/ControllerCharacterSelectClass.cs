using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ControllerCharacterSelectClass : MonoBehaviour
{
    CharacterClass[] CharactersAll;
    Color[] ColorProductionPixel;// = new Color[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1];
    uint[] CharactersIDHelpProductionR;// = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] CharactersIDHelpProductionG;// = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] CharactersIDHelpProductionB;// = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] CharactersIDProductionPixel;// = new uint[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1];
    uint[] CharactersIDProductionCharacter;// = new uint[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
    uint[] CharactersIDProducedCharacter;// = new uint[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];



    //Button ButtonTmp;// = null;
    uint CharacterIDTmp = 0;

    [SerializeField] Image ImageSelectCharacter;
    [SerializeField] Text TextSelectCharacter1;
    [SerializeField] Text TextSelectCharacter2;
    [SerializeField] Text TextSelectCharacter3;

    // Start is called before the first frame update
    public void Initialize(ref CharacterClass[] argCharactersAll,
                           ref Color[] argColorProductionPixel,
                           ref uint[] argCharactersIDHelpProductionR, ref uint[] argCharactersIDHelpProductionG, ref uint[] argCharactersIDHelpProductionB,
                           ref uint[] argCharactersIDProductionPixel,
                           ref uint[] argCharactersIDProductionCharacter, ref uint[] argCharactersIDProducedCharacter)
    {
        CharactersAll = argCharactersAll;
        ColorProductionPixel = argColorProductionPixel;
        CharactersIDHelpProductionR = argCharactersIDHelpProductionR;
        CharactersIDHelpProductionG = argCharactersIDHelpProductionG;
        CharactersIDHelpProductionB = argCharactersIDHelpProductionB;
        CharactersIDProductionPixel = argCharactersIDProductionPixel;
        CharactersIDProductionCharacter = argCharactersIDProductionCharacter;
        CharactersIDProducedCharacter = argCharactersIDProducedCharacter;
    }

    // Start is called before the first frame update
    void Start()
    {

    }
    // Update is called once per frame
    void Update()
    {
        
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //キャラクターセレクト

    //キャラクターセレクトのキャラクターボタンが押されたら
    public void SelectCharacterCharacter(uint argCharacterID, Button ButtonTmp)
    {
        if (IsLockedOutOfProduction(argCharacterID, ButtonTmp))
            return;
        if (ImportedCharacters.FindAny(CharactersAll, argCharacterID) == null)
            return;

        Debug.Log("SelectCharacterID : " + argCharacterID);
        ImageSelectCharacter.sprite = Sprite.Create(ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).ImageTexture2D, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).Size), new Vector2(0.5f, 0.5f));
        CharacterIDTmp = argCharacterID;
        Button ButtonSelect = GameObject.Find("ButtonConfirmSelectLeft").GetComponent<Button>();
        ButtonSelect.interactable = true;
        ButtonSelect = GameObject.Find("ButtonConfirmSelectRight").GetComponent<Button>();
        ButtonSelect.interactable = true;

        //TODO:キャラクターステータスの表示
        if (ButtonTmp.name.Contains("RProductionHelpCharacter") ||
            ButtonTmp.name.Contains("GProductionHelpCharacter") ||
            ButtonTmp.name.Contains("BProductionHelpCharacter"))
        {
            //選択キャラクターのステータスを表示
            TextSelectCharacter1.text = "CreateR : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).Stats[0].RCreates;
            TextSelectCharacter1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "CreateG : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).Stats[0].GCreates;
            TextSelectCharacter2.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "CreateB : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).Stats[0].BCreates;
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 1.0f, 1.0f);
        }
        else
        if (ButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ColorProductionPixelIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            //選択キャラクターのステータスを表示
            TextSelectCharacter1.text = "      SPD       : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).Stats[0].SPD;
            TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "CreatePixel : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).GetCreatePixels((ushort)(ColorProductionPixel[ColorProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].b * 255));
            TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "  Pixel/sec   : " + GetCreatePixelTime(ColorProductionPixel[ColorProductionPixelIndex], argCharacterID)
                                                           * ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).GetCreatePixels((ushort)(ColorProductionPixel[ColorProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].b * 255));
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterColor"))
        {
            TextSelectCharacter1.text = "";
            TextSelectCharacter2.text = "";
            TextSelectCharacter3.text = "";
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProductionCharacter"))
        {
            int ProductionCharacterIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            TextSelectCharacter1.text = "";
            TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "Pixel/sec : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).PaintPixels;
            TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "";
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            int ProductionCharacterIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            TextSelectCharacter1.text = "";
            TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "Pixels : " + ((ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).Size * ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).Size) - ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).APixels);
            TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "";
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        if (ControllerBattlePartyClass.IsCellButton(ButtonTmp))
        {
            ShowBattleStats(argCharacterID);
        }
    }

    public uint GetSelectedCharacterID()
    {
        return CharacterIDTmp;
    }

    //バトル編成のマスから呼ばれたキャラクターセレクトパネルの表示
    public void ShowPanelSelectCharacterBattleParty(Button ButtonTmp, uint argCurrentCharacterID)
    {
        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("SelectCharacter");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ButtonConfirmSelectLeft") ||
                gameObject.name.Equals("ButtonConfirmSelectRight"))
            {
                gameObject.GetComponent<Button>().interactable = false;
            }
            else
            if (gameObject.name.Equals("ButtonRemoveLeft") ||
                gameObject.name.Equals("ButtonRemoveRight"))
            {
                gameObject.GetComponent<Button>().interactable = argCurrentCharacterID != 0;
            }
        }

        ShowBattleStats(argCurrentCharacterID);
        CharacterClass currentCharacter = ImportedCharacters.FindAny(CharactersAll, argCurrentCharacterID);
        if (currentCharacter != null && currentCharacter.ImageTexture2D != null)
        {
            ImageSelectCharacter.sprite = Sprite.Create(currentCharacter.ImageTexture2D, new UnityEngine.Rect(0, 0, currentCharacter.Size, currentCharacter.Size), new Vector2(0.5f, 0.5f));
        }

        //編成に入っているだけでは Whereabouts は変わらない。最大所持数が 1 以上を候補にする
        for (int indexCharacter = 0; indexCharacter < Constants.CHARACTERS_ALL_NUM + 1; indexCharacter++)
        {
            if (ImportedCharacters.FindAny(CharactersAll, (uint)(indexCharacter)).OwnedNumMax != 0)
            {
                CreateCharacterButton(indexCharacter, ButtonTmp, false);
            }
        }
        AddImportedCharacterButtons(ButtonTmp, false);
    }

    //バトル用ステータスの表示。0なら項目名だけ
    void ShowBattleStats(uint argCharacterID)
    {
        TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        TextSelectCharacter3.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);

        if (argCharacterID == 0)
        {
            TextSelectCharacter1.text = "HP : ";
            TextSelectCharacter2.text = "ATK : " + "   DEF : ";
            TextSelectCharacter3.text = "SPD : " + "   LUC : ";
            return;
        }

        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, argCharacterID);
        if (character == null || character.Stats == null || character.Stats[0] == null)
            return;
        StatisticsClass stats = character.Stats[0];
        TextSelectCharacter1.text = "HP : " + stats.HPMax + "   " + character.CharacterType;
        TextSelectCharacter2.text = "ATK : " + stats.ATK + "   DEF : " + stats.DEF;
        TextSelectCharacter3.text = "SPD : " + stats.SPD + "   LUC : " + stats.LUC;
    }

    //キャラクターセレクトの決定ボタンが押されたら
    public void ConfirmSelectCharacter(Button ButtonTmp)
    {
        if (IsLockedOutOfProduction(CharacterIDTmp, ButtonTmp))
            return;

        ButtonTmp.image.sprite = ImageSelectCharacter.sprite;
        ButtonTmp.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        ButtonTmp.GetComponentInChildren<Text>().text = "";

        //HelpRGBProductionキャラクターの管理
        if (ButtonTmp.name.Contains("RProductionHelpCharacter") ||
            ButtonTmp.name.Contains("GProductionHelpCharacter") ||
            ButtonTmp.name.Contains("BProductionHelpCharacter"))
        {
            if (ButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter"))//TODO:リファクタリング
            {
                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                ReleaseCharacterFromOtherProduction(CharacterIDTmp, ButtonTmp);
                //前に設定されていたキャラの居場所変更
                ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionR[HelpProductionIndex])).Whereabouts = Place.None;
                CharactersIDHelpProductionR[HelpProductionIndex] = CharacterIDTmp;

                //居場所変更
                ImportedCharacters.FindAny(CharactersAll, (uint)(CharacterIDTmp)).Whereabouts = Place.CreateR;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
            {
                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                ReleaseCharacterFromOtherProduction(CharacterIDTmp, ButtonTmp);
                //前に設定されていたキャラの居場所変更
                ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionG[HelpProductionIndex])).Whereabouts = Place.None;
                CharactersIDHelpProductionG[HelpProductionIndex] = CharacterIDTmp;

                //居場所変更
                ImportedCharacters.FindAny(CharactersAll, (uint)(CharacterIDTmp)).Whereabouts = Place.CreateG;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
            {
                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                ReleaseCharacterFromOtherProduction(CharacterIDTmp, ButtonTmp);
                //前に設定されていたキャラの居場所変更
                ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionB[HelpProductionIndex])).Whereabouts = Place.None;
                CharactersIDHelpProductionB[HelpProductionIndex] = CharacterIDTmp;

                //居場所変更
                ImportedCharacters.FindAny(CharactersAll, (uint)(CharacterIDTmp)).Whereabouts = Place.CreateB;
            }
        }
        else
        //Pixel生産のキャラクターの管理
        if(ButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ProductionPixelIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            ReleaseCharacterFromOtherProduction(CharacterIDTmp, ButtonTmp);
            //前に設定されていたキャラの居場所変更
            ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).Whereabouts = Place.None;


            CharactersIDProductionPixel[ProductionPixelIndex] = CharacterIDTmp;

            //居場所変更
            ImportedCharacters.FindAny(CharactersAll, (uint)(CharacterIDTmp)).Whereabouts = Place.CreatePixel;
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProductionCharacter"))
        {
            int ProductionCharacterIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            ReleaseCharacterFromOtherProduction(CharacterIDTmp, ButtonTmp);
            //前に設定されていたキャラの居場所変更
            ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionCharacter[ProductionCharacterIndex])).Whereabouts = Place.None;

            CharactersIDProductionCharacter[ProductionCharacterIndex] = CharacterIDTmp;

            //居場所変更
            ImportedCharacters.FindAny(CharactersAll, (uint)(CharacterIDTmp)).Whereabouts = Place.CreateCharacter;
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            int ProductionCharacterIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            CharactersIDProducedCharacter[ProductionCharacterIndex] = CharacterIDTmp;
        }


        NotShowPanelSelectCharacter();
    }

    //キャラクターセレクトの外すボタンが押されたら
    public void RemoveCharacterSelect(Button ButtonTmp)
    {
        ButtonTmp.image.sprite = null;
        ButtonTmp.GetComponentInChildren<Text>().text = "+";

        //HelpProductionキャラクターの管理
        if (ButtonTmp.name.Contains("RProductionHelpCharacter") ||
            ButtonTmp.name.Contains("GProductionHelpCharacter") ||
            ButtonTmp.name.Contains("BProductionHelpCharacter"))
        {

            if (ButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter"))//TODO:リファクタリング
            {
                ButtonTmp.image.color = new Color(50 / 255f, 0.0f, 0.0f, 1.0f);

                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                //前に設定されていたキャラの居場所変更
                ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionR[HelpProductionIndex])).Whereabouts = Place.None;

                CharactersIDHelpProductionR[HelpProductionIndex] = 0;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
            {
                ButtonTmp.image.color = new Color(0.0f, 50 / 255f, 0.0f, 1.0f);

                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                //前に設定されていたキャラの居場所変更
                ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionG[HelpProductionIndex])).Whereabouts = Place.None;

                CharactersIDHelpProductionG[HelpProductionIndex] = 0;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
            {
                ButtonTmp.image.color = new Color(0.0f, 0.0f, 50 / 255f, 1.0f);

                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                //前に設定されていたキャラの居場所変更
                ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionB[HelpProductionIndex])).Whereabouts = Place.None;

                CharactersIDHelpProductionB[HelpProductionIndex] = 0;
            }
        }
        else
        //Pixel生産のキャラクターの管理
        if (ButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ProductionPixelIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            //前に設定されていたキャラの居場所変更
            ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).Whereabouts = Place.None;

            CharactersIDProductionPixel[ProductionPixelIndex] = 0;

        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProductionCharacter"))
        {
            int ProductionCharacterIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            //前に設定されていたキャラの居場所変更
            ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionCharacter[ProductionCharacterIndex])).Whereabouts = Place.None;

            CharactersIDProductionCharacter[ProductionCharacterIndex] = 0;
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            int ProductionCharacterIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            CharactersIDProducedCharacter[ProductionCharacterIndex] = 0;
        }

        NotShowPanelSelectCharacter();
    }

    //キャラクターセレクトパネルの表示
    public void ShowPanelSelectCharacter(Button ButtonTmp)
    {
        //選択ボタンと外すボタンのenable設定
        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("SelectCharacter");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ButtonConfirmSelectLeft") ||
                gameObject.name.Equals("ButtonConfirmSelectRight") ||
                gameObject.name.Equals("ButtonRemoveLeft") ||
                gameObject.name.Equals("ButtonRemoveRight"))
            {
                gameObject.GetComponent<Button>().interactable = false;
            }
        }

        //選択キャラクターの画像とステータスの表示と外すボタンのenable設定
        if (ButtonTmp.name.Contains("RProductionHelpCharacter") ||
            ButtonTmp.name.Contains("GProductionHelpCharacter") ||
            ButtonTmp.name.Contains("BProductionHelpCharacter"))
        {
            //選択キャラクターのステータスの初期表示
            TextSelectCharacter1.text = "CreateR : ";
            TextSelectCharacter1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "CreateG : ";
            TextSelectCharacter2.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "CreateB : ";
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 1.0f, 1.0f);

            if (ButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter"))//TODO:リファクタリング
            {
                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                if (CharactersIDHelpProductionR[HelpProductionIndex] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionR[HelpProductionIndex])).ImageTexture2D, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionR[HelpProductionIndex])).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionR[HelpProductionIndex])).Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacter1.text = "CreateR : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionR[HelpProductionIndex])).Stats[0].RCreates;
                    TextSelectCharacter1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    TextSelectCharacter2.text = "CreateG : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionR[HelpProductionIndex])).Stats[0].GCreates;
                    TextSelectCharacter2.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    TextSelectCharacter3.text = "CreateB : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionR[HelpProductionIndex])).Stats[0].BCreates;
                    TextSelectCharacter3.color = new Color(0.0f, 0.0f, 1.0f, 1.0f);

                    //外すボタンのenable設定
                    foreach (GameObject gameObject in tag1_Objects)
                    {
                        if (gameObject.name.Equals("ButtonRemoveLeft") ||
                            gameObject.name.Equals("ButtonRemoveRight"))
                        {
                            gameObject.GetComponent<Button>().interactable = true;
                        }
                    }
                }

            }
            else
            if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
            {
                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                if (CharactersIDHelpProductionG[HelpProductionIndex] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionG[HelpProductionIndex])).ImageTexture2D, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionG[HelpProductionIndex])).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionG[HelpProductionIndex])).Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacter1.text = "CreateR : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionG[HelpProductionIndex])).Stats[0].RCreates;
                    TextSelectCharacter1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    TextSelectCharacter2.text = "CreateG : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionG[HelpProductionIndex])).Stats[0].GCreates;
                    TextSelectCharacter2.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    TextSelectCharacter3.text = "CreateB : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionG[HelpProductionIndex])).Stats[0].BCreates;
                    TextSelectCharacter3.color = new Color(0.0f, 0.0f, 1.0f, 1.0f);

                    //外すボタンのenable設定
                    foreach (GameObject gameObject in tag1_Objects)
                    {
                        if (gameObject.name.Equals("ButtonRemoveLeft") ||
                            gameObject.name.Equals("ButtonRemoveRight"))
                        {
                            gameObject.GetComponent<Button>().interactable = true;
                        }
                    }
                }

            }
            else
            if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
            {
                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                if (CharactersIDHelpProductionB[HelpProductionIndex] != 0)
                {
                    //選択キャラクターの画像を表示
                    ImageSelectCharacter.sprite = Sprite.Create(ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionB[HelpProductionIndex])).ImageTexture2D, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionB[HelpProductionIndex])).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionB[HelpProductionIndex])).Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacter1.text = "CreateR : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionB[HelpProductionIndex])).Stats[0].RCreates;
                    TextSelectCharacter1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    TextSelectCharacter2.text = "CreateG : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionB[HelpProductionIndex])).Stats[0].GCreates;
                    TextSelectCharacter2.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    TextSelectCharacter3.text = "CreateB : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDHelpProductionB[HelpProductionIndex])).Stats[0].BCreates;
                    TextSelectCharacter3.color = new Color(0.0f, 0.0f, 1.0f, 1.0f);

                    //外すボタンのenable設定
                    foreach (GameObject gameObject in tag1_Objects)
                    {
                        if (gameObject.name.Equals("ButtonRemoveLeft") ||
                            gameObject.name.Equals("ButtonRemoveRight"))
                        {
                            gameObject.GetComponent<Button>().interactable = true;
                        }
                    }
                }

            }
        }
        else
        if (ButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            //選択キャラクターのステータスの初期表示
            TextSelectCharacter1.text = "      SPD       : ";
            TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "CreatePixel : ";
            TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "  Pixel/sec   : ";
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);

            int ProductionPixelIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            if (CharactersIDProductionPixel[ProductionPixelIndex] != 0)
            {
                //選択キャラクターの画像を表示
                ImageSelectCharacter.sprite = Sprite.Create(ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).ImageTexture2D, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).Size), new Vector2(0.5f, 0.5f));
                //選択キャラクターのステータスを表示
                TextSelectCharacter1.text = "      SPD       : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).Stats[0].SPD;
                TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                TextSelectCharacter2.text = "CreatePixel : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).GetCreatePixels((ushort)(ColorProductionPixel[ProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ProductionPixelIndex].b * 255));
                TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                TextSelectCharacter3.text = "  Pixel/sec   : " + GetCreatePixelTime(ColorProductionPixel[ProductionPixelIndex], CharactersIDProductionPixel[ProductionPixelIndex])
                                                               * ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).GetCreatePixels((ushort)(ColorProductionPixel[ProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ProductionPixelIndex].b * 255));
                TextSelectCharacter3.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);

                //外すボタンのenable設定
                foreach (GameObject gameObject in tag1_Objects)
                {
                    if (gameObject.name.Equals("ButtonRemoveLeft") ||
                        gameObject.name.Equals("ButtonRemoveRight"))
                    {
                        gameObject.GetComponent<Button>().interactable = true;
                    }
                }
            }

        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterColor"))
        {
            TextSelectCharacter1.text = "";
            TextSelectCharacter2.text = "";
            TextSelectCharacter3.text = "";
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProductionCharacter"))
        {
            TextSelectCharacter1.text = "";
            TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "Pixel/sec : ";
            TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "";
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);

            int ProductionCharacterIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));
            if (CharactersIDProductionCharacter[ProductionCharacterIndex] != 0)
            {
                //選択キャラクターの画像を表示
                ImageSelectCharacter.sprite = Sprite.Create(ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionCharacter[ProductionCharacterIndex])).ImageTexture2D, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionCharacter[ProductionCharacterIndex])).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionCharacter[ProductionCharacterIndex])).Size), new Vector2(0.5f, 0.5f));

                TextSelectCharacter2.text = "Pixel/sec : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionCharacter[ProductionCharacterIndex])).PaintPixels;

                //外すボタンのenable設定
                foreach (GameObject gameObject in tag1_Objects)
                {
                    if (gameObject.name.Equals("ButtonRemoveLeft") ||
                        gameObject.name.Equals("ButtonRemoveRight"))
                    {
                        gameObject.GetComponent<Button>().interactable = true;
                    }
                }
            }
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            TextSelectCharacter1.text = "";
            TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "Pixels : ";
            TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "";
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);

            int ProductionCharacterIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));
            if (CharactersIDProducedCharacter[ProductionCharacterIndex] != 0)
            {
                //選択キャラクターの画像を表示
                ImageSelectCharacter.sprite = Sprite.Create(ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProducedCharacter[ProductionCharacterIndex])).ImageTexture2D, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProducedCharacter[ProductionCharacterIndex])).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProducedCharacter[ProductionCharacterIndex])).Size), new Vector2(0.5f, 0.5f));

                TextSelectCharacter2.text = "Pixels : " + ((ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProducedCharacter[ProductionCharacterIndex])).Size * ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProducedCharacter[ProductionCharacterIndex])).Size) - ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProducedCharacter[ProductionCharacterIndex])).APixels);

                //外すボタンのenable設定
                foreach (GameObject gameObject in tag1_Objects)
                {
                    if (gameObject.name.Equals("ButtonRemoveLeft") ||
                        gameObject.name.Equals("ButtonRemoveRight"))
                    {
                        gameObject.GetComponent<Button>().interactable = true;
                    }
                }
            }
        }

        //キャラクターボタン生成
        if (ButtonTmp.name.Contains("RProductionHelpCharacter") ||
        ButtonTmp.name.Contains("GProductionHelpCharacter") ||
        ButtonTmp.name.Contains("BProductionHelpCharacter") ||
        ButtonTmp.name.Contains("ButtonPixelProductionCharacter") ||
        ButtonTmp.name.Contains("ButtonCharacterProductionCharacter"))
        {
            for (int indexCharacter = 0; indexCharacter < Constants.CHARACTERS_ALL_NUM + 1; indexCharacter++)
            {
                if (ImportedCharacters.FindAny(CharactersAll, (uint)(indexCharacter)).OwnedNumMax != 0)
                {
                    CreateCharacterButton(indexCharacter, ButtonTmp, true);
                }
            }
            AddImportedCharacterButtons(ButtonTmp, true);
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterColor") ||
            ButtonTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            for (int indexCharacter = 0; indexCharacter < Constants.CHARACTERS_ALL_NUM + 1; indexCharacter++)
            {
                if (ImportedCharacters.FindAny(CharactersAll, (uint)(indexCharacter)).OwnedNumMax != 0)
                {
                    CreateCharacterButton(indexCharacter, ButtonTmp, true);
                }
            }
            AddImportedCharacterButtons(ButtonTmp, true);
        }
    }

    void AddImportedCharacterButtons(Button source, bool warnIfUsedInOtherProduction)
    {
        ImportedCharacters.ForEach(character =>
        {
            if (character == null || character.OwnedNumMax == 0)
                return;
            CreateCharacterButton((int)character.ID, source, warnIfUsedInOtherProduction);
        });
    }
    //キャラクターボタンの作成
    private void CreateCharacterButton(int argCharacterIndex, Button argButtonTmp, bool warnIfUsedInOtherProduction)
    {
        CharacterClass buttonCharacter = ImportedCharacters.FindAny(CharactersAll, (uint)argCharacterIndex);
        if (buttonCharacter == null)
            return;
        Debug.Log("CreateCharacterButton argCharacterIndex = " + argCharacterIndex + " \n Path = " + buttonCharacter.ImagePath);

        GameObject gameObjectCharacterList = null;
        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("SelectCharacter");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ContentCharacterList"))
            {
                gameObjectCharacterList = gameObject;
            }
        }

        //プレハブのインスタンス化
        GameObject GameObjectCharacterButton = Instantiate((GameObject)Resources.Load("PrefabButtonCharacterImage"), gameObjectCharacterList.transform) as GameObject;

        //spriteの指定
        //GameObjectCharacterButton.GetComponentInChildren<Image>().sprite = Sprite.Create(ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).ImageTexture2D, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Size), new Vector2(0.5f, 0.5f));
        // --- 修正案：画像パスの安全チェック ---
        Sprite characterSprite = null;

        // パスがちゃんと入っているかチェック
        if (!string.IsNullOrWhiteSpace(ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).ImagePath))
        {
            // パスがあるときだけ画像を読み込む
            Texture2D tex = ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).ImageTexture2D;
            if (tex != null)
            {
                characterSprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Size), new Vector2(0.5f, 0.5f));
            }
        }

        // もし画像が読み込めなかった（パスが空、またはファイルがなかった）場合の予備
        if (characterSprite == null)
        {
            // 代わりの「NO IMAGE」的な仮スプライトを入れる（または Resources から読み込むなど）
            characterSprite = Resources.Load<Sprite>("NoImageSprite"); 
            Debug.LogWarning($"{argCharacterIndex}番目のキャラの画像パスが空のため、スプライトを生成できませんでした。");
        }

        // 確定したスプライトをセットする
        GameObjectCharacterButton.GetComponentInChildren<Image>().sprite = characterSprite;




        //textの指定
        if (argButtonTmp.name.Contains("RProductionHelpCharacter") ||
            argButtonTmp.name.Contains("GProductionHelpCharacter") ||
            argButtonTmp.name.Contains("BProductionHelpCharacter"))
        {
            if (argButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter"))
            {
                GameObjectCharacterButton.GetComponentInChildren<Text>().text = "CreateR/sec : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Stats[0].RCreates.ToString();
                GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            }
            else
            if (argButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
            {
                GameObjectCharacterButton.GetComponentInChildren<Text>().text = "CreateG/sec : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Stats[0].GCreates.ToString();
                GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
            }
            else
            if (argButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
            {
                GameObjectCharacterButton.GetComponentInChildren<Text>().text = "CreateB/sec : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Stats[0].BCreates.ToString();
                GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 1.0f, 1.0f);
            }
        }
        else
        if (argButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ColorProductionPixelIndex = int.Parse(argButtonTmp.name.Substring(argButtonTmp.name.Length - 2, 2));

            GameObjectCharacterButton.GetComponentInChildren<Text>().text = "Pixel/sec : " + GetCreatePixelTime(ColorProductionPixel[ColorProductionPixelIndex], (uint)argCharacterIndex)
                                                               * ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).GetCreatePixels((ushort)(ColorProductionPixel[ColorProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].b * 255));
            GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        if (argButtonTmp.name.Contains("ButtonCharacterColor"))
        {
            GameObjectCharacterButton.GetComponentInChildren<Text>().text = ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Name;
            GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        if (argButtonTmp.name.Contains("ButtonCharacterProductionCharacter"))
        {
            GameObjectCharacterButton.GetComponentInChildren<Text>().text = "Pixel/sec : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).PaintPixels;
            GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        if (argButtonTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            GameObjectCharacterButton.GetComponentInChildren<Text>().text = "Pixels : " + ((ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Size * ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Size) - ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).APixels);
            GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        if (ControllerBattlePartyClass.IsCellButton(argButtonTmp))
        {
            GameObjectCharacterButton.GetComponentInChildren<Text>().text = "HP " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Stats[0].HPMax + " / ATK " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Stats[0].ATK;
            GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }

        //出撃中のキャラは生産へ割り当てられない。一覧には残して、グレーで押せなくする
        if (IsLockedOutOfProduction((uint)argCharacterIndex, argButtonTmp))
        {
            GrayOutCharacterButton(GameObjectCharacterButton);
            return;
        }

        if (warnIfUsedInOtherProduction && IsUsedInOtherProduction((uint)argCharacterIndex, argButtonTmp))
            AddFaintWarningMark(GameObjectCharacterButton);

        //クリックイベントを追加
        uint CharacterID = (uint)(argCharacterIndex);//匿名メソッドの外部変数のキャプチャの関係で、別の変数に代入
        GameObjectCharacterButton.GetComponent<Button>().onClick.AddListener(() => SelectCharacterCharacter(CharacterID, argButtonTmp));
    }

    static bool IsProductionAssignmentButton(Button button)
    {
        if (button == null)
            return false;
        string name = button.name;
        return name.Contains("RProductionHelpCharacter")
            || name.Contains("GProductionHelpCharacter")
            || name.Contains("BProductionHelpCharacter")
            || name.Contains("ButtonPixelProductionCharacter")
            || name.Contains("ButtonCharacterProductionCharacter");
    }

    bool IsLockedOutOfProduction(uint characterId, Button sourceButton)
    {
        if (!IsProductionAssignmentButton(sourceButton))
            return false;
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, characterId);
        if (character == null || character.ID != characterId)
            return false;
        return character.Whereabouts == Place.Battle;
    }

    static void GrayOutCharacterButton(GameObject buttonObject)
    {
        Button button = buttonObject.GetComponent<Button>();
        Image image = buttonObject.GetComponentInChildren<Image>();
        if (image != null)
            image.color = new Color(0.45f, 0.45f, 0.45f, 1f);

        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
            button.interactable = false;
        }

        Text[] labels = buttonObject.GetComponentsInChildren<Text>();
        for (int i = 0; i < labels.Length; i++)
        {
            Color color = labels[i].color;
            labels[i].color = Color.Lerp(color, new Color(0.45f, 0.45f, 0.45f, color.a), 0.75f);
        }
    }

    static bool IsProductionPlace(Place place)
    {
        return place == Place.CreateR || place == Place.CreateG || place == Place.CreateB
            || place == Place.CreatePixel || place == Place.CreateCharacter;
    }

    bool IsUsedInOtherProduction(uint characterId, Button sourceButton)
    {
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, characterId);
        if (character == null || character.ID == 0 || !IsProductionPlace(character.Whereabouts))
            return false;
        return !IsCharacterInSourceSlot(characterId, sourceButton);
    }

    bool IsCharacterInSourceSlot(uint characterId, Button sourceButton)
    {
        if (sourceButton == null)
            return false;
        string name = sourceButton.name;
        if (name.StartsWith("ButtonRProductionHelpCharacter"))
            return CharactersIDHelpProductionR[int.Parse(name.Substring(name.Length - 1, 1))] == characterId;
        if (name.StartsWith("ButtonGProductionHelpCharacter"))
            return CharactersIDHelpProductionG[int.Parse(name.Substring(name.Length - 1, 1))] == characterId;
        if (name.StartsWith("ButtonBProductionHelpCharacter"))
            return CharactersIDHelpProductionB[int.Parse(name.Substring(name.Length - 1, 1))] == characterId;
        if (name.Contains("ButtonPixelProductionCharacter"))
            return CharactersIDProductionPixel[int.Parse(name.Substring(name.Length - 2, 2))] == characterId;
        if (name.Contains("ButtonCharacterProductionCharacter"))
            return CharactersIDProductionCharacter[int.Parse(name.Substring(name.Length - 2, 2))] == characterId;
        return false;
    }

    void ReleaseCharacterFromOtherProduction(uint characterId, Button destination)
    {
        if (characterId == 0)
            return;

        for (int i = 1; i <= Constants.CHARACTERS_HELP_PRODUCTION_NUM; i++)
        {
            if (CharactersIDHelpProductionR[i] == characterId && !IsSameButton(destination, "ButtonRProductionHelpCharacter" + i))
            {
                CharactersIDHelpProductionR[i] = 0;
                ClearProductionButton("ButtonRProductionHelpCharacter" + i, new Color(50 / 255f, 0.0f, 0.0f, 1.0f));
            }
            if (CharactersIDHelpProductionG[i] == characterId && !IsSameButton(destination, "ButtonGProductionHelpCharacter" + i))
            {
                CharactersIDHelpProductionG[i] = 0;
                ClearProductionButton("ButtonGProductionHelpCharacter" + i, new Color(0.0f, 50 / 255f, 0.0f, 1.0f));
            }
            if (CharactersIDHelpProductionB[i] == characterId && !IsSameButton(destination, "ButtonBProductionHelpCharacter" + i))
            {
                CharactersIDHelpProductionB[i] = 0;
                ClearProductionButton("ButtonBProductionHelpCharacter" + i, new Color(0.0f, 0.0f, 50 / 255f, 1.0f));
            }
        }

        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
        {
            if (CharactersIDProductionPixel[i] == characterId && !IsSameButton(destination, "ButtonPixelProductionCharacter" + i.ToString("00")))
            {
                CharactersIDProductionPixel[i] = 0;
                ClearProductionButton("ButtonPixelProductionCharacter" + i.ToString("00"), Color.white);
            }
        }

        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM; i++)
        {
            if (CharactersIDProductionCharacter[i] == characterId && !IsSameButton(destination, "ButtonCharacterProductionCharacter" + i.ToString("00")))
            {
                CharactersIDProductionCharacter[i] = 0;
                ClearProductionButton("ButtonCharacterProductionCharacter" + i.ToString("00"), Color.white);
            }
        }
    }

    static bool IsSameButton(Button button, string buttonName)
    {
        return button != null && button.name == buttonName;
    }

    static void ClearProductionButton(string buttonName, Color emptyColor)
    {
        GameObject buttonObject = GameObject.Find(buttonName);
        if (buttonObject == null)
            return;
        Button button = buttonObject.GetComponent<Button>();
        if (button == null || button.image == null)
            return;
        button.image.sprite = null;
        button.image.color = emptyColor;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.text = "+";
    }

    public static void AddFaintWarningMark(GameObject buttonObject)
    {
        Text source = buttonObject.GetComponentInChildren<Text>();
        GameObject markObject = new GameObject("ProductionWarning", typeof(RectTransform));
        markObject.transform.SetParent(buttonObject.transform, false);
        RectTransform rect = markObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text mark = markObject.AddComponent<Text>();
        mark.font = source != null && source.font != null
            ? source.font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mark.text = "!";
        mark.fontSize = 72;
        mark.fontStyle = FontStyle.Bold;
        mark.alignment = TextAnchor.MiddleCenter;
        mark.color = new Color(1.0f, 0.75f, 0.05f, 0.7f);
        mark.raycastTarget = false;
        mark.resizeTextForBestFit = true;
        mark.resizeTextMinSize = 24;
        mark.resizeTextMaxSize = 80;

        Outline outline = markObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2.5f, 2.5f);
    }

    //キャラクターセレクトパネルの非表示
    public void NotShowPanelSelectCharacter()
    {
        //どのキャラクターが選ばれたか削除
        CharacterIDTmp = 0;

        GameObject gameObjectCharacterList = null;
        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("SelectCharacter");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ContentCharacterList"))
            {
                gameObjectCharacterList = gameObject;
            }
        }
        //キャラクターボタンの削除
        foreach (Transform child in gameObjectCharacterList.transform)
        {
            Destroy(child.gameObject);
        }

        //ImageSelectCharacterの画像を削除
        ImageSelectCharacter.sprite = null;
    }


    int GetCreatePixelTime(Color argColor, uint argCharacterID)
    {
        return GetCreatePixelTimeRGB((ushort)(argColor.r * 255), argCharacterID) +
               GetCreatePixelTimeRGB((ushort)(argColor.g * 255), argCharacterID) +
               GetCreatePixelTimeRGB((ushort)(argColor.b * 255), argCharacterID);
    }
    int GetCreatePixelTimeRGB(ushort argRGB, uint argCharacterID)
    {
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, argCharacterID);
        if (character == null || character.Stats == null || character.Stats[0] == null || character.Stats[0].SPD < 1)
            return 0;
        return (int)(Mathf.Ceil((argRGB + 1) / (float)(character.Stats[0].SPD)));
    }
}
