namespace CardJong.InGame.Model
{
    /// <summary>プレイヤー 1 人分の状態。席順と、局の中で立つフラグを持つ。</summary>
    public sealed class PlayerStatusModel
    {
        public int Seat { get; }

        public bool IsRiichi { get; private set; }

        /// <summary>見逃しによる一時フリテン。次のツモまでロンできない。</summary>
        public bool IsTemporaryFuriten { get; private set; }

        public PlayerStatusModel(int seat)
        {
            Seat = seat;
        }

        /// <summary>局の開始時にリセットする。席順は変わらないので残す。</summary>
        public void ResetForNewRound()
        {
            IsRiichi = false;
            IsTemporaryFuriten = false;
        }

        public void DeclareRiichi()
        {
            IsRiichi = true;
        }

        public void SetTemporaryFuriten(bool value)
        {
            IsTemporaryFuriten = value;
        }
    }
}
