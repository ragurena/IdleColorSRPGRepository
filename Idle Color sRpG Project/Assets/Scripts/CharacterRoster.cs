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
    // ID は 1〜CHARACTERS_ALL_NUM。空きは 15, 16, 17, 18, 22, 32。
    public static readonly CharacterDefinition[] All =
    {
        new CharacterDefinition(1, "RedSlime8.png", "LittleRedSlime", true),
        new CharacterDefinition(2, "GreenSlime8.png", "LittleGreenSlime", true),
        new CharacterDefinition(3, "BlueSlime8.png", "LittleBlueSlime", true),
        new CharacterDefinition(4, "GraySlime8.png", "LittleGraySlime", false),
        new CharacterDefinition(5, "WhiteSlime8.png", "LittleWhiteSlime", false),
        new CharacterDefinition(6, "RBlackCat8.png", "LittleRBlackCat", false),
        new CharacterDefinition(7, "GBlackCat8.png", "LittleGBlackCat", false),
        new CharacterDefinition(8, "BBlackCat8.png", "LittleBBlackCat", false),
        new CharacterDefinition(9, "GrayCat8.png", "LittleGrayCat", false),
        new CharacterDefinition(10, "WhiteCat8.png", "LittleWhiteCat", false),
        new CharacterDefinition(11, "RedSlime16.png", "SmallRedSlime", false),
        new CharacterDefinition(12, "GreenSlime16.png", "SmallGreenSlime", false),
        new CharacterDefinition(13, "BlueSlime16.png", "SmallBlueSlime", false),
        new CharacterDefinition(14, "WhiteSlime16.png", "SmallWhiteSlime", false),
        new CharacterDefinition(19, "0032_slime_R.png", "RedSlime", false),
        new CharacterDefinition(20, "0032_slime_G.png", "GreenSlime", false),
        new CharacterDefinition(21, "0032_slime_B.png", "BlueSlime", false),
        new CharacterDefinition(23, "0032_rabbit.png", "WhiteRabbit", false),
        new CharacterDefinition(24, "0064_slimeking_R.png", "RedSlimeKing", false),
        new CharacterDefinition(25, "0064_slimeking_G.png", "GreenSlimeKing", false),
        new CharacterDefinition(26, "0064_slimeking_B.png", "BlueSlimeKing", false),
        new CharacterDefinition(27, "0016_akaneko.png", "赤ネコ", false),
        new CharacterDefinition(28, "0016_midorineko.png", "緑ネコ", false),
        new CharacterDefinition(29, "0016_aoneko.png", "青ネコ", false),
        new CharacterDefinition(30, "0016_kuroneko.png", "黒ネコ", false),
        new CharacterDefinition(31, "0016_shironeko.png", "白ネコ", false),
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
