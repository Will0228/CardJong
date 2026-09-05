using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CardJong.Core.Commands
{
    /// <summary>
    /// コマンドの実行口。実行前の検証・履歴の記録・実行通知をまとめて受け持つ。
    /// </summary>
    public interface IGameCommandInvoker
    {
        /// <summary>コマンドが実行された直後に発火する。View 側の演出トリガとして使う。</summary>
        Observable<IGameCommand> OnCommandExecuted { get; }

        /// <summary>実行済みコマンドの履歴（古い順）。</summary>
        IReadOnlyList<IGameCommand> History { get; }

        /// <summary>
        /// コマンドを実行する。<see cref="IGameCommand.CanExecute"/> が false の場合は実行せず false を返す。
        /// </summary>
        UniTask<bool> ExecuteAsync(IGameCommand command, CancellationToken cancellationToken);

        /// <summary>履歴を破棄する。局の開始時などに呼ぶ。</summary>
        void ClearHistory();
    }
}
