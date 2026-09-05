using System.Threading;
using CardJong.Core.Commands;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Commands
{
    /// <summary>生き山から 1 枚ツモる。</summary>
    public sealed class DrawCardCommand : IGameCommand
    {
        private readonly InGameModel _model;
        private readonly int _seat;

        /// <summary>実行後にツモった札。</summary>
        public Card? DrawnCard { get; private set; }

        public DrawCardCommand(InGameModel model, int seat)
        {
            _model = model;
            _seat = seat;
        }

        public string DebugName => $"Draw(seat={_seat})";

        public bool CanExecute() => !_model.Wall.IsLiveWallEmpty;

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            var card = _model.Wall.Draw();
            var player = _model.GetPlayer(_seat);

            player.Draw(card);

            // 見逃しによる一時フリテンはツモで解除される。ただしリーチ後はその局ずっと続く。
            if (!player.IsRiichi)
            {
                player.SetTemporaryFuriten(false);
            }

            _model.SetCanDeclareTsumo(true);
            DrawnCard = card;
            return UniTask.CompletedTask;
        }
    }
}
