using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;

namespace CardJong.InGame.Rules
{
    /// <summary>
    /// 鳴き・ロンの成立条件を判定する。
    /// 上がり形は色レベルで判定するのに対し、鳴きは同一マークで n-1 枚が必要という非対称性がある。
    /// </summary>
    public sealed class ClaimResolver : IClaimResolver
    {
        /// <summary>鳴きで作れる組は 3 枚組・4 枚組のみ。</summary>
        private static readonly int[] ClaimableMeldSizes = { 3, 4 };

        private readonly IHandAnalyzer _handAnalyzer;

        public ClaimResolver(IHandAnalyzer handAnalyzer)
        {
            _handAnalyzer = handAnalyzer ?? throw new ArgumentNullException(nameof(handAnalyzer));
        }

        public IReadOnlyList<ClaimOption> GetOptions(InGameModel model, int seat, DiscardInfo discard)
        {
            var options = new List<ClaimOption>();
            if (seat == discard.Seat) return options;

            var player = model.GetPlayer(seat);

            if (CanRon(player, discard.Card))
            {
                options.Add(ClaimOption.Ron());
            }

            AddPonOptions(player, discard.Card, options);

            // チーは上家の捨て札からのみ
            if (model.GetUpperSeat(seat) == discard.Seat)
            {
                AddChiOptions(player, discard.Card, options);
            }

            return options;
        }

        /// <summary>
        /// ロンできるか。上がり形が完成していて、かつフリテンでないこと。
        /// </summary>
        /// <remarks>
        /// TODO: 「役なしでは上がれない」の判定は <see cref="IScoreCalculator"/> 側の役判定が
        /// 揃ってから、ここに組み込む。
        /// </remarks>
        private bool CanRon(PlayerModel player, Card discarded)
        {
            // 見逃しによる一時フリテン
            if (player.IsTemporaryFuriten) return false;

            var hand = new List<Card>(player.ConcealedCards.Count + 1);
            hand.AddRange(player.ConcealedCards);
            hand.Add(discarded);

            if (!_handAnalyzer.IsWinningHand(hand, player.Melds)) return false;

            // フリテン: 自分の上がり札のいずれかが自分の捨て札に含まれる場合はロンできない
            var waits = _handAnalyzer.EnumerateWaits(player.ConcealedCards, player.Melds);
            for (var i = 0; i < waits.Count; i++)
            {
                for (var j = 0; j < player.Discards.Count; j++)
                {
                    if (waits[i].Matches(player.Discards[j])) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// ポンの選択肢。3 枚組には同一カード 2 枚、4 枚組には 3 枚が手札に必要。
        /// </summary>
        private static void AddPonOptions(PlayerModel player, Card discarded, List<ClaimOption> options)
        {
            var inHand = player.CountInHand(discarded);

            for (var i = 0; i < ClaimableMeldSizes.Length; i++)
            {
                var meldSize = ClaimableMeldSizes[i];
                var needed = meldSize - 1;
                if (inHand < needed) continue;

                var used = new Card[needed];
                for (var j = 0; j < needed; j++)
                {
                    used[j] = discarded;
                }

                options.Add(new ClaimOption(ClaimType.Pon, used, meldSize));
            }
        }

        /// <summary>
        /// チーの選択肢。3 枚組には同一マークの連続 2 枚、4 枚組には連続 3 枚が手札に必要。
        /// </summary>
        private static void AddChiOptions(PlayerModel player, Card discarded, List<ClaimOption> options)
        {
            for (var i = 0; i < ClaimableMeldSizes.Length; i++)
            {
                var meldSize = ClaimableMeldSizes[i];

                for (var start = 1; start + meldSize - 1 <= CardRunUtility.ExtendedRankCount; start++)
                {
                    if (!CardRunUtility.RunContainsRank(start, meldSize, (int)discarded.Rank)) continue;

                    var used = TryCollectRunFromHand(player, discarded, start, meldSize);
                    if (used == null) continue;

                    options.Add(new ClaimOption(ClaimType.Chi, used, meldSize));
                }
            }
        }

        /// <summary>
        /// 順子を埋めるのに必要なカードを手札から集める。揃わなければ null。
        /// 鳴いた組は同一マークで固定されるため、捨て札と同じマークのみを使う。
        /// </summary>
        private static List<Card> TryCollectRunFromHand(PlayerModel player, Card discarded, int start, int size)
        {
            var used = new List<Card>(size - 1);
            var remaining = new List<Card>(player.ConcealedCards);
            var isDiscardedUsed = false;

            for (var p = start; p < start + size; p++)
            {
                var rank = (Rank)CardRunUtility.PositionToRank(p);

                // 捨て札そのものが埋める位置
                if (!isDiscardedUsed && rank == discarded.Rank)
                {
                    isDiscardedUsed = true;
                    continue;
                }

                var needed = new Card(discarded.Suit, rank);
                var index = remaining.IndexOf(needed);
                if (index < 0) return null;

                remaining.RemoveAt(index);
                used.Add(needed);
            }

            return isDiscardedUsed ? used : null;
        }
    }
}
