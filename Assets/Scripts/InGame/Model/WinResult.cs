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
    /// <param name="WinnerSeat">上がったプレイヤーの席。</param>
    /// <param name="LoserSeat">放銃したプレイヤーの席。ツモ上がりの場合は -1。</param>
    /// <param name="WinningCard">14 枚目になったカード。</param>
    /// <param name="Yaku">成立した役。</param>
    /// <param name="DoraCount">ドラの枚数（翻数に加算済み）。</param>
    /// <param name="Han">役とドラを合計した翻数。</param>
    /// <param name="IsYakuman">役満役が成立しているか。</param>
    public sealed record WinResult(
        int WinnerSeat,
        int LoserSeat,
        Card WinningCard,
        IReadOnlyList<YakuResult> Yaku,
        int DoraCount,
        int Han,
        bool IsYakuman)
    {
        public bool IsTsumo => LoserSeat < 0;

        public override string ToString()
            => $"seat{WinnerSeat} {(IsTsumo ? "ツモ" : $"ロン(from seat{LoserSeat})")} {WinningCard} {Han}翻";
    }
}
