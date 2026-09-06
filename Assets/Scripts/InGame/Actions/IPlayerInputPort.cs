using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CardJong.InGame.Actions
{
    /// <summary>
    /// 人間プレイヤーの入力境界（View 側から見た口）。
    /// UI はここを購読して選択 UI を出し、選ばれた結果を Submit で返す。
    /// </summary>
    public interface IPlayerInputPort
    {
        /// <summary>打牌 / ツモ上がりの選択を求められた。</summary>
        Observable<TurnDecisionContext> OnTurnDecisionRequested { get; }

        /// <summary>ロン / ポン / チーの選択を求められた。</summary>
        Observable<ClaimDecisionContext> OnClaimDecisionRequested { get; }

        /// <summary>選択が確定または打ち切られた。選択 UI を閉じるのに使う。</summary>
        Observable<Unit> OnDecisionClosed { get; }

        /// <summary>手番の行動を確定する。</summary>
        void SubmitTurnAction(TurnAction action);

        /// <summary>鳴きの宣言を確定する。</summary>
        void SubmitClaim(ClaimDeclaration declaration);
    }

    /// <summary>
    /// 人間プレイヤーの入力境界（<see cref="IPlayerAgent"/> 側から見た口）。
    /// </summary>
    public interface IPlayerInputRequester
    {
        UniTask<TurnAction> RequestTurnActionAsync(TurnDecisionContext context, CancellationToken cancellationToken);

        UniTask<ClaimDeclaration> RequestClaimAsync(ClaimDecisionContext context, CancellationToken cancellationToken);
    }
}
