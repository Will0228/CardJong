using System;
using R3;

namespace CardJong.InGame.Model
{
    /// <summary>プレイヤー 1 人分の持ち点。局をまたいで持ち越す。</summary>
    public sealed class PlayerScoreModel : IDisposable
    {
        private readonly ReactiveProperty<int> _points;

        /// <summary>現在の持ち点。</summary>
        public ReadOnlyReactiveProperty<int> Points => _points;

        public PlayerScoreModel(int initialPoints)
        {
            _points = new ReactiveProperty<int>(initialPoints);
        }

        public void Add(int delta)
        {
            _points.Value += delta;
        }

        public void Dispose()
        {
            _points.Dispose();
        }
    }
}
