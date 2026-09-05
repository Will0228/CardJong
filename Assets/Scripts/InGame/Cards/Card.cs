using System;

namespace CardJong.InGame.Cards
{
    /// <summary>
    /// 1 枚のカード。52 枚 x 4 デッキ構成のため、同じ値のカードは最大 4 枚存在する。
    /// </summary>
    public readonly struct Card : IEquatable<Card>
    {
        public Suit Suit { get; }

        public Rank Rank { get; }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        /// <summary>色。♥♦ が赤、♠♣ が黒。</summary>
        public CardColor Color => Suit is Suit.Heart or Suit.Diamond ? CardColor.Red : CardColor.Black;

        /// <summary>么九札（A・J・Q・K）かどうか。</summary>
        public bool IsTerminal => Rank is Rank.Ace or Rank.Jack or Rank.Queen or Rank.King;

        /// <summary>純全帯么九で要求される端（A・K）かどうか。</summary>
        public bool IsEdge => Rank is Rank.Ace or Rank.King;

        /// <summary>色とランクだけを見たパターン。上がり形の判定・待ちの表現に使う。</summary>
        public CardPattern Pattern => new(Color, Rank);

        public bool Equals(Card other) => Suit == other.Suit && Rank == other.Rank;

        public override bool Equals(object obj) => obj is Card other && Equals(other);

        public override int GetHashCode() => ((int)Suit << 8) | (int)Rank;

        public static bool operator ==(Card left, Card right) => left.Equals(right);

        public static bool operator !=(Card left, Card right) => !left.Equals(right);

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
