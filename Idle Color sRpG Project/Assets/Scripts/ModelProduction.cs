using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;

public class ModelProduction : MonoBehaviour
{

    public enum TypeUpgrade { INCREASE = 1, MAX = 2 };

    // Start is called before the first frame update
    //int a = 0;
    void Start()
    {
        //Debug.Log("a : " + a);
        //a++;
    }

    // Update is called once per frame
    void Update()
    {

    }

    //RGB値を生産する
    public bool Increase(ref ulong Target, ulong IncreaseValue, ulong MaxValue)
    {
        Debug.Log("Increaseが呼ばれた");
        bool result = false;

        if (Target + IncreaseValue <= MaxValue)
        {
            Target += IncreaseValue;
            result = true;
        }
        else
        {
            Target = MaxValue;
            result = false;
        }

        return result;
    }

    //クリック増加量を強化する
    public bool UpgradeValue(ref ulong SourceValue, ref ulong Cost, ref ulong UpgradeValue, TypeUpgrade TypeUpgrade)
    {
        Debug.Log("UpUpgradeValueが呼ばれた");
        bool result = false;

        if(SourceValue >= Cost)
        {
            SourceValue -= Cost;
            if (TypeUpgrade == TypeUpgrade.INCREASE)
            {
                //コスト計算
                UpgradeValue += 1 + (ulong)(UpgradeValue * 0.5);
                Cost = (ulong)(Cost * (1 + (UpgradeValue * 0.025)));
            }
            else if (TypeUpgrade == TypeUpgrade.MAX)
            {
                //コスト計算
                UpgradeValue = (ulong)(UpgradeValue * 2);
                //Cost = (ulong)(Cost * (1 + (UpgradeValue * 0.0005)));//0.0008　大きすぎる
                Cost = (ulong)(Cost * 2);//0.0008　大きすぎる
                if(Cost > UpgradeValue)
                {
                    Debug.Log("コスト超過!!!!!!!!!! " + Cost);
                    Cost = UpgradeValue;
                }
            }

            result = true;
        }

        return result;
    }

}
