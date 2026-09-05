using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Rules;

namespace CardJong.InGame.Actions
{
    /// <summary>
    /// 待機中のプレイヤーが他家の捨て札に対して行った宣言。
    /// </summary>
    public readonly struct ClaimDeclaration
    {
        private static readonly Card[] EmptyCards = Array.Empty<Card>();

        public int Seat { get; }

        public ClaimType Type { get; }

        /// <summary>手札から使うカード。</summary>
        public IReadOnlyList<Card> UsedCards { get; }

        /// <summary>完成する組の枚数。</summary>
        public int MeldSize { get; }

        public bool IsPass => Type == ClaimType.Pass;

        public ClaimDeclaration(int seat, ClaimType type, IReadOnlyList<Card> usedCards, int meldSize)
        {
            Seat = seat;
            Type = type;
            UsedCards = usedCards ?? EmptyCards;
            MeldSize = meldSize;
        }

        public static ClaimDeclaration Pass(int seat) => new(seat, ClaimType.Pass, EmptyCards, 0);

        /// <summary>宣言なしを表す値。誰も鳴かなかった場合に使う。</summary>
        public static ClaimDeclaration None => new(-1, ClaimType.Pass, EmptyCards, 0);

        public static ClaimDeclaration From(int seat, ClaimOption option)
            => new(seat, option.Type, option.UsedCards, option.MeldSize);

        public override string ToString() => $"seat{Seat} {Type}";
    }
}
