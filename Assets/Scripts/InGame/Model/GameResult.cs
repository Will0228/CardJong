using System.Collections.Generic;

namespace CardJong.InGame.Model
{
    /// <summary>最終順位 1 人分。</summary>
    public readonly struct PlayerFinalScore
    {
        public int Seat { get; }

        public int Score { get; }

        /// <summary>順位（1 始まり）。</summary>
        public int Rank { get; }

        public PlayerFinalScore(int seat, int score, int rank)
        {
            Seat = seat;
            Score = score;
            Rank = rank;
        }

        public override string ToString() => $"{Rank}位 seat{Seat} {Score}点";
    }

    /// <summary>ゲーム全体の結果。</summary>
    /// <param name="Rankings">順位順に並んだ最終スコア。</param>
    public sealed record GameResult(IReadOnlyList<PlayerFinalScore> Rankings);
}
