using System.Collections.Generic;

namespace CardJong.InGame.Model
{
    /// <summary>1 局の結果。上がりまたは流局。</summary>
    /// <param name="Win">上がりの内容。流局の場合は null。</param>
    /// <param name="TenpaiSeats">流局時のテンパイ者の席。上がりの場合は空。</param>
    /// <param name="ScoreDeltas">席ごとの点数増減。</param>
    /// <param name="IsDealerRepeat">親が連荘するか。</param>
    public sealed record RoundResult(
        WinResult Win,
        IReadOnlyList<int> TenpaiSeats,
        IReadOnlyList<int> ScoreDeltas,
        bool IsDealerRepeat)
    {
        public bool IsDrawGame => Win == null;
    }
}
