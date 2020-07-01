using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class ImagegUtility : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static Texture2D ReadPng(string argImagePath)
    {
        FileStream fs = new FileStream(argImagePath, FileMode.Open, FileAccess.Read);
        BinaryReader br = new BinaryReader(fs);

        byte[] readBinary = br.ReadBytes((int)br.BaseStream.Length);

        br.Close();
        fs.Close();

        int w = 0;
        int h = 0;

        int pos = 16;
        for (int i = 0; i < 4; i++)
        {
            w = w * 256 + readBinary[pos++];
        }
        for (int i = 0; i < 4; i++)
        {
            h = h * 256 + readBinary[pos++];
        }

        Texture2D resTex = new Texture2D(w, h);
        resTex.LoadImage(readBinary);

        //テクスチャの設定
        resTex.filterMode = FilterMode.Point;

        return resTex;
    }

    public static Texture2D MakeSilhouette(Texture2D argTexture2D)
    {
        Texture2D resultTexture2D = new Texture2D(argTexture2D.width, argTexture2D.height, TextureFormat.ARGB32, false);

        for (int y = 0; y < argTexture2D.height; y++)
        {
            for (int x = 0; x < argTexture2D.width; x++)
            {
                if (argTexture2D.GetPixel(x, y).a == 0)
                    resultTexture2D.SetPixel(x, y, new Color(0, 0, 0, 0));
                else
                {
                    resultTexture2D.SetPixel(x, y, new Color(0.0f, 0.0f, 0.0f, 1.0f));
                }
            }
        }
        resultTexture2D.Apply();

        return resultTexture2D;
    }

}
