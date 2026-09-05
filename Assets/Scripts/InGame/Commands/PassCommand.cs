using System.Threading;
using CardJong.Core.Commands;
using CardJong.InGame.Model;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Commands
{
    /// <summary>
    /// 捨て札に対して何もしない。
    /// 見逃しをフリテンとして記録するため、履歴に残す意味でコマンドにしている。
    /// </summary>
    public sealed class PassCommand : IGameCommand
    {
        private readonly InGameModel _model;
        private readonly int _seat;
        private readonly bool _wasRonAvailable;

        public PassCommand(InGameModel model, int seat, bool wasRonAvailable)
        {
            _model = model;
            _seat = seat;
            _wasRonAvailable = wasRonAvailable;
        }

        public bool CanExecute() => true;

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            // 上がり札を見逃した場合は一時フリテンになり、次のツモまでロンできない。
            if (_wasRonAvailable)
            {
                _model.GetPlayer(_seat).SetTemporaryFuriten(true);
            }

            return UniTask.CompletedTask;
        }
    }
}
