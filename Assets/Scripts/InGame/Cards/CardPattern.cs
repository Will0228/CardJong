using System;

namespace CardJong.InGame.Cards
{
    /// <summary>
    /// 「色 + ランク」の組。
    /// 上がり形はマークを問わず色で判定するため、待ち牌はこの単位で表現する。
    /// </summary>
    public readonly struct CardPattern : IEquatable<CardPattern>
    {
        public CardColor Color { get; }

        public Rank Rank { get; }

        public CardPattern(CardColor color, Rank rank)
        {
            Color = color;
            Rank = rank;
        }

        /// <summary>このパターンに合致するカードかどうか。</summary>
        public bool Matches(Card card) => card.Color == Color && card.Rank == Rank;

        /// <summary>このパターンを満たす代表的なカードを 1 枚返す。上がり形の判定用。</summary>
        public Card ToRepresentativeCard()
            => new(Color == CardColor.Red ? Suit.Heart : Suit.Spade, Rank);

        public bool Equals(CardPattern other) => Color == other.Color && Rank == other.Rank;

        public override bool Equals(object obj) => obj is CardPattern other && Equals(other);

        public override int GetHashCode() => ((int)Color << 8) | (int)Rank;

        public static bool operator ==(CardPattern left, CardPattern right) => left.Equals(right);

        public static bool operator !=(CardPattern left, CardPattern right) => !left.Equals(right);

        public override string ToString() => $"{(Color == CardColor.Red ? "Red" : "Black")}-{Card.RankLabel(Rank)}";
    }
}
