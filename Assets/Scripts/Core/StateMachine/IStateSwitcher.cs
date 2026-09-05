using System;
using R3;

namespace CardJong.Core
{
    /// <summary>
    /// ステートの遷移要求と、現在のステートの参照。
    /// ステートの切り替えはすべてこのインターフェース経由で行う。
    /// </summary>
    public interface IStateSwitcher<TKey> where TKey : struct, Enum
    {
        /// <summary>現在のステート。</summary>
        ReadOnlyReactiveProperty<TKey> CurrentState { get; }

        /// <summary>ステートに入った瞬間に発火する。</summary>
        Observable<TKey> OnStateEntered { get; }

        /// <summary>
        /// 指定ステートへ遷移する。
        /// 実行中のステートは CancellationToken がキャンセルされ、ExitAsync を通してから切り替わる。
        /// </summary>
        void RequestTransition(TKey nextState);

        /// <summary>
        /// ステートマシンのループを終了する。遷移先が無い最終ステートから呼ぶ。
        /// </summary>
        void RequestExit();
    }
}
