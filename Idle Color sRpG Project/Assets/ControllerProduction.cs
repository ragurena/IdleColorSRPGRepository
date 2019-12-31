using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;

//生産画面のコントロール
public class ControllerProduction : MonoBehaviour
{
    ModelProduction ModelProduction;
    CharacterClass Character;

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

    [SerializeField] Text TextIncreaseValueR;
    [SerializeField] Text TextIncreaseValueG;
    [SerializeField] Text TextIncreaseValueB;

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


    //モンスター
    [SerializeField] Image ImageRedSlime8;


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("ControllerProduction Start");

        ModelProduction = GetComponent<ModelProduction>();
        Character = GetComponent<CharacterClass>();

        //UIの更新
        UpdateProductionScene();

        //キャラ生成
        Character.MakeCharacter(Resources.Load("Monster/RedSlime8", typeof(Texture2D)) as Texture2D, 1, "LittleRedSlime");
        Character.MakeCharacter(Resources.Load("Monster/RBlackCat8", typeof(Texture2D)) as Texture2D, 4, "LittleRBlackCat");

        /////////////////////////////////////////////////
        //テスト
        //モンスター画像を触る
        //Texture2D Texture2DRedSlime = ToTexture2D(ImageRedSlime8.mainTexture);
        Texture2D Texture2DRedSlime = Resources.Load("Monster/RedSlime8", typeof(Texture2D)) as Texture2D;//ToTexture2D(ImageRedSlime8.mainTexture);
        //Texture2D Texture2DRedSlime = ImageRedSlime8.//ToTexture2D(ImageRedSlime8.mainTexture);

        Debug.Log("Texture2D : " + Texture2DRedSlime);
        Debug.Log("Texture2D.GetType() : " + Texture2DRedSlime.GetType());
        Debug.Log("Texture2D Height : " + Texture2DRedSlime.height);
        Debug.Log("Texture2D Width : " + Texture2DRedSlime.width);



        Mat MatRedSlime = OpenCvSharp.Unity.TextureToMat(Texture2DRedSlime);
        //Mat MatRedSlime = new Mat(8, 8, MatType.CV_8UC4);
        Debug.Log("Mat : " + MatRedSlime);
        //Mat MatRedSlime = OpenCvSharp.Unity.TextureToMat(Texture2DRedSlime);
        //OpenCvSharp.Unity.TextureToMat
        Debug.Log("Mat : " + MatRedSlime);
        Debug.Log("Mat Height : " + MatRedSlime.Height);
        Debug.Log("Mat Width : " + MatRedSlime.Width);

        Debug.Log("Mat(0,0) : " + MatRedSlime.At<Vec3b>(0, 0)[0] + " , " + MatRedSlime.At<Vec3b>(0, 0)[1] + " , " + MatRedSlime.At<Vec3b>(0, 0)[2]);
        Debug.Log("Mat(0,1) : " + MatRedSlime.At<Vec3b>(0, 1)[0] + " , " + MatRedSlime.At<Vec3b>(0, 1)[1] + " , " + MatRedSlime.At<Vec3b>(0, 1)[2]);
        Debug.Log("Mat(0,2) : " + MatRedSlime.At<Vec3b>(0, 2)[0] + " , " + MatRedSlime.At<Vec3b>(0, 2)[1] + " , " + MatRedSlime.At<Vec3b>(0, 2)[2]);
        Debug.Log("Mat(0,3) : " + MatRedSlime.At<Vec3b>(0, 3)[0] + " , " + MatRedSlime.At<Vec3b>(0, 3)[1] + " , " + MatRedSlime.At<Vec3b>(0, 3)[2]);
        Debug.Log("Mat(1,0) : " + MatRedSlime.At<Vec3b>(1, 0)[0] + " , " + MatRedSlime.At<Vec3b>(1, 0)[1] + " , " + MatRedSlime.At<Vec3b>(1, 0)[2]);
        Debug.Log("Mat(1,1) : " + MatRedSlime.At<Vec3b>(1, 1)[0] + " , " + MatRedSlime.At<Vec3b>(1, 1)[1] + " , " + MatRedSlime.At<Vec3b>(1, 1)[2]);
        Debug.Log("Mat(1,2) : " + MatRedSlime.At<Vec3b>(1, 2)[0] + " , " + MatRedSlime.At<Vec3b>(1, 2)[1] + " , " + MatRedSlime.At<Vec3b>(1, 2)[2]);
        Debug.Log("Mat(1,3) : " + MatRedSlime.At<Vec3b>(1, 3)[0] + " , " + MatRedSlime.At<Vec3b>(1, 3)[1] + " , " + MatRedSlime.At<Vec3b>(1, 3)[2]);
        Debug.Log("MatType : " + MatRedSlime.Type());

        //Cv2.Reduceはintを扱えないっぽい？
        //Mat Mat32FC3RedSlime = new Mat(MatRedSlime.Height, MatRedSlime.Width, MatType.CV_32FC3);
        //MatRedSlime.ConvertTo(Mat32FC3RedSlime, MatType.CV_32F, 1.0/255);// = MatRedSlime;
        //Debug.Log("Mat32FC3RedSlime(1,0) : " + Mat32FC3RedSlime.At<Vec3b>(1, 0)[0] + " , " + Mat32FC3RedSlime.At<Vec3b>(1, 0)[1] + " , " + Mat32FC3RedSlime.At<Vec3b>(1, 0)[2]);
        //Debug.Log("Mat32FC3RedSlime(1,1) : " + Mat32FC3RedSlime.At<Vec3b>(1, 1)[0] + " , " + Mat32FC3RedSlime.At<Vec3b>(1, 1)[1] + " , " + Mat32FC3RedSlime.At<Vec3b>(1, 1)[2]);
        //Debug.Log("Mat32FC3RedSlime(1,2) : " + Mat32FC3RedSlime.At<Vec3b>(1, 2)[0] + " , " + Mat32FC3RedSlime.At<Vec3b>(1, 2)[1] + " , " + Mat32FC3RedSlime.At<Vec3b>(1, 2)[2]);
        //Debug.Log("Mat32FC3RedSlime(1,3) : " + Mat32FC3RedSlime.At<Vec3b>(1, 3)[0] + " , " + Mat32FC3RedSlime.At<Vec3b>(1, 3)[1] + " , " + Mat32FC3RedSlime.At<Vec3b>(1, 3)[2]);
        //Mat MatSumRedSlime = new Mat(3,1,MatType.CV_32FC3);
        //Cv2.Reduce(Mat32FC3RedSlime, MatSumRedSlime, ReduceDimension.Row, ReduceTypes.Sum,-1);
        //Debug.Log("MatSumRedSlime(0,0) : " + MatSumRedSlime.At<Vec3b>(0, 0)[0] + " , " + MatSumRedSlime.At<Vec3b>(0, 0)[1] + " , " + MatSumRedSlime.At<Vec3b>(0, 0)[2]);
        //Debug.Log("MatSumRedSlime(0,1) : " + MatSumRedSlime.At<Vec3b>(0, 1)[0] + " , " + MatSumRedSlime.At<Vec3b>(0, 1)[1] + " , " + MatSumRedSlime.At<Vec3b>(0, 1)[2]);
        //Debug.Log("MatSumRedSlime(0,2) : " + MatSumRedSlime.At<Vec3b>(0, 2)[0] + " , " + MatSumRedSlime.At<Vec3b>(0, 2)[1] + " , " + MatSumRedSlime.At<Vec3b>(0, 2)[2]);

        //モンスターの能力値
        Scalar ScalarRedSlimeAbility = Cv2.Sum(MatRedSlime);
        Debug.Log("ScalarRedSlime : " + ScalarRedSlimeAbility);



    }

    ////Texture2Dを読み込み可能にする
    ////参考：http://baba-s.hatenablog.com/entry/2018/02/26/210100
    //Texture2D ToTexture2D(Texture self)
    //{
    //    int sw = self.width;
    //    int sh = self.height;
    //    TextureFormat format = TextureFormat.RGBA32;
    //    Texture2D result = new Texture2D(sw, sh, format, false);
    //    RenderTexture currentRT = RenderTexture.active;
    //    RenderTexture rt = new RenderTexture(sw, sh, 32);
    //    Graphics.Blit(self, rt);
    //    RenderTexture.active = rt;
    //    UnityEngine.Rect source = new UnityEngine.Rect(0, 0, rt.width, rt.height);
    //    result.ReadPixels(source, 0, 0);
    //    result.Apply();
    //    RenderTexture.active = currentRT;
    //    return result;
    //}

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("ControllerProduction Update");
    }

    // ButtonRが押されたら
    public void PushButtonR()
    {
        Debug.Log("ButtonRが押された");
        ModelProduction.Increase(ref CurR, IncreaseValueR, MaxR);
        UpdateProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueR, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }
    // ButtonGが押されたら
    public void PushButtonG()
    {
        Debug.Log("ButtonGが押された");
        ModelProduction.Increase(ref CurG, IncreaseValueG, MaxG);
        UpdateProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueG, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }
    // ButtonBが押されたら
    public void PushButtonB()
    {
        Debug.Log("ButtonBが押された");
        ModelProduction.Increase(ref CurB, IncreaseValueB, MaxB);
        UpdateProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueB, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }

    //ButtonIncreaseValueRUpが押されたら
    public void PushButtonIncreaseValueRUp()
    {
        Debug.Log("ButtonIncreaseValueRUpが押された");
        ModelProduction.UpgradeValue(ref CurR, ref CostIncreaseValueRUp, ref IncreaseValueR, ModelProduction.TypeUpgrade.INCREASE);
        UpdateProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueR, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }
    //ButtonIncreaseValueGUpが押されたら
    public void PushButtonIncreaseValueGUp()
    {
        Debug.Log("ButtonIncreaseValueGUpが押された");
        ModelProduction.UpgradeValue(ref CurG, ref CostIncreaseValueGUp, ref IncreaseValueG, ModelProduction.TypeUpgrade.INCREASE);
        UpdateProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueG, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }
    //ButtonIncreaseValueBUpが押されたら
    public void PushButtonIncreaseValueBUp()
    {
        Debug.Log("ButtonIncreaseValueBUpが押された");
        ModelProduction.UpgradeValue(ref CurB, ref CostIncreaseValueBUp, ref IncreaseValueB, ModelProduction.TypeUpgrade.INCREASE);
        UpdateProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueB, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }

    //ButtonMaxRUpが押されたら
    public void PushButtonMaxRUp()
    {
        Debug.Log("ButtonMaxRUpが押された");
        ModelProduction.UpgradeValue(ref CurR, ref CostMaxRUp, ref MaxR, ModelProduction.TypeUpgrade.MAX);
        UpdateProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueR, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }

    //ButtonMaxGUpが押されたら
    public void PushButtonMaxGUp()
    {
        Debug.Log("ButtonMaxRUpが押された");
        ModelProduction.UpgradeValue(ref CurG, ref CostMaxGUp, ref MaxG, ModelProduction.TypeUpgrade.MAX);
        UpdateProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueG, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }

    //ButtonMaxBUpが押されたら
    public void PushButtonMaxBUp()
    {
        Debug.Log("ButtonMaxRUpが押された");
        ModelProduction.UpgradeValue(ref CurB, ref CostMaxBUp, ref MaxB, ModelProduction.TypeUpgrade.MAX);
        UpdateProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueB, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }


    ////////////////////////////////////////////////
    //View

    //グラフィックをデータと同期させる
    public void UpdateProductionScene()
    {
        UpdateProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueR, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
        UpdateProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueG, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
        UpdateProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueB, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }

    public void UpdateProductionOneColor(ulong ColorValue, ulong MaxValue, Text TextColorValue, Slider SliderColorValue, ulong IncreaseValue, ulong CostIncreaseUp, Text TextIncreaseValue, Text TextCostIncreaseValueUp, Slider SliderCostIncrease, Button ButtonIncreaseValueUp, ulong CostMaxValueUp, Text TextCostMaxValueUp, Slider SliderCostMaxValueUp, Button ButtonMaxValueUp)
    {
        TextColorValue.text = string.Format("{0} / {1}", ColorValue, MaxValue);
        SliderColorValue.maxValue = MaxValue;
        SliderColorValue.minValue = 0;
        SliderColorValue.value = ColorValue;

        TextIncreaseValue.text = string.Format("+{0}", IncreaseValue);
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