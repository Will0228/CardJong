using System.Threading;
using CardJong.Core;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>ゲーム終了画面。ここでステートマシンのループが終わる。</summary>
    public sealed class GameEndState : AsyncStateBase<InGameStateType>
    {
        private readonly InGameModel _model;
        private readonly IInGamePresentation _presentation;

        [Inject]
        public GameEndState(
            IStateSwitcher<InGameStateType> stateSwitcher,
            InGameModel model,
            IInGamePresentation presentation) : base(stateSwitcher)
        {
            _model = model;
            _presentation = presentation;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            var result = _model.BuildGameResult();
            await _presentation.ShowGameResultAsync(result, cancellationToken);

            RequestExit();
        }
    }
}
