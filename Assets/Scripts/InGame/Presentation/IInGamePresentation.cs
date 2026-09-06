using System.Threading;
using CardJong.InGame.Model;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Presentation
{
    /// <summary>
    /// 演出・画面表示の窓口。State はここを await するだけで、実際の見た目には関与しない。
    /// </summary>
    public interface IInGamePresentation
    {
        /// <summary>ゲーム開始。</summary>
        UniTask ShowGameStartAsync(CancellationToken cancellationToken);

        /// <summary>親決定の演出。</summary>
        UniTask ShowDealerDecisionAsync(int dealerSeat, CancellationToken cancellationToken);

        /// <summary>配牌・ドラめくりの演出。</summary>
        UniTask ShowRoundStartAsync(int roundNumber, int honba, CancellationToken cancellationToken);

        /// <summary>誰かが上がったときの演出画面。</summary>
        UniTask ShowWinAsync(WinResult win, CancellationToken cancellationToken);

        /// <summary>局の結果表示（点数移動・流局）。</summary>
        UniTask ShowRoundResultAsync(RoundResult result, CancellationToken cancellationToken);

        /// <summary>ゲーム終了画面。</summary>
        UniTask ShowGameResultAsync(GameResult result, CancellationToken cancellationToken);
    }
}
