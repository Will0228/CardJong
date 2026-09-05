namespace CardJong.InGame.Cards
{
    /// <summary>マーク（スート）。鳴きの成立条件はこのレベルで判定する。</summary>
    public enum Suit : byte
    {
        /// <summary>♠</summary>
        Spade = 0,

        /// <summary>♥</summary>
        Heart = 1,

        /// <summary>♦</summary>
        Diamond = 2,

        /// <summary>♣</summary>
        Club = 3,
    }
}
