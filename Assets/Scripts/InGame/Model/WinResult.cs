using System.Collections.Generic;
using CardJong.InGame.Cards;

namespace CardJong.InGame.Model
{
    /// <summary>成立した役 1 つ分。</summary>
    public readonly struct YakuResult
    {
        public string Name { get; }

        public int Han { get; }

        public YakuResult(string name, int han)
        {
            Name = name;
            Han = han;
        }

        public override string ToString() => $"{Name} {Han}翻";
    }

    /// <summary>上がりの内容。</summary>
    public sealed class WinResult
    {
        /// <summary>上がったプレイヤーの席。</summary>
        public int WinnerSeat { get; }

        /// <summary>放銃したプレイヤーの席。ツモ上がりの場合は -1。</summary>
        public int LoserSeat { get; }

        /// <summary>14 枚目になったカード。</summary>
        public Card WinningCard { get; }

        public IReadOnlyList<YakuResult> Yaku { get; }

        /// <summary>ドラの枚数（翻数に加算済み）。</summary>
        public int DoraCount { get; }

        /// <summary>役とドラを合計した翻数。</summary>
        public int Han { get; }

        /// <summary>役満役が成立しているか。</summary>
        public bool IsYakuman { get; }

        public bool IsTsumo => LoserSeat < 0;

        public WinResult(
            int winnerSeat,
            int loserSeat,
            Card winningCard,
            IReadOnlyList<YakuResult> yaku,
            int doraCount,
            int han,
            bool isYakuman)
        {
            WinnerSeat = winnerSeat;
            LoserSeat = loserSeat;
            WinningCard = winningCard;
            Yaku = yaku;
            DoraCount = doraCount;
            Han = han;
            IsYakuman = isYakuman;
        }

        public override string ToString()
            => $"seat{WinnerSeat} {(IsTsumo ? "ツモ" : $"ロン(from seat{LoserSeat})")} {WinningCard} {Han}翻";
    }
}
