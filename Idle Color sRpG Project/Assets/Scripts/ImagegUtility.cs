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

    public static Texture2D ReadPng(string fileNameOrPath)
    {
        // 0. 安全チェック（nullや空文字のときは即座に理由をログに出す）
        if (string.IsNullOrWhiteSpace(fileNameOrPath))
        {
            Debug.LogWarning("[ReadPng] 引数のパスが空っぽ(nullまたは空文字)です。");
            return null;
        }

        // 1. 渡された文字列がフルパスか、ファイル名だけかを判別して正しいパスを作る
        string fullPath = fileNameOrPath;

        if (!fileNameOrPath.Contains(Application.persistentDataPath))
        {
            string fileName = System.IO.Path.GetFileName(fileNameOrPath);
            fullPath = System.IO.Path.Combine(Application.persistentDataPath, "Character", fileName);
        }

        fullPath = fullPath.Replace('\\', '/');
        Debug.Log("[ReadPng] 読み込みを試みる最終パス: " + fullPath);

        // 2. ファイルの存在チェック
        if (!System.IO.File.Exists(fullPath))
        {
            // ★あえて LogError ではなく Log にしてゲームが止まるのを防ぎつつ原因を表示
            Debug.LogWarning("[ReadPng] 指定されたパスにファイルが存在しません: " + fullPath);
            return null;
        }

        // 3. 画像ファイルをバイト配列として読み込み、Texture2Dに変換
        try
        {
            byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
            Debug.Log($"[ReadPng] ファイルの読み込みに成功。バイト数: {bytes.Length} bytes");
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(bytes))
            {
                Debug.Log("[ReadPng] Texture2Dへの変換(LoadImage)に成功しました！");
                return texture;
            }
            else
            {
                Debug.LogError("[ReadPng] ファイルは存在しますが、画像(PNG)としてのデコードに失敗しました。ファイルが破損しているか、PNG形式ではない可能性があります。");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError("[ReadPng] ファイルの読み込み中にエラーが発生しました: " + e.Message);
        }

        return null;

    }



    public static Texture2D MakeSilhouetteTexture(Texture2D argTexture2D)
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
    public static bool[,] MakeSilhouetteBoolArray(Texture2D argTexture2D)
    {
        bool[,] resultBoolArray = new bool[argTexture2D.width, argTexture2D.height];

        for (int y = 0; y < argTexture2D.height; y++)
        {
            for (int x = 0; x < argTexture2D.width; x++)
            {
                if (argTexture2D.GetPixel(x, y).a == 0)
                    resultBoolArray[x, y] = true;
                else
                {
                    resultBoolArray[x, y] = false;
                }
            }
        }

        return resultBoolArray;
    }

    public static Texture BoolArrayTOTexture(bool[,] argBoolArray, int argWidth, int argHeight, Color argTrueColor, Color argFalseColor)
    {
        Texture2D resultTexture2D = new Texture2D(argWidth, argHeight, TextureFormat.ARGB32, false);

        for (int y = 0; y < argHeight; y++)
        {
            for (int x = 0; x < argWidth; x++)
            {
                if (argBoolArray[x,y])
                    resultTexture2D.SetPixel(x, y, argTrueColor);
                else
                {
                    resultTexture2D.SetPixel(x, y, argFalseColor);
                    resultTexture2D.filterMode = FilterMode.Point;
                }
            }
        }
        resultTexture2D.Apply();

        return resultTexture2D;
    }
    public static Texture BoolArrayTOTexture(bool[,] argBoolArray, Texture2D argTrueColorTexture, Color argFalseColor)
    {
        Texture2D resultTexture2D = new Texture2D(argTrueColorTexture.width, argTrueColorTexture.height, TextureFormat.ARGB32, false);

        for (int y = 0; y < argTrueColorTexture.height; y++)
        {
            for (int x = 0; x < argTrueColorTexture.width; x++)
            {
                if (argBoolArray[x, y])
                    resultTexture2D.SetPixel(x, y, argTrueColorTexture.GetPixel(x, y));
                else
                {
                    resultTexture2D.SetPixel(x, y, argFalseColor);
                    resultTexture2D.filterMode = FilterMode.Point;
                }
            }
        }
        resultTexture2D.Apply();

        return resultTexture2D;
    }
}
