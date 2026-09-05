using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace CardJong.Core.Commands
{
    /// <inheritdoc cref="IGameCommandInvoker"/>
    public sealed class GameCommandInvoker : IGameCommandInvoker, IDisposable
    {
        private readonly List<IGameCommand> _history = new();
        private readonly Subject<IGameCommand> _onCommandExecuted = new();

        private bool _isDisposed;

        public Observable<IGameCommand> OnCommandExecuted => _onCommandExecuted;

        public IReadOnlyList<IGameCommand> History => _history;

        public async UniTask<bool> ExecuteAsync(IGameCommand command, CancellationToken cancellationToken)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (_isDisposed) throw new ObjectDisposedException(nameof(GameCommandInvoker));

            if (!command.CanExecute())
            {
                // 不正なコマンドはゲームロジック側のバグなので握り潰さずに知らせる。
                Debug.LogError($"[Command] 実行条件を満たしていません: {command.GetType().Name}");
                return false;
            }

            await command.ExecuteAsync(cancellationToken);

            _history.Add(command);
            _onCommandExecuted.OnNext(command);
            return true;
        }

        public void ClearHistory()
        {
            _history.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _history.Clear();
            _onCommandExecuted.Dispose();
        }
    }
}
