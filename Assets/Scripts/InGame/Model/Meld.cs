using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;

namespace CardJong.InGame.Model
{
    /// <summary>鳴いて作った組の種類。カン（槓子）に相当する行為は存在しない。</summary>
    public enum MeldType : byte
    {
        /// <summary>未設定。</summary>
        None = 0,

        /// <summary>ポン（刻子の鳴き）</summary>
        Pon = 1,

        /// <summary>チー（順子の鳴き）</summary>
        Chi = 2,
    }

    /// <summary>
    /// 鳴いて公開された組。鳴いた瞬間に同一マークで固定され、以後変更できない。
    /// 鳴きで作れるのは 3 枚組・4 枚組のみ。
    /// </summary>
    public sealed class Meld
    {
        public MeldType Type { get; }

        /// <summary>鳴いた札を含む組の全カード。</summary>
        public IReadOnlyList<Card> Cards { get; }

        /// <summary>他家の捨て札から取得したカード。</summary>
        public Card ClaimedCard { get; }

        /// <summary>鳴いた相手の席。</summary>
        public int FromSeat { get; }

        public int Size => Cards.Count;

        public Meld(MeldType type, IReadOnlyList<Card> cards, Card claimedCard, int fromSeat)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (cards.Count is not (3 or 4))
            {
                throw new ArgumentException($"鳴きで作れるのは 3 枚組・4 枚組のみです: {cards.Count}", nameof(cards));
            }

            Type = type;
            Cards = cards;
            ClaimedCard = claimedCard;
            FromSeat = fromSeat;
        }

        public override string ToString() => $"{Type}[{string.Join(" ", Cards)}]";
    }
}
