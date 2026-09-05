using System.Threading;
using CardJong.Core;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>誰かが上がったときの演出画面。</summary>
    public sealed class WinState : InGameStateBase
    {
        private readonly InGameModel _model;
        private readonly IInGamePresentation _presentation;

        [Inject]
        public WinState(
            IStateSwitcher<InGameStateType> stateSwitcher,
            InGameModel model,
            IInGamePresentation presentation) : base(stateSwitcher)
        {
            _model = model;
            _presentation = presentation;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            var win = _model.PendingWin;
            if (win != null)
            {
                await _presentation.ShowWinAsync(win, cancellationToken);
            }

            RequestTransition(InGameStateType.RoundEnd);
        }
    }
}
