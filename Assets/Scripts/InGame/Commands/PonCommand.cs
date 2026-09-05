using System.Collections.Generic;
using System.Threading;
using CardJong.Core.Commands;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Commands
{
    /// <summary>
    /// ポン。他家の捨て札と手札の同一カードで刻子を作る。
    /// 3 枚組には同一カード 2 枚、4 枚組には 3 枚が手札に必要。
    /// </summary>
    public sealed class PonCommand : IGameCommand
    {
        private readonly InGameModel _model;
        private readonly int _seat;
        private readonly int _fromSeat;
        private readonly Card _claimedCard;
        private readonly IReadOnlyList<Card> _usedCards;

        public PonCommand(InGameModel model, int seat, int fromSeat, Card claimedCard, IReadOnlyList<Card> usedCards)
        {
            _model = model;
            _seat = seat;
            _fromSeat = fromSeat;
            _claimedCard = claimedCard;
            _usedCards = usedCards;
        }

        public string DebugName => $"Pon(seat={_seat}, from={_fromSeat}, {_claimedCard} x{_usedCards.Count + 1})";

        public bool CanExecute()
        {
            // 鳴いた組は同一マークで固定されるので、使う札はすべて捨て札と同一カードでなければならない。
            for (var i = 0; i < _usedCards.Count; i++)
            {
                if (_usedCards[i] != _claimedCard) return false;
            }

            return _model.GetPlayer(_seat).CountInHand(_claimedCard) >= _usedCards.Count;
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

            player.AddMeld(new Meld(MeldType.Pon, cards, _claimedCard, _fromSeat));
            player.SortHand();

            _model.SetCanDeclareTsumo(false);
            _model.ClearLastDiscard();
            return UniTask.CompletedTask;
        }
    }
}
