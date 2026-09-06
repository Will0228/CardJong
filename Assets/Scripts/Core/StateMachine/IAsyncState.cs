using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardJong.Core
{
    /// <summary>
    /// ステートマシンが回す 1 ステート。
    /// Enter -> Exit の順に必ず 1 度ずつ呼ばれる。
    /// </summary>
    /// <remarks>
    /// ステートの処理は <see cref="EnterAsync"/> に書き、次のステートへ移りたくなった時点で
    /// <see cref="IStateSwitcher{TKey}.RequestTransition"/> を呼ぶ。
    /// EnterAsync が完了しても要求が来るまではそのステートに留まるので、
    /// 「ボタンが押されたら遷移」のような入力起点の遷移は購読の中から要求すればよい。
    /// </remarks>
    public interface IAsyncState<TKey> : IDisposable where TKey : struct, Enum
    {
        /// <summary>ステートの処理。</summary>
        UniTask EnterAsync(CancellationToken cancellationToken);

        /// <summary>ステート終了時の後始末。中断された場合でも必ず呼ばれる。</summary>
        UniTask ExitAsync(CancellationToken cancellationToken);
    }
}
