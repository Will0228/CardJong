using System;
using CardJong.Core;
using VContainer;

namespace CardJong.OutGame.States
{
    /// <summary>ステート識別子から実体を DI コンテナ経由で解決する。</summary>
    public sealed class OutGameStateFactory : IStateFactory<OutGameStateType>
    {
        private readonly IObjectResolver _resolver;

        [Inject]
        public OutGameStateFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        IAsyncState<OutGameStateType> IStateFactory<OutGameStateType>.Create(OutGameStateType key) => key switch
        {
            OutGameStateType.Home => _resolver.Resolve<HomeState>(),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "未登録のステートです。"),
        };
    }
}
