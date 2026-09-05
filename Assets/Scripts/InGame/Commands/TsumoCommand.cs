using System.Threading;
using CardJong.Core.Commands;
using CardJong.InGame.Model;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Commands
{
    /// <summary>ツモ上がりを宣言する。</summary>
    public sealed class TsumoCommand : IGameCommand
    {
        private readonly InGameModel _model;
        private readonly IHandAnalyzer _handAnalyzer;
        private readonly IScoreCalculator _scoreCalculator;
        private readonly int _seat;

        public TsumoCommand(
            InGameModel model,
            IHandAnalyzer handAnalyzer,
            IScoreCalculator scoreCalculator,
            int seat)
        {
            _model = model;
            _handAnalyzer = handAnalyzer;
            _scoreCalculator = scoreCalculator;
            _seat = seat;
        }

        public bool CanExecute()
        {
            if (!_model.CanDeclareTsumo) return false;

            var player = _model.GetPlayer(_seat);
            if (player.Cards.LastDrawnCard == null) return false;

            return _handAnalyzer.IsWinningHand(player.Cards.ConcealedCards, player.Cards.Melds);
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            var player = _model.GetPlayer(_seat);
            var winningCard = player.Cards.LastDrawnCard;

            var win = _scoreCalculator.Evaluate(_model, _seat, loserSeat: -1, winningCard);
            _model.SetPendingWin(win);
            return UniTask.CompletedTask;
        }
    }
}
