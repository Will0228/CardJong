using System.Collections.Generic;

namespace CardJong.InGame.Model
{
    /// <summary>最終順位 1 人分。</summary>
    /// <param name="Seat">席。</param>
    /// <param name="Score">最終点数。</param>
    /// <param name="Rank">順位（1 始まり）。</param>
    public sealed record PlayerFinalScore(int Seat, int Score, int Rank)
    {
        public override string ToString() => $"{Rank}位 seat{Seat} {Score}点";
    }

    /// <summary>ゲーム全体の結果。</summary>
    /// <param name="Rankings">順位順に並んだ最終スコア。</param>
    public sealed record GameResult(IReadOnlyList<PlayerFinalScore> Rankings);
}
