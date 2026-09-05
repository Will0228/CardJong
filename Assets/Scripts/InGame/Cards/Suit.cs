namespace CardJong.InGame.Cards
{
    /// <summary>マーク（スート）。鳴きの成立条件はこのレベルで判定する。</summary>
    public enum Suit : byte
    {
        /// <summary>未設定。</summary>
        None = 0,

        /// <summary>♠</summary>
        Spade = 1,

        /// <summary>♥</summary>
        Heart = 2,

        /// <summary>♦</summary>
        Diamond = 3,

        /// <summary>♣</summary>
        Club = 4,
    }
}
