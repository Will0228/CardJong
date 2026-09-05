using System;
using System.Collections.Generic;
using System.Threading;
using CardJong.Core;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>
    /// 局の終了。点数を移動し、連荘するかを決めて次局かゲーム終了へ進める。
    /// 上がりが無いままここに来た場合は流局として扱う。
    /// </summary>
    public sealed class RoundEndState : InGameStateBase
    {
        private static readonly int[] EmptySeats = Array.Empty<int>();

        private readonly InGameModel _model;
        private readonly IHandAnalyzer _handAnalyzer;
        private readonly IScoreCalculator _scoreCalculator;
        private readonly IInGamePresentation _presentation;

        [Inject]
        public RoundEndState(
            IStateSwitcher<InGameStateType> stateSwitcher,
            InGameModel model,
            IHandAnalyzer handAnalyzer,
            IScoreCalculator scoreCalculator,
            IInGamePresentation presentation) : base(stateSwitcher)
        {
            _model = model;
            _handAnalyzer = handAnalyzer;
            _scoreCalculator = scoreCalculator;
            _presentation = presentation;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            var result = _model.PendingWin != null ? BuildWinResult() : BuildDrawGameResult();

            ApplyScoreDeltas(result.ScoreDeltas);
            _model.SetRoundResult(result);
            _model.ClearPendingWin();

            await _presentation.ShowRoundResultAsync(result, cancellationToken);

            _model.BeginNextRound(result.IsDealerRepeat);

            RequestTransition(_model.IsGameOver
                ? InGameStateType.GameEnd
                : InGameStateType.RoundStart);
        }

        private RoundResult BuildWinResult()
        {
            var win = _model.PendingWin;
            var deltas = _scoreCalculator.CalculateWinDeltas(_model, win);

            // 親が上がれば連荘
            var isDealerRepeat = win.WinnerSeat == _model.DealerSeat.CurrentValue;
            return new RoundResult(win, EmptySeats, deltas, isDealerRepeat);
        }

        private RoundResult BuildDrawGameResult()
        {
            var tenpaiSeats = CollectTenpaiSeats();
            var deltas = _scoreCalculator.CalculateDrawGameDeltas(_model, tenpaiSeats);

            // 親がテンパイなら連荘
            var isDealerRepeat = tenpaiSeats.Contains(_model.DealerSeat.CurrentValue);
            return new RoundResult(null, tenpaiSeats, deltas, isDealerRepeat);
        }

        private List<int> CollectTenpaiSeats()
        {
            var seats = new List<int>(_model.PlayerCount);

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                var player = _model.GetPlayer(seat);
                if (_handAnalyzer.IsTenpai(player.ConcealedCards, player.Melds))
                {
                    seats.Add(seat);
                }
            }

            return seats;
        }

        private void ApplyScoreDeltas(IReadOnlyList<int> deltas)
        {
            for (var seat = 0; seat < deltas.Count; seat++)
            {
                _model.GetPlayer(seat).AddScore(deltas[seat]);
            }
        }
    }
}
