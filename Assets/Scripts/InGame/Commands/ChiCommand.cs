using System.Collections.Generic;
using System.Threading;
using CardJong.Core.Commands;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Commands
{
    /// <summary>
    /// チー。上家の捨て札と手札の同一マーク連続札で順子を作る。
    /// 3 枚組には連続 2 枚、4 枚組には連続 3 枚が手札に必要。
    /// </summary>
    public sealed class ChiCommand : IGameCommand
    {
        private readonly InGameModel _model;
        private readonly int _seat;
        private readonly int _fromSeat;
        private readonly Card _claimedCard;
        private readonly IReadOnlyList<Card> _usedCards;

        public ChiCommand(InGameModel model, int seat, int fromSeat, Card claimedCard, IReadOnlyList<Card> usedCards)
        {
            _model = model;
            _seat = seat;
            _fromSeat = fromSeat;
            _claimedCard = claimedCard;
            _usedCards = usedCards;
        }

        public string DebugName => $"Chi(seat={_seat}, from={_fromSeat}, {_claimedCard} x{_usedCards.Count + 1})";

        public bool CanExecute()
        {
            // チーは上家からのみ
            if (_model.GetUpperSeat(_seat) != _fromSeat) return false;

            var player = _model.GetPlayer(_seat);
            var remaining = new List<Card>(player.ConcealedCards);

            for (var i = 0; i < _usedCards.Count; i++)
            {
                // 鳴いた組は同一マークで固定される
                if (_usedCards[i].Suit != _claimedCard.Suit) return false;

                var index = remaining.IndexOf(_usedCards[i]);
                if (index < 0) return false;

                remaining.RemoveAt(index);
            }

            return true;
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            var player = _model.GetPlayer(_seat);
            _model.GetPlayer(_fromSeat).RemoveLastDiscard();

            var cards = new List<Card>(_usedCards.Count + 1);
            for (var i = 0; i < _usedCards.Count; i++)
            {
                player.RemoveFromHand(_usedCards[i]);
                cards.Add(_usedCards[i]);
            }

            cards.Add(_claimedCard);

            player.AddMeld(new Meld(MeldType.Chi, CardRunUtility.OrderAsRun(cards), _claimedCard, _fromSeat));
            player.SortHand();

            _model.SetCanDeclareTsumo(false);
            _model.ClearLastDiscard();
            return UniTask.CompletedTask;
        }
    }
}
