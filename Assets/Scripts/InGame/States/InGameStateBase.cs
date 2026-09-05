using System;
using System.Threading;
using CardJong.Core;
using CardJong.InGame.Actions;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.States
{
    /// <summary>インゲームのステートの基底。制限時間つきの意思決定をここにまとめている。</summary>
    public abstract class InGameStateBase : AsyncStateBase<InGameStateType>
    {
        protected InGameStateBase(IStateSwitcher<InGameStateType> stateSwitcher) : base(stateSwitcher)
        {
        }

        /// <summary>
        /// 制限時間つきで手番の行動を決めさせる。時間切れの場合は fallback を返す。
        /// </summary>
        protected static UniTask<TurnAction> DecideTurnActionAsync(
            IPlayerAgent agent,
            TurnDecisionContext context,
            TurnAction fallback,
            CancellationToken cancellationToken)
            => DecideWithTimeoutAsync(
                token => agent.DecideTurnActionAsync(context, token),
                fallback,
                context.TimeLimitSeconds,
                cancellationToken);

        /// <summary>
        /// 制限時間つきで宣言を決めさせる。時間切れの場合は fallback を返す。
        /// </summary>
        protected static UniTask<ClaimDeclaration> DecideClaimAsync(
            IPlayerAgent agent,
            ClaimDecisionContext context,
            ClaimDeclaration fallback,
            CancellationToken cancellationToken)
            => DecideWithTimeoutAsync(
                token => agent.DecideClaimAsync(context, token),
                fallback,
                context.TimeLimitSeconds,
                cancellationToken);

        private static async UniTask<T> DecideWithTimeoutAsync<T>(
            Func<CancellationToken, UniTask<T>> decide,
            T fallback,
            float timeLimitSeconds,
            CancellationToken cancellationToken)
        {
            if (timeLimitSeconds <= 0f)
            {
                return await decide(cancellationToken);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfterSlim(TimeSpan.FromSeconds(timeLimitSeconds));

            try
            {
                return await decide(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // 制限時間切れ。ステート自体の中断は握り潰さずに伝播させる。
                return fallback;
            }
        }
    }
}
