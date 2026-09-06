using System.Collections.Generic;
using System.Threading;
using CardJong.Core;
using CardJong.Core.Commands;
using CardJong.InGame.Actions;
using CardJong.InGame.Cards;
using CardJong.InGame.Commands;
using CardJong.InGame.Model;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>
    /// 思考時間（行動パターン）。手番のプレイヤーが「1 枚捨てる」か「ツモ上がり」を選ぶ。
    /// 鳴いた直後もこのステートに入るが、その場合はツモ上がりを選べない。
    /// </summary>
    public sealed class PlayerActionState : AsyncStateBase<InGameStateType>
    {
        private readonly InGameModel _model;
        private readonly InGameSettings _settings;
        private readonly IPlayerAgentRegistry _agentRegistry;
        private readonly IHandAnalyzer _handAnalyzer;
        private readonly GameCommandFactory _commandFactory;
        private readonly IGameCommandInvoker _commandInvoker;

        [Inject]
        public PlayerActionState(
            IStateSwitcher<InGameStateType> stateSwitcher,
            InGameModel model,
            InGameSettings settings,
            IPlayerAgentRegistry agentRegistry,
            IHandAnalyzer handAnalyzer,
            GameCommandFactory commandFactory,
            IGameCommandInvoker commandInvoker) : base(stateSwitcher)
        {
            _model = model;
            _settings = settings;
            _agentRegistry = agentRegistry;
            _handAnalyzer = handAnalyzer;
            _commandFactory = commandFactory;
            _commandInvoker = commandInvoker;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            var seat = _model.CurrentSeat.CurrentValue;
            var player = _model.GetPlayer(seat);

            var canDeclareTsumo = _model.CanDeclareTsumo
                && _handAnalyzer.IsWinningHand(player.Cards.ConcealedCards, player.Cards.Melds);
            var canDeclareRiichi = CanDeclareRiichi(player);

            var context = new TurnDecisionContext(
                _model,
                seat,
                canDeclareTsumo,
                canDeclareRiichi,
                _settings.ThinkTimeSeconds);

            // 時間切れはツモ切り（ツモした札をそのまま捨てる）
            var fallback = TurnAction.Discard(GetDefaultDiscard(player));

            var action = await DecideTurnActionAsync(_agentRegistry.Get(seat), context, fallback, cancellationToken);
            action = Sanitize(action, canDeclareTsumo, canDeclareRiichi, fallback);

            var executed = await _commandInvoker.ExecuteAsync(
                _commandFactory.CreateFromTurnAction(seat, action),
                cancellationToken);

            if (!executed)
            {
                // 手札に無い札を指定されたなど、実行できない行動が返ってきた場合。
                // 進行を止めないようにツモ切りへ落とす。
                action = fallback;
                await _commandInvoker.ExecuteAsync(
                    _commandFactory.CreateFromTurnAction(seat, action),
                    cancellationToken);
            }

            RequestTransition(action.Type == TurnActionType.Tsumo
                ? InGameStateType.Win
                : InGameStateType.ClaimWait);
        }

        /// <summary>制限時間つきで手番の行動を決めさせる。時間切れの場合は fallback を返す。</summary>
        private static UniTask<TurnAction> DecideTurnActionAsync(
            IPlayerAgent agent,
            TurnDecisionContext context,
            TurnAction fallback,
            CancellationToken cancellationToken)
            => TimeLimitedDecision.RunAsync(
                token => agent.DecideTurnActionAsync(context, token),
                fallback,
                context.TimeLimitSeconds,
                cancellationToken);

        /// <summary>宣言できない行動が返ってきた場合に、実行可能な行動へ丸める。</summary>
        private TurnAction Sanitize(TurnAction action, bool canDeclareTsumo, bool canDeclareRiichi, TurnAction fallback)
        {
            return action.Type switch
            {
                TurnActionType.Tsumo when !canDeclareTsumo => fallback,
                TurnActionType.Riichi when !canDeclareRiichi => TurnAction.Discard(action.Card),
                _ => action,
            };
        }

        private Card GetDefaultDiscard(PlayerModel player)
        {
            if (player.Cards.LastDrawnCard != null) return player.Cards.LastDrawnCard;

            // 鳴いた直後はツモ札が無いので、手札の末尾を捨てる
            return player.Cards.ConcealedCards[player.Cards.ConcealedCards.Count - 1];
        }

        /// <summary>リーチできるか。門前かつ、1 枚捨てればテンパイになる形があること。</summary>
        private bool CanDeclareRiichi(PlayerModel player)
        {
            if (!player.Cards.IsMenzen || player.Status.IsRiichi) return false;

            var hand = new List<Card>(player.Cards.ConcealedCards);
            for (var i = 0; i < hand.Count; i++)
            {
                var removed = hand[i];
                hand.RemoveAt(i);
                var isTenpai = _handAnalyzer.IsTenpai(hand, player.Cards.Melds);
                hand.Insert(i, removed);

                if (isTenpai) return true;
            }

            return false;
        }
    }
}
