using System.Threading;
using CardJong.Core;
using CardJong.InGame.Actions;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>ゲーム開始。プレイヤーと点数を初期化する。</summary>
    public sealed class GameStartState : InGameStateBase
    {
        private readonly InGameModel _model;
        private readonly InGameSettings _settings;
        private readonly IPlayerAgentRegistry _agentRegistry;
        private readonly IInGamePresentation _presentation;

        [Inject]
        public GameStartState(
            IStateSwitcher<InGameStateType> stateSwitcher,
            InGameModel model,
            InGameSettings settings,
            IPlayerAgentRegistry agentRegistry,
            IInGamePresentation presentation) : base(stateSwitcher)
        {
            _model = model;
            _settings = settings;
            _agentRegistry = agentRegistry;
            _presentation = presentation;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            _model.Setup(_settings.PlayerCount, _settings.InitialScore, _settings.TotalRoundCount);
            _agentRegistry.Setup(_settings.PlayerCount, _settings.HumanSeat);

            await _presentation.ShowGameStartAsync(cancellationToken);
            RequestTransition(InGameStateType.DecideDealer);
        }
    }
}
