using System;
using System.Collections.Generic;
using CardJong.Core.Commands;
using CardJong.InGame.Actions;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using CardJong.InGame.Rules;

namespace CardJong.InGame.Commands
{
    /// <summary>
    /// コマンドの生成口。依存の注入をここに閉じ込め、State 側は「何をするか」だけを書けるようにする。
    /// </summary>
    public sealed class GameCommandFactory
    {
        private readonly InGameModel _model;
        private readonly IHandAnalyzer _handAnalyzer;
        private readonly IScoreCalculator _scoreCalculator;

        public GameCommandFactory(InGameModel model, IHandAnalyzer handAnalyzer, IScoreCalculator scoreCalculator)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _handAnalyzer = handAnalyzer ?? throw new ArgumentNullException(nameof(handAnalyzer));
            _scoreCalculator = scoreCalculator ?? throw new ArgumentNullException(nameof(scoreCalculator));
        }

        public DrawCardCommand CreateDraw(int seat) => new(_model, seat);

        public DiscardCardCommand CreateDiscard(int seat, Card card, bool declareRiichi)
            => new(_model, seat, card, declareRiichi);

        public TsumoCommand CreateTsumo(int seat) => new(_model, _handAnalyzer, _scoreCalculator, seat);

        public RonCommand CreateRon(int seat, int fromSeat, Card card)
            => new(_model, _handAnalyzer, _scoreCalculator, seat, fromSeat, card);

        public PonCommand CreatePon(int seat, int fromSeat, Card claimedCard, IReadOnlyList<Card> usedCards)
            => new(_model, seat, fromSeat, claimedCard, usedCards);

        public ChiCommand CreateChi(int seat, int fromSeat, Card claimedCard, IReadOnlyList<Card> usedCards)
            => new(_model, seat, fromSeat, claimedCard, usedCards);

        public PassCommand CreatePass(int seat, bool wasRonAvailable) => new(_model, seat, wasRonAvailable);

        /// <summary>思考時間の結果（行動パターン）をコマンドに変換する。</summary>
        public IGameCommand CreateFromTurnAction(int seat, TurnAction action) => action.Type switch
        {
            TurnActionType.Tsumo => CreateTsumo(seat),
            TurnActionType.Riichi => CreateDiscard(seat, action.Card, declareRiichi: true),
            _ => CreateDiscard(seat, action.Card, declareRiichi: false),
        };

        /// <summary>宣言（待機パターン）をコマンドに変換する。</summary>
        public IGameCommand CreateFromClaim(ClaimDeclaration declaration, DiscardInfo discard, bool wasRonAvailable)
            => declaration.Type switch
            {
                ClaimType.Ron => CreateRon(declaration.Seat, discard.Seat, discard.Card),
                ClaimType.Pon => CreatePon(declaration.Seat, discard.Seat, discard.Card, declaration.UsedCards),
                ClaimType.Chi => CreateChi(declaration.Seat, discard.Seat, discard.Card, declaration.UsedCards),
                _ => CreatePass(declaration.Seat, wasRonAvailable),
            };
    }
}
