using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CardJong.Core.Commands
{
    /// <summary>
    /// コマンドの実行口。実行前の検証・履歴の記録・実行通知をまとめて受け持つ。
    /// </summary>
    /// <remarks>
    /// 使う側が 1 つずつ持つ想定で、履歴も通知もその実体に閉じる。
    /// ゲーム全体の履歴を集めたくなったら、それ用の窓口を別に立てる。
    /// </remarks>
    public interface IGameCommandInvoker
    {
        /// <summary>この実行口で実行されたコマンドの直後に発火する。演出トリガとして使う。</summary>
        Observable<IGameCommand> OnCommandExecuted { get; }

        /// <summary>この実行口で実行したコマンドの履歴（古い順）。</summary>
        IReadOnlyList<IGameCommand> History { get; }

        /// <summary>
        /// コマンドを実行する。<see cref="IGameCommand.CanExecute"/> が false の場合は実行せず false を返す。
        /// </summary>
        UniTask<bool> ExecuteAsync(IGameCommand command, CancellationToken cancellationToken);

        /// <summary>履歴を破棄する。同じ実行口を使い回して区切りを付けたいときに呼ぶ。</summary>
        void ClearHistory();
    }
}
