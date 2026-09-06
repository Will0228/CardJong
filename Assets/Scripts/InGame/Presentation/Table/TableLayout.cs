using CardJong.InGame.Presentation.Tiles;
using UnityEngine;

namespace CardJong.InGame.Presentation.Table
{
    /// <summary>
    /// 卓のどこに牌を置くかを決める。単位はメートルで、<see cref="MahjongTileShape"/> の実寸に合わせてある。
    /// </summary>
    /// <remarks>
    /// 席ごとの座標は「そのプレイヤーが -Z 側に座り、+Z が卓の中心」を向くローカル系で考え、
    /// 最後に席番号ぶんの Y 軸回転を掛ける。こうすると 4 席とも同じ式で置ける。
    /// 牌は面が -Z を向いているので、この向きのまま立てればそのプレイヤーに面が向く
    /// （＝他家の手牌は自動的に背しか見えない）。
    /// </remarks>
    public sealed record TableLayout
    {
        /// <summary>既定の配置。読み取り専用の共有値。</summary>
        public static readonly TableLayout Standard = new();

        /// <summary>牌の寸法。間隔の既定値はこれを基準に決めている。</summary>
        public MahjongTileShape Tile { get; init; } = MahjongTileShape.Standard;

        /// <summary>手牌の列の、卓の中心からの距離。</summary>
        public float HandDistance { get; init; } = 0.330f;

        /// <summary>鳴いた組を置く列の、卓の中心からの距離。</summary>
        public float MeldDistance { get; init; } = 0.288f;

        /// <summary>河の 1 行目の、卓の中心からの距離。</summary>
        public float DiscardDistance { get; init; } = 0.238f;

        /// <summary>牌を横に並べるときの間隔。</summary>
        public float ColumnSpacing { get; init; } = 0.0215f;

        /// <summary>河の行間。寝かせた牌は縦（高さ方向）が奥行きになるので、幅より広く取る。</summary>
        public float RowSpacing { get; init; } = 0.0285f;

        /// <summary>河 1 行あたりの枚数。</summary>
        public int DiscardColumns { get; init; } = 6;

        /// <summary>鳴いた組を並べ始める X。河の右端より外側に置く。</summary>
        public float MeldOriginX { get; init; } = 0.090f;

        /// <summary>組と組のあいだに空ける幅。</summary>
        public float MeldGap { get; init; } = 0.008f;

        /// <summary>立てた牌の中心の高さ。</summary>
        public float StandingHeight => Tile.Height * 0.5f;

        /// <summary>寝かせた牌の中心の高さ。</summary>
        public float LyingHeight => Tile.Depth * 0.5f;

        /// <summary>寝かせて牌面を上に向ける回転。</summary>
        public static Quaternion FaceUpRotation => Quaternion.Euler(90f, 0f, 0f);

        /// <summary>席の向き。手前（0）から反時計回りに 90 度ずつ。</summary>
        public static Quaternion SlotRotation(int slot) => Quaternion.Euler(0f, -90f * slot, 0f);

        /// <summary>
        /// 席が卓のどの位置に来るか。自分が 0（手前）で、以降は打順どおり反時計回りに 1, 2, 3。
        /// 人間が居ない場合は席番号をそのまま使う。
        /// </summary>
        public static int SlotOf(int seat, int humanSeat, int playerCount)
            => humanSeat < 0 ? seat : (seat - humanSeat + playerCount) % playerCount;

        /// <summary>立てた手牌 1 枚。列の中心が席の正面に来るよう左右に振り分ける。</summary>
        public Pose HandTile(int slot, int index, int count)
        {
            var x = (index - (count - 1) * 0.5f) * ColumnSpacing;
            return Compose(slot, new Vector3(x, StandingHeight, -HandDistance), Quaternion.identity);
        }

        /// <summary>寝かせた河 1 枚。左から右へ並べ、<see cref="DiscardColumns"/> 枚で折り返す。</summary>
        public Pose DiscardTile(int slot, int index)
        {
            var column = index % DiscardColumns;
            var row = index / DiscardColumns;
            var x = (column - (DiscardColumns - 1) * 0.5f) * ColumnSpacing;
            var z = -DiscardDistance + row * RowSpacing;
            return Compose(slot, new Vector3(x, LyingHeight, z), FaceUpRotation);
        }

        /// <summary>寝かせた鳴き 1 枚。組の切れ目が分かるよう、組ごとに少し間を空ける。</summary>
        /// <param name="meldIndex">何組目か。</param>
        /// <param name="cardIndexInAllMelds">その席の鳴き牌を通しで数えた番号。</param>
        public Pose MeldTile(int slot, int meldIndex, int cardIndexInAllMelds)
        {
            var x = MeldOriginX + cardIndexInAllMelds * ColumnSpacing + meldIndex * MeldGap;
            return Compose(slot, new Vector3(x, LyingHeight, -MeldDistance), FaceUpRotation);
        }

        /// <summary>卓の中央に寝かせるドラ表示札。</summary>
        public Pose DoraTile(int index, int count)
        {
            var x = (index - (count - 1) * 0.5f) * ColumnSpacing;
            return new Pose(new Vector3(x, LyingHeight, 0f), FaceUpRotation);
        }

        private static Pose Compose(int slot, Vector3 localPosition, Quaternion localRotation)
        {
            var rotation = SlotRotation(slot);
            return new Pose(rotation * localPosition, rotation * localRotation);
        }
    }
}
