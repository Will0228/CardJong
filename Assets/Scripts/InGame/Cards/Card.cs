namespace CardJong.InGame.Cards
{
    /// <summary>
    /// 1 枚のカード。52 枚 x 4 デッキ構成のため、同じ値のカードは最大 4 枚存在する。
    /// マークとランクが同じなら同じ札として扱うので、等値比較は record に任せる。
    /// </summary>
    /// <param name="Suit">マーク。</param>
    /// <param name="Rank">ランク。</param>
    public sealed record Card(Suit Suit, Rank Rank)
    {
        /// <summary>色。♥♦ が赤、♠♣ が黒。</summary>
        public CardColor Color => Suit is Suit.Heart or Suit.Diamond ? CardColor.Red : CardColor.Black;

        /// <summary>么九札（A・J・Q・K）かどうか。</summary>
        public bool IsTerminal => Rank is Rank.Ace or Rank.Jack or Rank.Queen or Rank.King;

        /// <summary>純全帯么九で要求される端（A・K）かどうか。</summary>
        public bool IsEdge => Rank is Rank.Ace or Rank.King;

        /// <summary>色とランクだけを見たパターン。上がり形の判定・待ちの表現に使う。</summary>
        public CardPattern Pattern => new(Color, Rank);

        public override string ToString() => $"{SuitSymbol(Suit)}{RankLabel(Rank)}";

        public static string SuitSymbol(Suit suit) => suit switch
        {
            Suit.Spade => "S",
            Suit.Heart => "H",
            Suit.Diamond => "D",
            Suit.Club => "C",
            _ => "?",
        };

        public static string RankLabel(Rank rank) => rank switch
        {
            Rank.Ace => "A",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            _ => ((int)rank).ToString(),
        };
    }
}
