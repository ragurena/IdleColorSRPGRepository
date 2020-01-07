using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveClass// : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Save(ulong CurR, ulong CurG, ulong CurB)
    {
        if(File.Exists("Assets/Resources/ICS.csv"))
        {
            Debug.Log("セーブファイルが存在します");
        }
        else
        {
            Debug.Log("セーブファイルが存在しません");
            FileStream fs = File.Create("Assets/Resources/ICS.csv");
            fs.Close();
        }

        StreamWriter sw = new StreamWriter("Assets/Resources/ICS.csv");
        sw.WriteLine("CurR," + CurR);
        sw.WriteLine("CurG," + CurG);
        sw.WriteLine("CurB," + CurB);
        sw.Flush();
        sw.Close();
    }

    public void Load(ref ulong CurR, ref ulong CurG, ref ulong CurB)
    {
        if (File.Exists("Assets/Resources/ICS.csv"))
        {
            Debug.Log("セーブファイルが存在します");
        }
        else
        {
            Debug.Log("セーブファイルが存在しません");
            FileStream fs = File.Create("Assets/Resources/ICS.csv");
            fs.Close();

            return;
        }

        StreamReader sr = new StreamReader("Assets/Resources/ICS.csv");

        string line = sr.ReadLine();
        string[] values = line.Split(',');
        CurR = (ulong)(int.Parse(values[1]));

        line = sr.ReadLine();
        values = line.Split(',');
        CurG = (ulong)(int.Parse(values[1]));

        line = sr.ReadLine();
        values = line.Split(',');
        CurB = (ulong)(int.Parse(values[1]));
    }
}
