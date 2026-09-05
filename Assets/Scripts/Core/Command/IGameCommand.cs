using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardJong.Core.Commands
{
    /// <summary>
    /// ゲーム状態を変更する 1 操作。
    /// 「誰が」「何を」するかをオブジェクトとして表現し、実行を <see cref="IGameCommandInvoker"/> に委ねる。
    /// </summary>
    /// <remarks>
    /// コマンドは実行履歴として残るため、リプレイ・観戦・通信同期の単位としても使える。
    /// </remarks>
    public interface IGameCommand
    {
        /// <summary>現在のゲーム状態でこのコマンドが実行可能か。</summary>
        bool CanExecute();

        /// <summary>コマンドを実行してゲーム状態を変更する。</summary>
        UniTask ExecuteAsync(CancellationToken cancellationToken);
    }
}
