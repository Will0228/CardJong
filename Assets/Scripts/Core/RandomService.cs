using System;

namespace CardJong.Core
{
    /// <summary>
    /// 乱数の供給口。テスト時に固定シードへ差し替えられるようにインターフェースを挟む。
    /// </summary>
    public interface IRandomService
    {
        /// <summary>0 以上 maxExclusive 未満の整数を返す。</summary>
        int Next(int maxExclusive);
    }

    /// <summary><see cref="System.Random"/> を使う既定実装。</summary>
    public sealed class SystemRandomService : IRandomService
    {
        private readonly Random _random;

        public SystemRandomService() : this(Environment.TickCount)
        {
        }

        public SystemRandomService(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }
}
