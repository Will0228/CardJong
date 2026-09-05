using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;

namespace CardJong.InGame.Rules
{
    /// <summary>役の判定と点数移動の計算。</summary>
    public interface IScoreCalculator
    {
        /// <summary>
        /// 上がりの内容を評価する。
        /// </summary>
        /// <param name="loserSeat">放銃者の席。ツモ上がりの場合は -1。</param>
        WinResult Evaluate(InGameModel model, int winnerSeat, int loserSeat, Card winningCard);

        /// <summary>上がり時の席ごとの点数増減。合計は 0 になる。</summary>
        int[] CalculateWinDeltas(InGameModel model, WinResult win);

        /// <summary>流局時（ノーテン罰符）の席ごとの点数増減。合計は 0 になる。</summary>
        int[] CalculateDrawGameDeltas(InGameModel model, IReadOnlyList<int> tenpaiSeats);
    }
}
