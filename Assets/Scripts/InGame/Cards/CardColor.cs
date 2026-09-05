namespace CardJong.InGame.Cards
{
    /// <summary>色。上がり形（刻子・順子・雀頭）の成立条件はこのレベルで判定する。</summary>
    public enum CardColor : byte
    {
        /// <summary>黒（♠♣）</summary>
        Black = 0,

        /// <summary>赤（♥♦）</summary>
        Red = 1,
    }
}
