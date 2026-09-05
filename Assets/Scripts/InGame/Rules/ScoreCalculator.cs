using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using VContainer;

namespace CardJong.InGame.Rules
{
    /// <summary>
    /// 役判定と点数計算。
    /// </summary>
    /// <remarks>
    /// 現状で判定できるのは、上がり形の内訳だけで決まる役に限る（リーチ・ツモ・断么九・
    /// 対々和・平和・一色・清一色）。以下は未実装。
    /// TODO: 一発 / ハイテイ / ホウテイ / ダブルリーチ
    /// TODO: 混全帯么九 / 純全帯么九 / 一気通貫 / 入れ子
    /// TODO: ポーカー役（ファイブカード・ストレートフラッシュ・フォーカード・フルハウス）
    /// TODO: 役満各種
    /// TODO: 役なしでは上がれない制約（現状は 0 翻でも上がれてしまう）
    /// TODO: リーチ時の裏ドラ
    /// </remarks>
    public sealed class ScoreCalculator : IScoreCalculator
    {
        private readonly IHandAnalyzer _handAnalyzer;

        [Inject]
        public ScoreCalculator(IHandAnalyzer handAnalyzer)
        {
            _handAnalyzer = handAnalyzer ?? throw new ArgumentNullException(nameof(handAnalyzer));
        }

        public WinResult Evaluate(InGameModel model, int winnerSeat, int loserSeat, Card winningCard)
        {
            var player = model.GetPlayer(winnerSeat);
            var isTsumo = loserSeat < 0;

            // 上がり形の 14 枚（ロンの場合は上がり札を足した状態）
            var concealed = new List<Card>(player.Cards.ConcealedCards);
            if (!isTsumo) concealed.Add(winningCard);

            var allCards = new List<Card>(concealed);
            for (var i = 0; i < player.Cards.Melds.Count; i++)
            {
                allCards.AddRange(player.Cards.Melds[i].Cards);
            }

            var yaku = new List<YakuResult>();
            if (_handAnalyzer.TryDecompose(concealed, player.Cards.Melds, out var decomposition))
            {
                CollectYaku(player, decomposition, allCards, isTsumo, yaku);
            }

            var doraCount = CountDora(model, allCards);

            var han = doraCount;
            for (var i = 0; i < yaku.Count; i++)
            {
                han += yaku[i].Han;
            }

            return new WinResult(winnerSeat, loserSeat, winningCard, yaku, doraCount, han, IsYakuman: false);
        }

        public int[] CalculateWinDeltas(InGameModel model, WinResult win)
        {
            var deltas = new int[model.PlayerCount];
            var dealerSeat = model.DealerSeat.CurrentValue;
            var isDealerWin = win.WinnerSeat == dealerSeat;
            var honbaTotal = ScoreTable.HonbaBonus * model.Honba.CurrentValue;

            if (win.IsTsumo)
            {
                var payerCount = model.PlayerCount - 1;
                var honbaEach = payerCount > 0 ? honbaTotal / payerCount : 0;

                for (var seat = 0; seat < model.PlayerCount; seat++)
                {
                    if (seat == win.WinnerSeat) continue;

                    var payment = GetTsumoPayment(win, isDealerWin, isPayerDealer: seat == dealerSeat) + honbaEach;
                    deltas[seat] -= payment;
                    deltas[win.WinnerSeat] += payment;
                }
            }
            else
            {
                var payment = ScoreTable.GetRonPayment(win.Han, isDealerWin, win.IsYakuman) + honbaTotal;
                deltas[win.LoserSeat] -= payment;
                deltas[win.WinnerSeat] += payment;
            }

            return deltas;
        }

        public int[] CalculateDrawGameDeltas(InGameModel model, IReadOnlyList<int> tenpaiSeats)
        {
            var deltas = new int[model.PlayerCount];
            var tenpaiCount = tenpaiSeats?.Count ?? 0;
            var notenCount = model.PlayerCount - tenpaiCount;

            // 全員テンパイ・全員ノーテンなら移動なし
            if (tenpaiCount == 0 || notenCount == 0) return deltas;

            var gainPerTenpai = ScoreTable.NotenPenaltyTotal / tenpaiCount;
            var payPerNoten = ScoreTable.NotenPenaltyTotal / notenCount;

            for (var seat = 0; seat < model.PlayerCount; seat++)
            {
                deltas[seat] = Contains(tenpaiSeats, seat) ? gainPerTenpai : -payPerNoten;
            }

            return deltas;
        }

        /// <summary>ツモ和了で 1 人が支払う点数。親が上がった場合は全員同額。</summary>
        private int GetTsumoPayment(WinResult win, bool isDealerWin, bool isPayerDealer)
        {
            if (isDealerWin) return ScoreTable.GetDealerTsumoPayment(win.Han, win.IsYakuman);

            return isPayerDealer
                ? ScoreTable.GetNonDealerTsumoPaymentFromDealer(win.Han, win.IsYakuman)
                : ScoreTable.GetNonDealerTsumoPaymentFromNonDealer(win.Han, win.IsYakuman);
        }

        private void CollectYaku(
            PlayerModel player,
            HandDecomposition decomposition,
            IReadOnlyList<Card> allCards,
            bool isTsumo,
            List<YakuResult> yaku)
        {
            // 状況役
            if (player.Status.IsRiichi) yaku.Add(new YakuResult("リーチ", 1));
            if (isTsumo && player.Cards.IsMenzen) yaku.Add(new YakuResult("ツモ", 1));

            // 手役
            if (IsAllSimples(allCards)) yaku.Add(new YakuResult("断么九", 1));

            if (IsAllTriplets(decomposition))
            {
                yaku.Add(new YakuResult("対々和", 2));
            }
            else if (player.Cards.IsMenzen && IsAllRuns(decomposition))
            {
                yaku.Add(new YakuResult("平和", 1));
            }

            // 清一色は一色の上位役なので重複させない
            if (IsSameSuit(allCards))
            {
                yaku.Add(new YakuResult("清一色", player.Cards.IsMenzen ? 4 : 3));
            }
            else if (IsSameColor(allCards))
            {
                yaku.Add(new YakuResult("一色", player.Cards.IsMenzen ? 2 : 1));
            }
        }

        /// <summary>断么九: A・J・Q・K を 1 枚も含まない。</summary>
        private bool IsAllSimples(IReadOnlyList<Card> cards)
        {
            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i].IsTerminal) return false;
            }

            return true;
        }

        /// <summary>対々和: 5 / 4 / 3 枚組がすべて刻子。</summary>
        private bool IsAllTriplets(HandDecomposition decomposition)
        {
            for (var i = 0; i < decomposition.Groups.Count; i++)
            {
                if (!decomposition.Groups[i].IsTriplet) return false;
            }

            return true;
        }

        /// <summary>平和: 5 / 4 / 3 枚組がすべて順子。</summary>
        private bool IsAllRuns(HandDecomposition decomposition)
        {
            for (var i = 0; i < decomposition.Groups.Count; i++)
            {
                if (decomposition.Groups[i].IsTriplet) return false;
            }

            return true;
        }

        /// <summary>一色: 14 枚すべてが同じ色。</summary>
        private bool IsSameColor(IReadOnlyList<Card> cards)
        {
            if (cards.Count == 0) return false;

            var color = cards[0].Color;
            for (var i = 1; i < cards.Count; i++)
            {
                if (cards[i].Color != color) return false;
            }

            return true;
        }

        /// <summary>清一色: 14 枚すべてが同じマーク。</summary>
        private bool IsSameSuit(IReadOnlyList<Card> cards)
        {
            if (cards.Count == 0) return false;

            var suit = cards[0].Suit;
            for (var i = 1; i < cards.Count; i++)
            {
                if (cards[i].Suit != suit) return false;
            }

            return true;
        }

        private int CountDora(InGameModel model, IReadOnlyList<Card> cards)
        {
            var count = 0;
            for (var i = 0; i < cards.Count; i++)
            {
                if (model.Wall.IsDora(cards[i])) count++;
            }

            return count;
        }

        private bool Contains(IReadOnlyList<int> values, int target)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] == target) return true;
            }

            return false;
        }
    }
}
