using System.Collections.Generic;
using System.Threading;
using CardJong.Core.Commands;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Commands
{
    /// <summary>他家の捨て札で上がる。</summary>
    public sealed class RonCommand : IGameCommand
    {
        private readonly InGameModel _model;
        private readonly IHandAnalyzer _handAnalyzer;
        private readonly IScoreCalculator _scoreCalculator;
        private readonly int _seat;
        private readonly int _fromSeat;
        private readonly Card _card;

        public RonCommand(
            InGameModel model,
            IHandAnalyzer handAnalyzer,
            IScoreCalculator scoreCalculator,
            int seat,
            int fromSeat,
            Card card)
        {
            _model = model;
            _handAnalyzer = handAnalyzer;
            _scoreCalculator = scoreCalculator;
            _seat = seat;
            _fromSeat = fromSeat;
            _card = card;
        }

        public string DebugName => $"Ron(seat={_seat}, from={_fromSeat}, {_card})";

        public bool CanExecute()
        {
            var player = _model.GetPlayer(_seat);
            if (player.IsTemporaryFuriten) return false;

            var hand = new List<Card>(player.ConcealedCards.Count + 1);
            hand.AddRange(player.ConcealedCards);
            hand.Add(_card);

            return _handAnalyzer.IsWinningHand(hand, player.Melds);
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            // 上がり札は放銃者の河から取り除く。
            _model.GetPlayer(_fromSeat).RemoveLastDiscard();

            var win = _scoreCalculator.Evaluate(_model, _seat, _fromSeat, _card);
            _model.SetPendingWin(win);
            _model.ClearLastDiscard();
            return UniTask.CompletedTask;
        }
    }
}
