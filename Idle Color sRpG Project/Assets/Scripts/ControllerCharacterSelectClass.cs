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
                           ref uint[] argCharactersIDProductionPixel)
                           //ref Image argImageSelectCharacter,
                           //ref Text argTextSelectCharacter1, ref Text argTextSelectCharacter2, ref Text argTextSelectCharacter3)
    {
        CharactersAll = argCharactersAll;
        ColorProductionPixel = argColorProductionPixel;
        CharactersIDHelpProductionR = argCharactersIDHelpProductionR;
        CharactersIDHelpProductionG = argCharactersIDHelpProductionG;
        CharactersIDHelpProductionB = argCharactersIDHelpProductionB;
        CharactersIDProductionPixel = argCharactersIDProductionPixel;
        //ImageSelectCharacter = argImageSelectCharacter;
        //TextSelectCharacter1 = argTextSelectCharacter1;
        //TextSelectCharacter2 = argTextSelectCharacter2;
        //TextSelectCharacter3 = argTextSelectCharacter3;
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
        Debug.Log("SelectCharacterID : " + argCharacterID);
        ImageSelectCharacter.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[argCharacterID].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[argCharacterID].Size, CharactersAll[argCharacterID].Size), new Vector2(0.5f, 0.5f));
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
            TextSelectCharacter1.text = "CreateR : " + CharactersAll[argCharacterID].Stats[0].RCreates;
            TextSelectCharacter1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "CreateG : " + CharactersAll[argCharacterID].Stats[0].GCreates;
            TextSelectCharacter2.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "CreateB : " + CharactersAll[argCharacterID].Stats[0].BCreates;
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 1.0f, 1.0f);
        }
        else
        if (ButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ColorProductionPixelIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            //選択キャラクターのステータスを表示
            TextSelectCharacter1.text = "      SPD       : " + CharactersAll[argCharacterID].Stats[0].SPD;
            TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter2.text = "CreatePixel : " + CharactersAll[argCharacterID].GetCreatePixels((ushort)(ColorProductionPixel[ColorProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].b * 255));
            TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            TextSelectCharacter3.text = "  Pixel/sec   : " + GetCreatePixelTime(ColorProductionPixel[ColorProductionPixelIndex], argCharacterID)
                                                           * CharactersAll[argCharacterID].GetCreatePixels((ushort)(ColorProductionPixel[ColorProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].b * 255));
            TextSelectCharacter3.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        if (ButtonTmp.name.Contains("ButtonCharacterColor"))
        {
            TextSelectCharacter1.text = "";
            TextSelectCharacter2.text = "";
            TextSelectCharacter3.text = "";
        }
    }

    //キャラクターセレクトの決定ボタンが押されたら
    public void ConfirmSelectCharacter(Button ButtonTmp)
    {
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

                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionR[HelpProductionIndex]].Whereabouts = Place.None;
                CharactersIDHelpProductionR[HelpProductionIndex] = CharacterIDTmp;

                //居場所変更
                CharactersAll[CharacterIDTmp].Whereabouts = Place.CreateR;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
            {
                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionG[HelpProductionIndex]].Whereabouts = Place.None;
                CharactersIDHelpProductionG[HelpProductionIndex] = CharacterIDTmp;

                //居場所変更
                CharactersAll[CharacterIDTmp].Whereabouts = Place.CreateG;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
            {
                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionB[HelpProductionIndex]].Whereabouts = Place.None;
                CharactersIDHelpProductionB[HelpProductionIndex] = CharacterIDTmp;

                //居場所変更
                CharactersAll[CharacterIDTmp].Whereabouts = Place.CreateB;
            }
        }
        else
        //Pixel生産のキャラクターの管理
        if(ButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ProductionPixelIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            //前に設定されていたキャラの居場所変更
            CharactersAll[CharactersIDProductionPixel[ProductionPixelIndex]].Whereabouts = Place.None;


            CharactersIDProductionPixel[ProductionPixelIndex] = CharacterIDTmp;

            //居場所変更
            CharactersAll[CharacterIDTmp].Whereabouts = Place.CreatePixel;
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
                CharactersAll[CharactersIDHelpProductionR[HelpProductionIndex]].Whereabouts = Place.None;

                CharactersIDHelpProductionR[HelpProductionIndex] = 0;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
            {
                ButtonTmp.image.color = new Color(0.0f, 50 / 255f, 0.0f, 1.0f);

                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionG[HelpProductionIndex]].Whereabouts = Place.None;

                CharactersIDHelpProductionG[HelpProductionIndex] = 0;
            }
            else
            if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
            {
                ButtonTmp.image.color = new Color(0.0f, 0.0f, 50 / 255f, 1.0f);

                int HelpProductionIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 1, 1));

                //前に設定されていたキャラの居場所変更
                CharactersAll[CharactersIDHelpProductionB[HelpProductionIndex]].Whereabouts = Place.None;

                CharactersIDHelpProductionB[HelpProductionIndex] = 0;
            }
        }
        else
        //Pixel生産のキャラクターの管理
        if (ButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ProductionPixelIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

            //前に設定されていたキャラの居場所変更
            CharactersAll[CharactersIDProductionPixel[ProductionPixelIndex]].Whereabouts = Place.None;

            CharactersIDProductionPixel[ProductionPixelIndex] = 0;

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
                    ImageSelectCharacter.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionR[HelpProductionIndex]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[HelpProductionIndex]].Size, CharactersAll[CharactersIDHelpProductionR[HelpProductionIndex]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacter1.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionR[HelpProductionIndex]].Stats[0].RCreates;
                    TextSelectCharacter1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    TextSelectCharacter2.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionR[HelpProductionIndex]].Stats[0].GCreates;
                    TextSelectCharacter2.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    TextSelectCharacter3.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionR[HelpProductionIndex]].Stats[0].BCreates;
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
                    ImageSelectCharacter.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionG[HelpProductionIndex]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[HelpProductionIndex]].Size, CharactersAll[CharactersIDHelpProductionG[HelpProductionIndex]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacter1.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionG[HelpProductionIndex]].Stats[0].RCreates;
                    TextSelectCharacter1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    TextSelectCharacter2.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionG[HelpProductionIndex]].Stats[0].GCreates;
                    TextSelectCharacter2.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    TextSelectCharacter3.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionG[HelpProductionIndex]].Stats[0].BCreates;
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
                    ImageSelectCharacter.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionB[HelpProductionIndex]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[HelpProductionIndex]].Size, CharactersAll[CharactersIDHelpProductionB[HelpProductionIndex]].Size), new Vector2(0.5f, 0.5f));
                    //選択キャラクターのステータスを表示
                    TextSelectCharacter1.text = "CreateR : " + CharactersAll[CharactersIDHelpProductionB[HelpProductionIndex]].Stats[0].RCreates;
                    TextSelectCharacter1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    TextSelectCharacter2.text = "CreateG : " + CharactersAll[CharactersIDHelpProductionB[HelpProductionIndex]].Stats[0].GCreates;
                    TextSelectCharacter2.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    TextSelectCharacter3.text = "CreateB : " + CharactersAll[CharactersIDHelpProductionB[HelpProductionIndex]].Stats[0].BCreates;
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
                ImageSelectCharacter.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDProductionPixel[ProductionPixelIndex]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDProductionPixel[ProductionPixelIndex]].Size, CharactersAll[CharactersIDProductionPixel[ProductionPixelIndex]].Size), new Vector2(0.5f, 0.5f));
                //選択キャラクターのステータスを表示
                TextSelectCharacter1.text = "      SPD       : " + CharactersAll[CharactersIDProductionPixel[ProductionPixelIndex]].Stats[0].SPD;
                TextSelectCharacter1.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                TextSelectCharacter2.text = "CreatePixel : " + CharactersAll[CharactersIDProductionPixel[ProductionPixelIndex]].GetCreatePixels((ushort)(ColorProductionPixel[ProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ProductionPixelIndex].b * 255));
                TextSelectCharacter2.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                TextSelectCharacter3.text = "  Pixel/sec   : " + GetCreatePixelTime(ColorProductionPixel[ProductionPixelIndex], CharactersIDProductionPixel[ProductionPixelIndex])
                                                               * CharactersAll[CharactersIDProductionPixel[ProductionPixelIndex]].GetCreatePixels((ushort)(ColorProductionPixel[ProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ProductionPixelIndex].b * 255));
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


        //キャラクターボタン生成
        GameObject[] ArrayButtonCharacter = new GameObject[Constants.CHARACTERS_ALL_NUM + 1];
        for (int indexCharacter = 0; indexCharacter < Constants.CHARACTERS_ALL_NUM + 1; indexCharacter++)
        {
            if (CharactersAll[indexCharacter].OwnedNumCur != 0 && CharactersAll[indexCharacter].Whereabouts == Place.None)
            {
                //プレハブのインスタンス化
                ArrayButtonCharacter[indexCharacter] = Instantiate((GameObject)Resources.Load("PrefabButtonCharacterImage"), GameObject.Find("ContentCharacterList").transform) as GameObject;

                //spriteの指定
                ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Image>().sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[indexCharacter].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[indexCharacter].Size, CharactersAll[indexCharacter].Size), new Vector2(0.5f, 0.5f));

                //textの指定
                if (ButtonTmp.name.Contains("RProductionHelpCharacter") ||
                    ButtonTmp.name.Contains("GProductionHelpCharacter") ||
                    ButtonTmp.name.Contains("BProductionHelpCharacter"))
                {
                    if (ButtonTmp.name.StartsWith("ButtonRProductionHelpCharacter"))
                    {
                        ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Text>().text = "CreateR/sec : " + CharactersAll[indexCharacter].Stats[0].RCreates.ToString();
                        ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Text>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    }
                    else
                    if (ButtonTmp.name.StartsWith("ButtonGProductionHelpCharacter"))
                    {
                        ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Text>().text = "CreateG/sec : " + CharactersAll[indexCharacter].Stats[0].GCreates.ToString();
                        ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Text>().color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    }
                    else
                    if (ButtonTmp.name.StartsWith("ButtonBProductionHelpCharacter"))
                    {
                        ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Text>().text = "CreateB/sec : " + CharactersAll[indexCharacter].Stats[0].BCreates.ToString();
                        ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 1.0f, 1.0f);
                    }
                }
                else
                if (ButtonTmp.name.Contains("ButtonPixelProductionCharacter"))
                {
                    int ColorProductionPixelIndex = int.Parse(ButtonTmp.name.Substring(ButtonTmp.name.Length - 2, 2));

                    ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Text>().text = "Pixel/sec : " + GetCreatePixelTime(ColorProductionPixel[ColorProductionPixelIndex], (uint)indexCharacter)
                                                                       * CharactersAll[indexCharacter].GetCreatePixels((ushort)(ColorProductionPixel[ColorProductionPixelIndex].r * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].g * 255), (ushort)(ColorProductionPixel[ColorProductionPixelIndex].b * 255));
                    ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                }
                else
                if (ButtonTmp.name.Contains("ButtonCharacterColor"))
                {
                    ArrayButtonCharacter[indexCharacter].GetComponentInChildren<Text>().text = "";
                }


                //クリックイベントを追加
                uint CharacterID = (uint)(indexCharacter);//匿名メソッドの外部変数のキャプチャの関係で、別の変数に代入
                ArrayButtonCharacter[indexCharacter].GetComponent<Button>().onClick.AddListener(() => SelectCharacterCharacter(CharacterID, ButtonTmp));
            }
        }
    }

    //キャラクターセレクトパネルの非表示
    public void NotShowPanelSelectCharacter()
    {
        //どのキャラクターが選ばれたか削除
        CharacterIDTmp = 0;

        //キャラクターボタンの削除
        foreach (Transform child in GameObject.Find("ContentCharacterList").transform)
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
        return (int)(Mathf.Ceil((argRGB + 1) / (float)(CharactersAll[argCharacterID].Stats[0].SPD)));
    }
}
