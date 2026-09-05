using System.Threading;
using CardJong.Core;
using CardJong.Core.Commands;
using CardJong.InGame.Commands;
using CardJong.InGame.Model;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>
    /// カードを引く（行動パターン）。
    /// 生き山を引き切っていた場合はここで流局にする。
    /// </summary>
    public sealed class DrawState : InGameStateBase
    {
        private readonly InGameModel _model;
        private readonly GameCommandFactory _commandFactory;
        private readonly IGameCommandInvoker _commandInvoker;

        [Inject]
        public DrawState(
            IStateSwitcher<InGameStateType> stateSwitcher,
            InGameModel model,
            GameCommandFactory commandFactory,
            IGameCommandInvoker commandInvoker) : base(stateSwitcher)
        {
            _model = model;
            _commandFactory = commandFactory;
            _commandInvoker = commandInvoker;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            // 生き山を引き切ったら流局
            if (_model.Wall.IsLiveWallEmpty)
            {
                RequestTransition(InGameStateType.RoundEnd);
                return;
            }

            var seat = _model.CurrentSeat.CurrentValue;
            await _commandInvoker.ExecuteAsync(_commandFactory.CreateDraw(seat), cancellationToken);

            RequestTransition(InGameStateType.PlayerAction);
        }
    }
}
