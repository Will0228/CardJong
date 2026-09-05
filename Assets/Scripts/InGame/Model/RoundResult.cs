using System.Collections.Generic;

namespace CardJong.InGame.Model
{
    /// <summary>1 局の結果。上がりまたは流局。</summary>
    public sealed class RoundResult
    {
        /// <summary>上がりの内容。流局の場合は null。</summary>
        public WinResult Win { get; }

        public bool IsDrawGame => Win == null;

        /// <summary>流局時のテンパイ者の席。上がりの場合は空。</summary>
        public IReadOnlyList<int> TenpaiSeats { get; }

        /// <summary>席ごとの点数増減。</summary>
        public IReadOnlyList<int> ScoreDeltas { get; }

        /// <summary>親が連荘するか。</summary>
        public bool IsDealerRepeat { get; }

        public RoundResult(
            WinResult win,
            IReadOnlyList<int> tenpaiSeats,
            IReadOnlyList<int> scoreDeltas,
            bool isDealerRepeat)
        {
            Win = win;
            TenpaiSeats = tenpaiSeats;
            ScoreDeltas = scoreDeltas;
            IsDealerRepeat = isDealerRepeat;
        }
    }
}
