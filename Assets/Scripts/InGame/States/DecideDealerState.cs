using System.Threading;
using CardJong.Core;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>
    /// 親決定。
    /// </summary>
    /// <remarks>TODO: 現状は無作為に決めている。カードめくりなどの決定方法を入れる場合はここを差し替える。</remarks>
    public sealed class DecideDealerState : InGameStateBase
    {
        private readonly InGameModel _model;
        private readonly IRandomService _random;
        private readonly IInGamePresentation _presentation;

        [Inject]
        public DecideDealerState(
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
            var dealerSeat = _random.Next(_model.PlayerCount);

            _model.SetDealer(dealerSeat);
            _model.SetCurrentSeat(dealerSeat);

            await _presentation.ShowDealerDecisionAsync(dealerSeat, cancellationToken);
            RequestTransition(InGameStateType.RoundStart);
        }
    }
}
