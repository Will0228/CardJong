using System.Threading;
using CardJong.Core.Commands;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Commands
{
    /// <summary>手札から 1 枚捨てる。リーチ宣言を伴うこともある。</summary>
    public sealed class DiscardCardCommand : IGameCommand
    {
        private readonly InGameModel _model;
        private readonly int _seat;
        private readonly Card _card;
        private readonly bool _declareRiichi;

        public DiscardCardCommand(InGameModel model, int seat, Card card, bool declareRiichi)
        {
            _model = model;
            _seat = seat;
            _card = card;
            _declareRiichi = declareRiichi;
        }

        public string DebugName => $"Discard(seat={_seat}, {_card}{(_declareRiichi ? ", riichi" : string.Empty)})";

        public bool CanExecute()
        {
            var player = _model.GetPlayer(_seat);
            if (_declareRiichi && !player.IsMenzen) return false;

            return player.CountInHand(_card) > 0;
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            var player = _model.GetPlayer(_seat);

            if (_declareRiichi)
            {
                player.DeclareRiichi();
            }

            player.RemoveFromHand(_card);
            player.SortHand();
            player.AddDiscard(_card);

            _model.SetLastDiscard(new DiscardInfo(_card, _seat));
            _model.SetCanDeclareTsumo(false);
            return UniTask.CompletedTask;
        }
    }
}
