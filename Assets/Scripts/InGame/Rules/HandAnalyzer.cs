using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;

namespace CardJong.InGame.Rules
{
    /// <summary>
    /// 上がり形の判定。
    /// 手牌 14 枚を 5 枚組 + 4 枚組 + 3 枚組 + 雀頭 に分割できるかを総当りで探索する。
    /// </summary>
    /// <remarks>
    /// 探索は「残っているカードのうち最も小さい 1 枚は必ずどれかの組に属する」という性質を使って
    /// 分岐を絞っている。組は必ず単色で、刻子（同ランク）か順子（連続ランク）のいずれか。
    /// A は順子の最小（A-2-3）としても最大（Q-K-A）としても使えるが、K-A-2 のような循環はしない。
    /// </remarks>
    public sealed class HandAnalyzer : IHandAnalyzer
    {
        // counts 配列は色・ランクをそのまま添字に使う。CardColor.None と Rank の 0 に当たる
        // 添字 0 は使わないので、どちらの次元も要素数を 1 つ多く取る。
        private const int ColorCount = 2;
        private const int RankCount = 13;

        /// <summary>鳴きが無いときに手札で作る必要がある組のサイズ。</summary>
        private static readonly int[] BaseGroupSizes = { 5, 4, 3 };

        public bool IsWinningHand(IReadOnlyList<Card> concealedCards, IReadOnlyList<Meld> melds)
            => TryDecompose(concealedCards, melds, out _);

        public bool TryDecompose(
            IReadOnlyList<Card> concealedCards,
            IReadOnlyList<Meld> melds,
            out HandDecomposition decomposition)
        {
            decomposition = null;
            if (concealedCards == null) return false;
            if (!TryGetRemainingGroupSizes(melds, out var sizes)) return false;

            var required = 2;
            for (var i = 0; i < sizes.Count; i++)
            {
                required += sizes[i];
            }

            if (concealedCards.Count != required) return false;

            var counts = BuildCounts(concealedCards);
            var used = new bool[sizes.Count];
            var groups = new List<HandGroup>(BaseGroupSizes.Length);

            // 雀頭は同一ランク 2 枚（色も一致）。候補を総当りする。
            for (var color = 1; color <= ColorCount; color++)
            {
                for (var rank = 1; rank <= RankCount; rank++)
                {
                    if (counts[color, rank] < 2) continue;

                    counts[color, rank] -= 2;
                    groups.Clear();
                    var formed = TryFormGroups(counts, sizes, used, sizes.Count, groups);
                    counts[color, rank] += 2;

                    if (!formed) continue;

                    AppendMeldedGroups(melds, groups);
                    decomposition = new HandDecomposition((CardColor)color, (Rank)rank, groups);
                    return true;
                }
            }

            return false;
        }

        public bool IsTenpai(IReadOnlyList<Card> concealedCards, IReadOnlyList<Meld> melds)
            => EnumerateWaits(concealedCards, melds).Count > 0;

        public IReadOnlyList<CardPattern> EnumerateWaits(IReadOnlyList<Card> concealedCards, IReadOnlyList<Meld> melds)
        {
            var waits = new List<CardPattern>();
            if (concealedCards == null) return waits;
            if (!TryGetRemainingGroupSizes(melds, out var sizes)) return waits;

            var required = 2;
            for (var i = 0; i < sizes.Count; i++)
            {
                required += sizes[i];
            }

            // テンパイは上がり形に 1 枚足りない状態。
            if (concealedCards.Count != required - 1) return waits;

            // 末尾の 1 枚を候補カードで差し替えながら総当りするので、置き場所だけ先に作る。
            var buffer = new List<Card>(concealedCards.Count + 1);
            buffer.AddRange(concealedCards);
            buffer.Add(null);
            var lastIndex = buffer.Count - 1;

            for (var color = 1; color <= ColorCount; color++)
            {
                for (var rank = 1; rank <= RankCount; rank++)
                {
                    var pattern = new CardPattern((CardColor)color, (Rank)rank);
                    buffer[lastIndex] = pattern.ToRepresentativeCard();
                    if (IsWinningHand(buffer, melds))
                    {
                        waits.Add(pattern);
                    }
                }
            }

            return waits;
        }

        /// <summary>
        /// 鳴いた組を差し引いて、手札で作る必要がある組のサイズを求める。
        /// 同じサイズの組は 1 つずつしか無いので、同サイズを 2 回鳴いた形は成立しない。
        /// </summary>
        private bool TryGetRemainingGroupSizes(IReadOnlyList<Meld> melds, out List<int> sizes)
        {
            sizes = new List<int>(BaseGroupSizes);
            if (melds == null) return true;

            for (var i = 0; i < melds.Count; i++)
            {
                if (!sizes.Remove(melds[i].Size)) return false;
            }

            return true;
        }

        private int[,] BuildCounts(IReadOnlyList<Card> cards)
        {
            var counts = new int[ColorCount + 1, RankCount + 1];
            for (var i = 0; i < cards.Count; i++)
            {
                counts[(int)cards[i].Color, (int)cards[i].Rank]++;
            }

            return counts;
        }

        private bool TryFormGroups(
            int[,] counts,
            IReadOnlyList<int> sizes,
            bool[] used,
            int remaining,
            List<HandGroup> groups)
        {
            if (remaining == 0) return IsEmpty(counts);
            if (!TryFindLowestCard(counts, out var color, out var rank)) return false;

            for (var i = 0; i < sizes.Count; i++)
            {
                if (used[i]) continue;

                var size = sizes[i];
                used[i] = true;

                // 刻子: 同じランク・同じ色を size 枚
                if (counts[color, rank] >= size)
                {
                    counts[color, rank] -= size;
                    groups.Add(CreateTriplet((CardColor)color, (Rank)rank, size));

                    if (TryFormGroups(counts, sizes, used, remaining - 1, groups))
                    {
                        counts[color, rank] += size;
                        used[i] = false;
                        return true;
                    }

                    groups.RemoveAt(groups.Count - 1);
                    counts[color, rank] += size;
                }

                // 順子: 連続するランク・同じ色を size 枚
                for (var start = 1; start + size - 1 <= CardRunUtility.ExtendedRankCount; start++)
                {
                    if (!CardRunUtility.RunContainsRank(start, size, rank)) continue;
                    if (!TryTakeRun(counts, color, start, size)) continue;

                    groups.Add(CreateRun((CardColor)color, start, size));

                    if (TryFormGroups(counts, sizes, used, remaining - 1, groups))
                    {
                        ReturnRun(counts, color, start, size);
                        used[i] = false;
                        return true;
                    }

                    groups.RemoveAt(groups.Count - 1);
                    ReturnRun(counts, color, start, size);
                }

                used[i] = false;
            }

            return false;
        }

        private void AppendMeldedGroups(IReadOnlyList<Meld> melds, List<HandGroup> groups)
        {
            if (melds == null) return;

            for (var i = 0; i < melds.Count; i++)
            {
                var meld = melds[i];
                var ranks = new Rank[meld.Cards.Count];
                for (var j = 0; j < meld.Cards.Count; j++)
                {
                    ranks[j] = meld.Cards[j].Rank;
                }

                groups.Add(new HandGroup(meld.Type == MeldType.Pon, meld.Cards[0].Color, ranks, true));
            }
        }

        private HandGroup CreateTriplet(CardColor color, Rank rank, int size)
        {
            var ranks = new Rank[size];
            for (var i = 0; i < size; i++)
            {
                ranks[i] = rank;
            }

            return new HandGroup(true, color, ranks, false);
        }

        private HandGroup CreateRun(CardColor color, int start, int size)
            => new(false, color, CardRunUtility.GetRunRanks(start, size), false);

        private bool TryTakeRun(int[,] counts, int color, int start, int size)
        {
            for (var p = start; p < start + size; p++)
            {
                if (counts[color, CardRunUtility.PositionToRank(p)] <= 0) return false;
            }

            for (var p = start; p < start + size; p++)
            {
                counts[color, CardRunUtility.PositionToRank(p)]--;
            }

            return true;
        }

        private void ReturnRun(int[,] counts, int color, int start, int size)
        {
            for (var p = start; p < start + size; p++)
            {
                counts[color, CardRunUtility.PositionToRank(p)]++;
            }
        }

        private bool TryFindLowestCard(int[,] counts, out int color, out int rank)
        {
            for (var c = 1; c <= ColorCount; c++)
            {
                for (var r = 1; r <= RankCount; r++)
                {
                    if (counts[c, r] <= 0) continue;

                    color = c;
                    rank = r;
                    return true;
                }
            }

            color = -1;
            rank = -1;
            return false;
        }

        private bool IsEmpty(int[,] counts)
        {
            for (var c = 1; c <= ColorCount; c++)
            {
                for (var r = 1; r <= RankCount; r++)
                {
                    if (counts[c, r] > 0) return false;
                }
            }

            return true;
        }
    }
}
