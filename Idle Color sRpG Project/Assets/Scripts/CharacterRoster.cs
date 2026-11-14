using UnityEngine;

// キャラの登録表。新しい敵はここに1行足す。
// 画像は Assets/StreamingAssets/Character に、縦横が同じ正方形の PNG を置く。
// 初期所持は ID 1〜3 だけ。StartsOwned が false のキャラは、セーブに所持数が無いあいだ未所持。仲間化すると所持になる。
public sealed class CharacterDefinition
{
    public uint Id;
    public string FileName;
    public string DisplayName;
    public bool StartsOwned;

    public CharacterDefinition(uint id, string fileName, string displayName, bool startsOwned)
    {
        Id = id;
        FileName = fileName;
        DisplayName = displayName;
        StartsOwned = startsOwned;
    }
}

public static class CharacterRoster
{
    // ID は 1〜CHARACTERS_ALL_NUM。
    public static readonly CharacterDefinition[] All =
    {
        new CharacterDefinition(1, "RedSlime8.png", "ミニレッドスライム", true),
        new CharacterDefinition(2, "GreenSlime8.png", "ミニグリーンスライム", true),
        new CharacterDefinition(3, "BlueSlime8.png", "ミニブルースライム", true),
        new CharacterDefinition(4, "GraySlime8.png", "ミニグレースライム", false),
        new CharacterDefinition(5, "WhiteSlime8.png", "ミニホワイトスライム", false),
        new CharacterDefinition(6, "RBlackCat8.png", "チビ黒猫R", false),
        new CharacterDefinition(7, "GBlackCat8.png", "チビ黒猫G", false),
        new CharacterDefinition(8, "BBlackCat8.png", "チビ黒猫B", false),
        new CharacterDefinition(9, "GrayCat8.png", "チビ灰猫", false),
        new CharacterDefinition(10, "WhiteCat8.png", "チビ白猫", false),
        new CharacterDefinition(11, "RedSlime16.png", "レッドスライム", false),
        new CharacterDefinition(12, "GreenSlime16.png", "グリーンスライム", false),
        new CharacterDefinition(13, "BlueSlime16.png", "ブルースライム", false),
        new CharacterDefinition(14, "GraySlime16.png", "グレースライム", false),
        new CharacterDefinition(15, "WhiteSlime16.png", "ホワイトスライム", false),
        new CharacterDefinition(16, "ペンカド_32.png", "ペンカド", false),
        new CharacterDefinition(17, "モッシー_32.png", "モッシー", false),
        new CharacterDefinition(18, "ネギュシ_32.png", "ネギュシ", false),
        new CharacterDefinition(19, "ズンラゲ_64.png", "ズンラゲ", false),
        new CharacterDefinition(20, "ネギュシ_64.png", "オオネギュシ", false),
        new CharacterDefinition(21, "ミケ_32.png", "ミケ", false),
        new CharacterDefinition(22, "グルン_32.png", "グルン", false),
        new CharacterDefinition(23, "ベリー_32.png", "ベリー", false),
        new CharacterDefinition(24, "グレイズ_64.png", "グレイズ", false),
        new CharacterDefinition(25, "ベリル_64.png", "ベリル", false),
        new CharacterDefinition(26, "コウモリ_32.png", "コウモリ", false),
        new CharacterDefinition(27, "カラス_32.png", "カラス", false),
        new CharacterDefinition(28, "シャチ_32.png", "シャチ", false),
        new CharacterDefinition(29, "オオカミ_64.png", "オオカミ", false),
        new CharacterDefinition(30, "ブラックドラゴン_128.png", "ブラックドラゴン", false),
        new CharacterDefinition(31, "0064_slimeking_G.png", "GreenSlimeKing", false),
        new CharacterDefinition(32, "0064_slimeking_B.png", "BlueSlimeKing", false),
        new CharacterDefinition(33, "0016_akaneko.png", "赤ネコ", false),
        new CharacterDefinition(34, "0016_midorineko.png", "緑ネコ", false),
        new CharacterDefinition(35, "0016_aoneko.png", "青ネコ", false),
        new CharacterDefinition(36, "0016_kuroneko.png", "黒ネコ", false),
        new CharacterDefinition(37, "0016_shironeko.png", "白ネコ", false),
    };

    public static void RegisterAll(CharacterClass[] characters)
    {
        if (characters == null)
            return;

        for (int i = 0; i < All.Length; i++)
        {
            CharacterDefinition def = All[i];
            if (def.Id == 0 || def.Id >= characters.Length || characters[def.Id] == null)
                continue;

            string path = Application.persistentDataPath + "/Character/" + def.FileName;
            characters[def.Id].MakeCharacter(path, def.Id, def.DisplayName);
            if (!def.StartsOwned && characters[def.Id].ID == def.Id)
            {
                characters[def.Id].OwnedNumCur = 0;
                characters[def.Id].OwnedNumMax = 0;
            }
        }
    }
}
