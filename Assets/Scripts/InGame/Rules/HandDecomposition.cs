using System.Collections.Generic;
using CardJong.InGame.Cards;

namespace CardJong.InGame.Rules
{
    /// <summary>上がり形を構成する 1 組（5 枚組 / 4 枚組 / 3 枚組）。</summary>
    public sealed class HandGroup
    {
        /// <summary>刻子なら true、順子なら false。</summary>
        public bool IsTriplet { get; }

        /// <summary>組の色。1 つの組に赤と黒は混在しない。</summary>
        public CardColor Color { get; }

        /// <summary>組を構成するランク。順子の場合は並び順（Q-K-A なら Q, K, A）。</summary>
        public IReadOnlyList<Rank> Ranks { get; }

        /// <summary>鳴いて作った組かどうか。</summary>
        public bool IsMelded { get; }

        public int Size => Ranks.Count;

        public HandGroup(bool isTriplet, CardColor color, IReadOnlyList<Rank> ranks, bool isMelded)
        {
            IsTriplet = isTriplet;
            Color = color;
            Ranks = ranks;
            IsMelded = isMelded;
        }

        public override string ToString()
        {
            var labels = new string[Ranks.Count];
            for (var i = 0; i < Ranks.Count; i++)
            {
                labels[i] = Card.RankLabel(Ranks[i]);
            }

            return $"{(IsTriplet ? "刻子" : "順子")}({Color}){string.Join("-", labels)}";
        }
    }

    /// <summary>成立した上がり形の内訳。役判定はこれを入力にする。</summary>
    public sealed class HandDecomposition
    {
        public CardColor PairColor { get; }

        public Rank PairRank { get; }

        /// <summary>5 枚組・4 枚組・3 枚組。鳴いた組も含む。</summary>
        public IReadOnlyList<HandGroup> Groups { get; }

        public HandDecomposition(CardColor pairColor, Rank pairRank, IReadOnlyList<HandGroup> groups)
        {
            PairColor = pairColor;
            PairRank = pairRank;
            Groups = groups;
        }
    }
}
