using System.Collections.Generic;
using CardJong.InGame.Cards;

namespace CardJong.InGame.Rules
{
    /// <summary>
    /// 順子の並びを扱うユーティリティ。
    /// A を最小（A-2-3）としても最大（Q-K-A）としても使えるようにするため、
    /// ランクではなく「位置 1〜14」で順子を表現する（位置 14 = 最大として使う A）。
    /// 位置は連続でしか取れないので、K-A-2 のような循環は自然に排除される。
    /// </summary>
    internal static class CardRunUtility
    {
        public const int ExtendedRankCount = 14;

        /// <summary>探索位置を実際のランク値に変換する。</summary>
        public static int PositionToRank(int position)
            => position == ExtendedRankCount ? (int)Rank.Ace : position;

        /// <summary>開始位置 start・長さ size の順子が、指定ランクを含むか。</summary>
        public static bool RunContainsRank(int start, int size, int rank)
        {
            for (var p = start; p < start + size; p++)
            {
                if (PositionToRank(p) == rank) return true;
            }

            return false;
        }

        /// <summary>開始位置 start・長さ size の順子を構成するランクを並び順で返す。</summary>
        public static Rank[] GetRunRanks(int start, int size)
        {
            var ranks = new Rank[size];
            for (var i = 0; i < size; i++)
            {
                ranks[i] = (Rank)PositionToRank(start + i);
            }

            return ranks;
        }

        /// <summary>
        /// カードの並びを順子として自然な順に整える。
        /// A と K が同時に含まれる場合、その順子は Q-K-A の形しか有り得ないので A を末尾に置く。
        /// </summary>
        public static List<Card> OrderAsRun(IReadOnlyList<Card> cards)
        {
            var ordered = new List<Card>(cards);
            ordered.Sort(static (a, b) => a.Rank.CompareTo(b.Rank));

            var hasAce = false;
            var hasKing = false;
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].Rank == Rank.Ace) hasAce = true;
                else if (ordered[i].Rank == Rank.King) hasKing = true;
            }

            if (!hasAce || !hasKing) return ordered;

            var aceIndex = ordered.FindIndex(static card => card.Rank == Rank.Ace);
            var ace = ordered[aceIndex];
            ordered.RemoveAt(aceIndex);
            ordered.Add(ace);
            return ordered;
        }
    }
}
