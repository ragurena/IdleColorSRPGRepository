// 人の生産量・経験値・バトル倍率。バランス調整はここだけを変える。
public class PersonBalanceConfig
{
    public int RgbBase = 10;
    public int RgbSpecialtyBase = 50;
    public int PixelBase = 10;
    public int PixelSpecialtyBase = 25;
    public int PixelSpd = 50;
    public int CharacterBase = 1;
    public int AuthorCharacterMultiplier = 2;
    public int ExpSpecialtyMultiplier = 3;
    // ピクセル生産の完成経験値。作った色が得意属性なら、この倍にする。
    public int PixelExpSpecialtyMultiplier = 2;
    public int RgbExpBatch = 255;
    public int LevelGrowthDivisor = 10;
    // ピクセル生産のレベル成長。共通の LevelGrowthDivisor の半分なので、1レベルごとの増加は倍。
    public int PixelLevelGrowthDivisor = 5;
    public int ExpBase = 100;
    // ピクセル生産の必要経験値は、初期値 ExpBase をこの倍にしてからレベルで増やす。
    public int PixelExpBaseMultiplier = 3;
    // キャラ作成の必要経験値は、初期値 ExpBase をこの倍にしてからレベルで増やす。
    public int CharacterExpBaseMultiplier = 8;
    // キャラ作成の完成経験値は、不透明ピクセル数をこの数で割る。端数は切り上げ。
    public int CharacterExpOpaqueDivisor = 8;
    public double RgbExpExponent = 1.7;
    public double PixelExpExponent = 1.6;
    public double CharacterExpExponent = 1.5;
    public double BattleExpExponent = 1.7;
    public double SpecialtyBuffMatch = 1.5;
    public double SpecialtyBuffAuthor = 2.0;
    public double BattleLevelBonus = 0.01;
    public double StockFull = 1.0;
    public double StockEight = 0.8;
    public double StockHalf = 0.5;
    public int PosterizeLevels = 8;
    public int MaxLevel = 9999;
    public int MaxLevelsPerGrant = 30;

    public static PersonBalanceConfig CreateDefault()
    {
        return new PersonBalanceConfig();
    }
}
