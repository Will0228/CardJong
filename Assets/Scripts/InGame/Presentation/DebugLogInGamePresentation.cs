using System.Text;
using System.Threading;
using CardJong.InGame.Model;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardJong.InGame.Presentation
{
    /// <summary>
    /// View ができるまでの仮実装。ログを出すだけで待ち時間は発生させない。
    /// 実際の演出を作るときは、このクラスを差し替えて await を実アニメーションに繋ぐ。
    /// </summary>
    public sealed class DebugLogInGamePresentation : IInGamePresentation
    {
        public UniTask ShowGameStartAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[InGame] ゲーム開始");
            return UniTask.CompletedTask;
        }

        public UniTask ShowDealerDecisionAsync(int dealerSeat, CancellationToken cancellationToken)
        {
            Debug.Log($"[InGame] 親は seat{dealerSeat}");
            return UniTask.CompletedTask;
        }

        public UniTask ShowRoundStartAsync(int roundNumber, int honba, CancellationToken cancellationToken)
        {
            Debug.Log($"[InGame] 第{roundNumber}局 {honba}本場 開始");
            return UniTask.CompletedTask;
        }

        public UniTask ShowWinAsync(WinResult win, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            builder.Append("[InGame] 和了: ").Append(win);

            for (var i = 0; i < win.Yaku.Count; i++)
            {
                builder.Append(" / ").Append(win.Yaku[i]);
            }

            if (win.DoraCount > 0)
            {
                builder.Append(" / ドラ").Append(win.DoraCount);
            }

            Debug.Log(builder.ToString());
            return UniTask.CompletedTask;
        }

        public UniTask ShowRoundResultAsync(RoundResult result, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            builder.Append(result.IsDrawGame ? "[InGame] 流局" : "[InGame] 局終了");

            for (var seat = 0; seat < result.ScoreDeltas.Count; seat++)
            {
                builder.Append($" seat{seat}:{result.ScoreDeltas[seat]:+#;-#;0}");
            }

            if (result.IsDealerRepeat)
            {
                builder.Append(" (連荘)");
            }

            Debug.Log(builder.ToString());
            return UniTask.CompletedTask;
        }

        public UniTask ShowGameResultAsync(GameResult result, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder("[InGame] ゲーム終了");
            for (var i = 0; i < result.Rankings.Count; i++)
            {
                builder.Append(' ').Append(result.Rankings[i]);
            }

            Debug.Log(builder.ToString());
            return UniTask.CompletedTask;
        }
    }
}
