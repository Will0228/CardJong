using System;
using CardJong.Core;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>ステート識別子から実体を DI コンテナ経由で解決する。</summary>
    public sealed class InGameStateFactory : IStateFactory<InGameStateType>
    {
        private readonly IObjectResolver _resolver;

        [Inject]
        public InGameStateFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        IAsyncState<InGameStateType> IStateFactory<InGameStateType>.Create(InGameStateType key) => key switch
        {
            InGameStateType.GameStart => _resolver.Resolve<GameStartState>(),
            InGameStateType.DecideDealer => _resolver.Resolve<DecideDealerState>(),
            InGameStateType.RoundStart => _resolver.Resolve<RoundStartState>(),
            InGameStateType.Draw => _resolver.Resolve<DrawState>(),
            InGameStateType.PlayerAction => _resolver.Resolve<PlayerActionState>(),
            InGameStateType.ClaimWait => _resolver.Resolve<ClaimWaitState>(),
            InGameStateType.Win => _resolver.Resolve<WinState>(),
            InGameStateType.RoundEnd => _resolver.Resolve<RoundEndState>(),
            InGameStateType.GameEnd => _resolver.Resolve<GameEndState>(),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "未登録のステートです。"),
        };
    }
}
