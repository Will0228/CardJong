using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.States
{
    /// <summary>
    /// 制限時間つきの意思決定。
    /// PlayerActionState（手番の行動）や ClaimWaitState（鳴きの宣言）など、一部のステートだけが
    /// 必要とする機能なので、すべてのステートが継承する基底には置かず、ここに独立させている。
    /// </summary>
    public static class TimeLimitedDecision
    {
        /// <summary>
        /// 制限時間つきで意思決定させる。時間切れの場合は fallback を返す。
        /// </summary>
        public static async UniTask<T> RunAsync<T>(
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
