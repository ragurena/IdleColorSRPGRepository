
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
    uint[] CharactersIDProductionCharacter;
    uint[] CharactersIDProducedCharacter;
    uint[] PersonIdHelpProductionR;
    uint[] PersonIdHelpProductionG;
    uint[] PersonIdHelpProductionB;
    uint[] PersonIdProductionPixel;
    uint[] PersonIdProductionCharacter;
    PersonClass[] People;
    PersonBalanceConfig PersonBalance;
    System.Func<int, bool> PersonSortieCheck;
    int SelectedPersonId;
    GameObject WorkerTabs;
    Button _characterTab;
    Button _personTab;
    Button _workerToggle;
    bool _showingCharacters = true;
    bool _sceneWorkerTabs;
    bool _workerTabsReady;
    Sprite _previewSprite;
    readonly List<Sprite> _listSprites = new List<Sprite>();



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
                           ref uint[] argCharactersIDProductionCharacter, ref uint[] argCharactersIDProducedCharacter,
                           ref uint[] argPersonIdHelpProductionR, ref uint[] argPersonIdHelpProductionG, ref uint[] argPersonIdHelpProductionB,
                           ref uint[] argPersonIdProductionPixel, ref uint[] argPersonIdProductionCharacter,
                           PersonClass[] people, PersonBalanceConfig personBalance)
    {
        CharactersAll = argCharactersAll;
        ColorProductionPixel = argColorProductionPixel;
        CharactersIDHelpProductionR = argCharactersIDHelpProductionR;
        CharactersIDHelpProductionG = argCharactersIDHelpProductionG;
        CharactersIDHelpProductionB = argCharactersIDHelpProductionB;
        CharactersIDProductionPixel = argCharactersIDProductionPixel;
        CharactersIDProductionCharacter = argCharactersIDProductionCharacter;
        CharactersIDProducedCharacter = argCharactersIDProducedCharacter;
        PersonIdHelpProductionR = argPersonIdHelpProductionR;
        PersonIdHelpProductionG = argPersonIdHelpProductionG;
        PersonIdHelpProductionB = argPersonIdHelpProductionB;
        PersonIdProductionPixel = argPersonIdProductionPixel;
        PersonIdProductionCharacter = argPersonIdProductionCharacter;
        People = people;
        PersonBalance = personBalance;
    }

    public void SetPersonSortieCheck(System.Func<int, bool> check)
    {
        PersonSortieCheck = check;
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
        SelectedPersonId = 0;
        if (ImportedCharacters.FindAny(CharactersAll, argCharacterID) == null)
            return;

        Debug.Log("SelectCharacterID : " + argCharacterID);
        ShowCharacterPreview(argCharacterID);
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
            int ColorProductionPixelIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

            //選択キャラクターのステータスを表示
            TextSelectCharacter1.text = "      SPD       : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).Stats[0].SPD;
            TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "CreatePixel : " + ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).GetCreatePixels((ushort)CharacterClass.ColorByte(ColorProductionPixel[ColorProductionPixelIndex].r), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ColorProductionPixelIndex].g), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ColorProductionPixelIndex].b));
            TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "  Pixel/sec   : " + GetCreatePixelTime(ColorProductionPixel[ColorProductionPixelIndex], argCharacterID)
                                                           * ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterID)).GetCreatePixels((ushort)CharacterClass.ColorByte(ColorProductionPixel[ColorProductionPixelIndex].r), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ColorProductionPixelIndex].g), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ColorProductionPixelIndex].b));
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
            int ProductionCharacterIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

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
            int ProductionCharacterIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

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

    public int GetSelectedPersonId()
    {
        return SelectedPersonId;
    }

    //バトル編成のマスから呼ばれたキャラクターセレクトパネルの表示
    public void ShowPanelSelectCharacterBattleParty(Button ButtonTmp, uint argCurrentCharacterID)
    {
        HideWorkerTabs();
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
            ShowCharacterPreview(argCurrentCharacterID);

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
        string author = "なし";
        PersonClass authorPerson = PersonClass.Find(People, character.AuthorPersonId);
        if (authorPerson != null)
            author = authorPerson.Name;
        TextSelectCharacter1.text = "HP : " + stats.HPMax + "   " + character.CharacterType;
        TextSelectCharacter2.text = "ATK : " + stats.ATK + "   DEF : " + stats.DEF;
        TextSelectCharacter3.text = "SPD : " + stats.SPD + "   LUC : " + stats.LUC + "   作者 " + author;
    }

    //キャラクターセレクトの決定ボタンが押されたら
    public void ConfirmSelectCharacter(Button ButtonTmp)
    {
        if (IsLockedOutOfProduction(CharacterIDTmp, ButtonTmp))
            return;

        Sprite handed = _previewSprite;
        if (handed != null && CharacterIDTmp != 0 && ImageSelectCharacter != null && ImageSelectCharacter.sprite == handed)
        {
            _previewSprite = null;
            ControllerProduction.TakeButtonSprite(ButtonTmp, CharacterIDTmp, handed);
        }
        else
        {
            ControllerProduction.ReleaseButtonCharacterSprite(ButtonTmp);
            if (ImageSelectCharacter != null)
                ButtonTmp.image.sprite = ImageSelectCharacter.sprite;
        }
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
            int ProductionPixelIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

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
            int ProductionCharacterIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

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
            int ProductionCharacterIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

            CharactersIDProducedCharacter[ProductionCharacterIndex] = CharacterIDTmp;
        }


        NotShowPanelSelectCharacter();
    }

    void ReleaseWorker(uint characterId)
    {
        if (characterId == 0)
            return;
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, characterId);
        if (character != null)
            character.Whereabouts = Place.None;
    }

    //キャラクターセレクトの外すボタンが押されたら
    public void RemoveCharacterSelect(Button ButtonTmp)
    {
        ControllerProduction.ReleaseButtonCharacterSprite(ButtonTmp);
        ControllerProduction.HidePersonPortrait(ButtonTmp);
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

                ReleaseWorker(CharactersIDHelpProductionR[HelpProductionIndex]);
                CharactersIDHelpProductionR[HelpProductionIndex] = 0;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
            {
                ButtonTmp.image.color = new Color(0.0f, 50 / 255f, 0.0f, 1.0f);

                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                ReleaseWorker(CharactersIDHelpProductionG[HelpProductionIndex]);
                CharactersIDHelpProductionG[HelpProductionIndex] = 0;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
            {
                ButtonTmp.image.color = new Color(0.0f, 0.0f, 50 / 255f, 1.0f);

                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                ReleaseWorker(CharactersIDHelpProductionB[HelpProductionIndex]);
                CharactersIDHelpProductionB[HelpProductionIndex] = 0;
            }
        }
        else
        //Pixel生産のキャラクターの管理
        if (ButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ProductionPixelIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

            ReleaseWorker(CharactersIDProductionPixel[ProductionPixelIndex]);
            CharactersIDProductionPixel[ProductionPixelIndex] = 0;

        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProductionCharacter"))
        {
            int ProductionCharacterIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

            ReleaseWorker(CharactersIDProductionCharacter[ProductionCharacterIndex]);
            CharactersIDProductionCharacter[ProductionCharacterIndex] = 0;
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            int ProductionCharacterIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

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
                    ShowCharacterPreview(CharactersIDHelpProductionR[HelpProductionIndex]);
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
                    ShowCharacterPreview(CharactersIDHelpProductionG[HelpProductionIndex]);
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
                    ShowCharacterPreview(CharactersIDHelpProductionB[HelpProductionIndex]);
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

            int ProductionPixelIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);

            if (CharactersIDProductionPixel[ProductionPixelIndex] != 0)
            {
                //選択キャラクターの画像を表示
                ShowCharacterPreview(CharactersIDProductionPixel[ProductionPixelIndex]);
                //選択キャラクターのステータスを表示
                TextSelectCharacter1.text = "      SPD       : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).Stats[0].SPD;
                TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                TextSelectCharacter2.text = "CreatePixel : " + ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).GetCreatePixels((ushort)CharacterClass.ColorByte(ColorProductionPixel[ProductionPixelIndex].r), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ProductionPixelIndex].g), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ProductionPixelIndex].b));
                TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                TextSelectCharacter3.text = "  Pixel/sec   : " + GetCreatePixelTime(ColorProductionPixel[ProductionPixelIndex], CharactersIDProductionPixel[ProductionPixelIndex])
                                                               * ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProductionPixel[ProductionPixelIndex])).GetCreatePixels((ushort)CharacterClass.ColorByte(ColorProductionPixel[ProductionPixelIndex].r), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ProductionPixelIndex].g), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ProductionPixelIndex].b));
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

            int ProductionCharacterIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);
            if (CharactersIDProductionCharacter[ProductionCharacterIndex] != 0)
            {
                //選択キャラクターの画像を表示
                ShowCharacterPreview(CharactersIDProductionCharacter[ProductionCharacterIndex]);

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

            int ProductionCharacterIndex = ControllerProduction.ProductionSlotIndex(ButtonTmp.name);
            if (CharactersIDProducedCharacter[ProductionCharacterIndex] != 0)
            {
                //選択キャラクターの画像を表示
                ShowCharacterPreview(CharactersIDProducedCharacter[ProductionCharacterIndex]);

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

        SelectedPersonId = 0;
        if (IsProductionAssignmentButton(ButtonTmp))
        {
            EnsureWorkerTabs(ButtonTmp);
            int assigned = AssignedPerson(ButtonTmp);
            if (assigned != 0)
                ShowAssignedPerson(assigned, ButtonTmp);
        }
        else
            HideWorkerTabs();
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
        bool createdSprite = false;

        // パスがちゃんと入っているかチェック
        if (!string.IsNullOrWhiteSpace(ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).ImagePath))
        {
            // パスがあるときだけ画像を読み込む
            Texture2D tex = ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).ImageTexture2D;
            if (tex != null)
            {
                characterSprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Size, ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Size), new Vector2(0.5f, 0.5f));
                ControllerProduction.MarkRuntime(characterSprite);
                createdSprite = true;
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
        if (createdSprite)
            _listSprites.Add(characterSprite);




        //textの指定
        if (argButtonTmp.name.Contains("RProductionHelpCharacter") ||
            argButtonTmp.name.Contains("GProductionHelpCharacter") ||
            argButtonTmp.name.Contains("BProductionHelpCharacter"))
        {
            if (argButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter"))
            {
                GameObjectCharacterButton.GetComponentInChildren<Text>().text = "CreateR/sec : " + UiNumber.Format(ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Stats[0].RCreates);
                GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            }
            else
            if (argButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
            {
                GameObjectCharacterButton.GetComponentInChildren<Text>().text = "CreateG/sec : " + UiNumber.Format(ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Stats[0].GCreates);
                GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
            }
            else
            if (argButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
            {
                GameObjectCharacterButton.GetComponentInChildren<Text>().text = "CreateB/sec : " + UiNumber.Format(ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).Stats[0].BCreates);
                GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 1.0f, 1.0f);
            }
        }
        else
        if (argButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ColorProductionPixelIndex = ControllerProduction.ProductionSlotIndex(argButtonTmp.name);

            GameObjectCharacterButton.GetComponentInChildren<Text>().text = "Pixel/sec : " + GetCreatePixelTime(ColorProductionPixel[ColorProductionPixelIndex], (uint)argCharacterIndex)
                                                               * ImportedCharacters.FindAny(CharactersAll, (uint)(argCharacterIndex)).GetCreatePixels((ushort)CharacterClass.ColorByte(ColorProductionPixel[ColorProductionPixelIndex].r), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ColorProductionPixelIndex].g), (ushort)CharacterClass.ColorByte(ColorProductionPixel[ColorProductionPixelIndex].b));
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
            return CharactersIDProductionPixel[ControllerProduction.ProductionSlotIndex(name)] == characterId;
        if (name.Contains("ButtonCharacterProductionCharacter"))
            return CharactersIDProductionCharacter[ControllerProduction.ProductionSlotIndex(name)] == characterId;
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
        ControllerProduction.ForEachNamedButton(buttonName, button =>
        {
            if (button.image == null)
                return;
            button.image.sprite = null;
            button.image.color = emptyColor;
            ControllerProduction.HidePersonPortrait(button);
            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = "+";
        });
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
        SelectedPersonId = 0;
        HideWorkerTabs();

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
            Destroy(child.gameObject);
        ReleaseListSprites();

        ReleasePreviewSprite();
        if (ImageSelectCharacter != null)
        {
            ImageSelectCharacter.sprite = null;
            ImageSelectCharacter.color = Color.white;
        }
    }


    int GetCreatePixelTime(Color argColor, uint argCharacterID)
    {
        return GetCreatePixelTimeRGB((ushort)CharacterClass.ColorByte(argColor.r), argCharacterID) +
               GetCreatePixelTimeRGB((ushort)CharacterClass.ColorByte(argColor.g), argCharacterID) +
               GetCreatePixelTimeRGB((ushort)CharacterClass.ColorByte(argColor.b), argCharacterID);
    }
    int GetCreatePixelTimeRGB(ushort argRGB, uint argCharacterID)
    {
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, argCharacterID);
        if (character == null || character.Stats == null || character.Stats[0] == null || character.Stats[0].SPD < 1)
            return 0;
        return (int)(Mathf.Ceil((argRGB + 1) / (float)(character.Stats[0].SPD)));
    }

    public void ShowPanelSelectBattlePerson(Button source, int currentPersonId)
    {
        SelectedPersonId = 0;
        CharacterIDTmp = 0;
        HideWorkerTabs();
        ClearCharacterList();
        TextSelectCharacter1.text = "人";
        TextSelectCharacter2.text = "";
        TextSelectCharacter3.text = "";
        TextSelectCharacter1.color = Color.black;
        SetConfirmEnabled(false);
        if (currentPersonId != 0)
            ShowAssignedPerson(currentPersonId, source);
        for (int id = 1; id <= PersonIds.Count; id++)
            CreatePersonButton(id, source);
        SetRemoveEnabled(currentPersonId != 0);
    }

    void EnsureWorkerTabs(Button source)
    {
        Transform content = FindCharacterList();
        if (content == null)
            return;
        Transform panel = content;
        while (panel != null && panel.name != "PanelSelectCharacter")
            panel = panel.parent;
        if (panel == null)
            panel = content.parent;
        if (!_workerTabsReady)
            FindOrCreateWorkerTabs(panel);
        if (!_workerTabsReady)
            return;
        _showingCharacters = true;
        if (WorkerTabs != null)
            WorkerTabs.SetActive(true);
        SetWorkerToggleShown(true);
        SetSceneWorkerTabsActive(true);
        Button captured = source;
        if (_workerToggle != null)
        {
            ForEachWorkerToggle(button =>
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ToggleWorkerKind(captured));
            });
            return;
        }
        _characterTab.onClick.RemoveAllListeners();
        _personTab.onClick.RemoveAllListeners();
        _characterTab.onClick.AddListener(() => ShowCharacterTab(captured));
        _personTab.onClick.AddListener(() => ShowPersonTab(captured));
        PaintWorkerTabs(true);
    }

    void ToggleWorkerKind(Button source)
    {
        if (_showingCharacters)
            ShowPersonTab(source);
        else
            ShowCharacterTab(source);
    }

    void FindOrCreateWorkerTabs(Transform panel)
    {
        Button toggle = FindWorkerToggle();
        if (toggle != null)
        {
            _sceneWorkerTabs = true;
            _workerToggle = toggle;
            _workerTabsReady = true;
            return;
        }

        Button characterTab = FindNamedButton(panel, "ButtonWorkerTabCharacter");
        Button personTab = FindNamedButton(panel, "ButtonWorkerTabPerson");
        if (characterTab != null && personTab != null)
        {
            _sceneWorkerTabs = true;
            _characterTab = characterTab;
            _personTab = personTab;
            _workerTabsReady = true;
            return;
        }

        _sceneWorkerTabs = false;
        if (WorkerTabs == null)
        {
            WorkerTabs = new GameObject("WorkerKindTabs", typeof(RectTransform));
            WorkerTabs.transform.SetParent(panel, false);
            RectTransform tabs = WorkerTabs.GetComponent<RectTransform>();
            tabs.anchorMin = new Vector2(0f, 1f);
            tabs.anchorMax = new Vector2(1f, 1f);
            tabs.pivot = new Vector2(0.5f, 1f);
            tabs.anchoredPosition = new Vector2(0f, -8f);
            tabs.sizeDelta = new Vector2(-24f, 64f);
            CreateTab(WorkerTabs.transform, "ButtonWorkerTabCharacter", "キャラ", new Vector2(-160f, 0f), null);
            CreateTab(WorkerTabs.transform, "ButtonWorkerTabPerson", "人", new Vector2(160f, 0f), null);
        }
        _characterTab = WorkerTabs.transform.Find("ButtonWorkerTabCharacter").GetComponent<Button>();
        _personTab = WorkerTabs.transform.Find("ButtonWorkerTabPerson").GetComponent<Button>();
        _workerTabsReady = _characterTab != null && _personTab != null;
    }

    static Button FindNamedButton(Transform panel, string name)
    {
        if (panel == null)
            return null;
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == name)
                return buttons[i];
        }
        return null;
    }

    void HideWorkerTabs()
    {
        RememberSceneWorkerTabs();
        if (WorkerTabs != null)
            WorkerTabs.SetActive(false);
        SetWorkerToggleShown(false);
        SetSceneWorkerTabsActive(false);
    }

    void RememberSceneWorkerTabs()
    {
        if (_workerToggle == null)
            _workerToggle = FindWorkerToggle();
        if (_workerToggle != null)
        {
            _sceneWorkerTabs = true;
            _workerTabsReady = true;
        }
        if (_sceneWorkerTabs || _characterTab != null)
            return;
        Transform content = FindCharacterList();
        if (content == null)
            return;
        Transform panel = content;
        while (panel != null && panel.name != "PanelSelectCharacter")
            panel = panel.parent;
        if (panel == null)
            panel = content.parent;
        Button characterTab = FindNamedButton(panel, "ButtonWorkerTabCharacter");
        Button personTab = FindNamedButton(panel, "ButtonWorkerTabPerson");
        if (characterTab == null || personTab == null)
            return;
        _sceneWorkerTabs = true;
        _characterTab = characterTab;
        _personTab = personTab;
        _workerTabsReady = true;
    }

    static Button FindWorkerToggle()
    {
        Button found = null;
        ForEachWorkerToggle(button =>
        {
            if (found == null)
                found = button;
        });
        return found;
    }

    static bool IsWorkerToggle(string name)
    {
        return name == "ButtonWorkerTabCharacterPerson"
            || (name != null && name.StartsWith("ButtonWorkerTabCharacterPerson ", System.StringComparison.Ordinal));
    }

    static void ForEachWorkerToggle(System.Action<Button> action)
    {
        if (action == null)
            return;
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !IsWorkerToggle(button.name))
                continue;
            if (!button.gameObject.scene.IsValid())
                continue;
            action(button);
        }
    }

    static void SetWorkerToggleShown(bool shown)
    {
        ForEachWorkerToggle(button =>
        {
            if (button.gameObject.activeSelf != shown)
                button.gameObject.SetActive(shown);
        });
    }

    void SetSceneWorkerTabsActive(bool active)
    {
        if (!_sceneWorkerTabs)
            return;
        if (_workerToggle != null)
            _workerToggle.gameObject.SetActive(active);
        if (_characterTab != null)
            _characterTab.gameObject.SetActive(active);
        if (_personTab != null)
            _personTab.gameObject.SetActive(active);
    }

    void PaintWorkerTabs(bool characterSelected)
    {
        PaintWorkerTab(_characterTab, characterSelected);
        PaintWorkerTab(_personTab, !characterSelected);
    }

    static void PaintWorkerTab(Button button, bool selected)
    {
        if (button == null)
            return;
        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected
                ? new Color(0.25f, 0.48f, 0.75f, 1f)
                : new Color(0.75f, 0.78f, 0.84f, 1f);
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.color = selected ? Color.white : new Color(0.1f, 0.1f, 0.12f, 1f);
    }

    void ShowCharacterTab(Button source)
    {
        ClearCharacterList();
        for (int indexCharacter = 0; indexCharacter < Constants.CHARACTERS_ALL_NUM + 1; indexCharacter++)
        {
            CharacterClass character = ImportedCharacters.FindAny(CharactersAll, (uint)indexCharacter);
            if (character != null && character.OwnedNumMax != 0)
                CreateCharacterButton(indexCharacter, source, true);
        }
        AddImportedCharacterButtons(source, true);
        _showingCharacters = true;
        PaintWorkerTabs(true);
    }

    void ShowPersonTab(Button source)
    {
        ClearCharacterList();
        for (int id = 1; id <= PersonIds.Count; id++)
            CreatePersonButton(id, source);
        _showingCharacters = false;
        PaintWorkerTabs(false);
    }

    void CreatePersonButton(int personId, Button source)
    {
        PersonClass person = PersonClass.Find(People, personId);
        Transform list = FindCharacterList();
        if (person == null || list == null)
            return;
        GameObject buttonObject = Instantiate((GameObject)Resources.Load("PrefabButtonCharacterImage"), list) as GameObject;
        Button button = buttonObject.GetComponent<Button>();
        button.interactable = true;
        ControllerProduction.PaintPerson(button, person);
        Text label = buttonObject.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = person.Name + "\n" + PersonChoiceText(person, source);
            label.alignment = TextAnchor.LowerCenter;
            label.color = Color.white;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.42f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Outline outline = label.GetComponent<Outline>();
            if (outline == null)
                outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
        if (IsPersonLockedOutOfProduction(personId, source))
        {
            GrayOutCharacterButton(buttonObject);
            return;
        }
        int captured = personId;
        button.onClick.AddListener(() => SelectPerson(captured, source));
    }

    bool IsPersonLockedOutOfProduction(int personId, Button source)
    {
        if (!IsProductionAssignmentButton(source))
            return false;
        return PersonSortieCheck != null && PersonSortieCheck(personId);
    }

    void SelectPerson(int personId, Button source)
    {
        if (IsPersonLockedOutOfProduction(personId, source))
            return;
        SelectedPersonId = personId;
        CharacterIDTmp = 0;
        ShowAssignedPerson(personId, source);
        SetConfirmEnabled(true);
    }

    void ShowAssignedPerson(int personId, Button source)
    {
        PersonClass person = PersonClass.Find(People, personId);
        if (person == null)
            return;
        if (ImageSelectCharacter != null)
        {
            ReleasePreviewSprite();
            Sprite portrait = PersonPortraits.SpriteOf(person.Id);
            ImageSelectCharacter.preserveAspect = true;
            if (portrait != null)
            {
                ImageSelectCharacter.sprite = portrait;
                ImageSelectCharacter.color = Color.white;
            }
            else
            {
                ImageSelectCharacter.sprite = null;
                ImageSelectCharacter.color = ControllerProduction.PersonColor(person.Id);
            }
        }
        TextSelectCharacter1.color = Color.black;
        TextSelectCharacter2.color = Color.black;
        TextSelectCharacter3.color = Color.black;
        TextSelectCharacter1.text = person.Name;
        TextSelectCharacter2.text = "得意 " + person.SpecialtyText();
        TextSelectCharacter3.text = PersonChoiceText(person, source);
        SetRemoveEnabled(true);
    }

    string PersonChoiceText(PersonClass person, Button source)
    {
        if (PersonBalance == null || person == null)
            return "";
        if (source != null && source.name.Contains("RProductionHelpCharacter"))
            return "R " + UiNumber.Format(person.RgbAmount(0, PersonBalance)) + "/秒";
        if (source != null && source.name.Contains("GProductionHelpCharacter"))
            return "G " + UiNumber.Format(person.RgbAmount(1, PersonBalance)) + "/秒";
        if (source != null && source.name.Contains("BProductionHelpCharacter"))
            return "B " + UiNumber.Format(person.RgbAmount(2, PersonBalance)) + "/秒";
        if (source != null && source.name.Contains("ButtonPixelProductionCharacter"))
        {
            int index = ControllerProduction.ProductionSlotIndex(source.name);
            int r = CharacterClass.ColorByte(ColorProductionPixel[index].r);
            int g = CharacterClass.ColorByte(ColorProductionPixel[index].g);
            int b = CharacterClass.ColorByte(ColorProductionPixel[index].b);
            return UiNumber.Format(person.PixelAmount(r, g, b, PersonBalance)) + " px";
        }
        if (source != null && source.name.Contains("ButtonCharacterProductionCharacter"))
            return UiNumber.Format(person.CharacterAmount(false, PersonBalance)) + " /秒";
        return "バトル Lv" + person.BattleLevel.ToString();
    }

    int AssignedPerson(Button source)
    {
        if (source == null)
            return 0;
        if (source.name.StartsWith("ButtonRProductionHelpCharacter"))
            return (int)PersonIdHelpProductionR[int.Parse(source.name.Substring(source.name.Length - 1, 1))];
        if (source.name.StartsWith("ButtonGProductionHelpCharacter"))
            return (int)PersonIdHelpProductionG[int.Parse(source.name.Substring(source.name.Length - 1, 1))];
        if (source.name.StartsWith("ButtonBProductionHelpCharacter"))
            return (int)PersonIdHelpProductionB[int.Parse(source.name.Substring(source.name.Length - 1, 1))];
        if (source.name.Contains("ButtonPixelProductionCharacter"))
            return (int)PersonIdProductionPixel[ControllerProduction.ProductionSlotIndex(source.name)];
        if (source.name.Contains("ButtonCharacterProductionCharacter"))
            return (int)PersonIdProductionCharacter[ControllerProduction.ProductionSlotIndex(source.name)];
        return 0;
    }

    void CreateTab(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction click)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(280f, 56f);
        rect.anchoredPosition = position;
        buttonObject.GetComponent<Image>().color = new Color(0.75f, 0.78f, 0.84f, 1f);
        if (click != null)
            buttonObject.GetComponent<Button>().onClick.AddListener(click);
        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 28;
        text.color = new Color(0.1f, 0.1f, 0.12f, 1f);
    }

    Transform FindCharacterList()
    {
        GameObject[] tagged = GameObject.FindGameObjectsWithTag("SelectCharacter");
        for (int i = 0; i < tagged.Length; i++)
        {
            if (tagged[i].name == "ContentCharacterList")
                return tagged[i].transform;
        }
        return null;
    }

    void ClearCharacterList()
    {
        Transform list = FindCharacterList();
        if (list == null)
            return;
        for (int i = list.childCount - 1; i >= 0; i--)
            Destroy(list.GetChild(i).gameObject);
        ReleaseListSprites();
    }

    void ShowCharacterPreview(uint id)
    {
        if (ImageSelectCharacter == null)
            return;
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, id);
        if (character == null || character.ImageTexture2D == null)
        {
            ReleasePreviewSprite();
            return;
        }
        ReleasePreviewSprite();
        _previewSprite = Sprite.Create(character.ImageTexture2D, new Rect(0f, 0f, character.Size, character.Size), new Vector2(0.5f, 0.5f));
        ControllerProduction.MarkRuntime(_previewSprite);
        ImageSelectCharacter.sprite = _previewSprite;
        ImageSelectCharacter.color = Color.white;
    }

    void ReleasePreviewSprite()
    {
        if (_previewSprite == null)
            return;
        if (ImageSelectCharacter != null && ImageSelectCharacter.sprite == _previewSprite)
            ImageSelectCharacter.sprite = null;
        ControllerProduction.DestroyRuntime(_previewSprite);
        _previewSprite = null;
    }

    void ReleaseListSprites()
    {
        for (int i = 0; i < _listSprites.Count; i++)
        {
            if (_listSprites[i] != null)
                ControllerProduction.DestroyRuntime(_listSprites[i]);
        }
        _listSprites.Clear();
    }

    void SetConfirmEnabled(bool enabled)
    {
        SetNamedButtons(enabled, "ButtonConfirmSelectLeft", "ButtonConfirmSelectRight");
    }

    void SetRemoveEnabled(bool enabled)
    {
        SetNamedButtons(enabled, "ButtonRemoveLeft", "ButtonRemoveRight");
    }

    void SetNamedButtons(bool enabled, string left, string right)
    {
        GameObject[] tagged = GameObject.FindGameObjectsWithTag("SelectCharacter");
        for (int i = 0; i < tagged.Length; i++)
        {
            if (tagged[i].name != left && tagged[i].name != right)
                continue;
            Button button = tagged[i].GetComponent<Button>();
            if (button != null)
                button.interactable = enabled;
        }
    }
}
