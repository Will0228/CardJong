using System.Threading;
using CardJong.Core;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>
    /// 局の開始。山を作り、各プレイヤーに 13 枚配り、ドラ表示札を 1 枚めくる。
    /// 残りから生き山を確保し、それ以降は死に山として使わない。
    /// </summary>
    public sealed class RoundStartState : InGameStateBase
    {
        private readonly InGameModel _model;
        private readonly IRandomService _random;
        private readonly IInGamePresentation _presentation;

        [Inject]
        public RoundStartState(
            IStateSwitcher<InGameStateType> stateSwitcher,
            InGameModel model,
            IRandomService random,
            IInGamePresentation presentation) : base(stateSwitcher)
        {
            _model = model;
            _random = random;
            _presentation = presentation;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            var deck = CardDeckFactory.CreateFullDeck();
            CardDeckFactory.Shuffle(deck, _random);

            _model.Wall.Reset(deck);
            _model.ClearPendingWin();
            _model.ClearLastDiscard();
            _model.SetCanDeclareTsumo(false);

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                var player = _model.GetPlayer(seat);
                player.ResetForNewRound();
                player.Cards.DealCards(_model.Wall.DealCards(InGameModel.HandSize));
            }

            _model.Wall.RevealDoraIndicator();
            _model.Wall.SetLiveWall(_model.LiveWallCount);

            // 親から順に進める
            _model.SetCurrentSeat(_model.DealerSeat.CurrentValue);

            await _presentation.ShowRoundStartAsync(
                _model.RoundNumber.CurrentValue,
                _model.Honba.CurrentValue,
                cancellationToken);

            RequestTransition(InGameStateType.Draw);
        }
    }
}
