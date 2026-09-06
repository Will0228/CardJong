using System;

namespace CardJong.Core
{
    /// <summary>
    /// ステート識別子から <see cref="IAsyncState{TKey}"/> を生成する。
    /// 実装側で DI コンテナからステートを解決する想定。
    /// </summary>
    public interface IStateFactory<TKey> where TKey : struct, Enum
    {
        IAsyncState<TKey> Create(TKey key);
    }
}
