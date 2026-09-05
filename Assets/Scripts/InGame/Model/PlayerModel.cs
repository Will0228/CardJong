using System;

namespace CardJong.InGame.Model
{
    /// <summary>
    /// プレイヤー 1 人分。カード・持ち点・状態をそれぞれのモデルに持たせ、ここでは束ねるだけにする。
    /// 状態を書き換えるのは Command 層だけにする。
    /// </summary>
    public sealed class PlayerModel : IDisposable
    {
        /// <summary>手札・鳴き・捨て札。</summary>
        public PlayerCardModel Cards { get; }

        /// <summary>持ち点。</summary>
        public PlayerScoreModel Score { get; }

        /// <summary>席順、リーチ中かどうかなど。</summary>
        public PlayerStatusModel Status { get; }

        public int Seat => Status.Seat;

        public PlayerModel(int seat, int initialScore)
        {
            Cards = new PlayerCardModel();
            Score = new PlayerScoreModel(initialScore);
            Status = new PlayerStatusModel(seat);
        }

        /// <summary>局の開始時に手札まわりをリセットする。点数は持ち越す。</summary>
        public void ResetForNewRound()
        {
            Cards.ResetForNewRound();
            Status.ResetForNewRound();
        }

        public void Dispose()
        {
            Cards.Dispose();
            Score.Dispose();
        }
    }
}
