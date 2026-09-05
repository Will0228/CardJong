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

        public bool CanExecute()
        {
            var player = _model.GetPlayer(_seat);
            if (_declareRiichi && !player.Cards.IsMenzen) return false;

            return player.Cards.CountSameCardsInHand(_card) > 0;
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            var player = _model.GetPlayer(_seat);

            if (_declareRiichi)
            {
                player.Status.DeclareRiichi();
            }

            player.Cards.RemoveFromHand(_card);
            player.Cards.SortHand();
            player.Cards.AddDiscard(_card);

            _model.SetLastDiscard(new DiscardInfo(_card, _seat));
            _model.SetCanDeclareTsumo(false);
            return UniTask.CompletedTask;
        }
    }
}
