using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;

namespace CardJong.InGame.Rules
{
    /// <summary>
    /// 他家の捨て札に対する宣言の種類。
    /// 数値が大きいほど優先度が高い（ロン &gt; ポン &gt; チー）。
    /// </summary>
    public enum ClaimType : byte
    {
        /// <summary>未設定。</summary>
        None = 0,

        /// <summary>何もしない。</summary>
        Pass = 1,

        /// <summary>チー（順子の鳴き）。上家からのみ。</summary>
        Chi = 2,

        /// <summary>ポン（刻子の鳴き）。</summary>
        Pon = 3,

        /// <summary>ロン（他家の捨て札で上がる）。</summary>
        Ron = 4,
    }

    /// <summary>捨て札に対して実行できる宣言 1 つ分。</summary>
    public sealed class ClaimOption
    {
        private static readonly Card[] EmptyCards = Array.Empty<Card>();

        public ClaimType Type { get; }

        /// <summary>手札から使うカード。ロンの場合は空。</summary>
        public IReadOnlyList<Card> UsedCards { get; }

        /// <summary>完成する組の枚数（3 または 4）。ロンの場合は 0。</summary>
        public int MeldSize { get; }

        public ClaimOption(ClaimType type, IReadOnlyList<Card> usedCards, int meldSize)
        {
            Type = type;
            UsedCards = usedCards ?? EmptyCards;
            MeldSize = meldSize;
        }

        public static ClaimOption Ron() => new(ClaimType.Ron, EmptyCards, 0);

        public override string ToString()
            => Type == ClaimType.Ron ? "Ron" : $"{Type}{MeldSize}[{string.Join(" ", UsedCards)}]";
    }
}
