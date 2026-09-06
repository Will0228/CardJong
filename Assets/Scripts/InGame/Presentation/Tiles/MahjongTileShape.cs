namespace CardJong.InGame.Presentation.Tiles
{
    /// <summary>
    /// 麻雀牌の寸法。単位はメートルで、既定値は実物の「26 ミリ牌」（高さ 26 / 幅 20 / 奥行 16 mm）に揃えている。
    /// </summary>
    /// <remarks>
    /// 牌は面を -Z に向けて立っている。Unity 標準の Quad と同じ向きなので、
    /// 既定カメラ（-Z から +Z を見る）の前に置けばそのまま牌面が見える。
    /// </remarks>
    public sealed record MahjongTileShape
    {
        /// <summary>既定の寸法。読み取り専用の共有値。</summary>
        public static readonly MahjongTileShape Standard = new();

        /// <summary>幅（X）。</summary>
        public float Width { get; init; } = 0.020f;

        /// <summary>高さ（Y）。</summary>
        public float Height { get; init; } = 0.026f;

        /// <summary>奥行（Z）。牌面が -Z 側、背が +Z 側。</summary>
        public float Depth { get; init; } = 0.016f;

        /// <summary>輪郭の角丸半径。</summary>
        public float CornerRadius { get; init; } = 0.0028f;

        /// <summary>前後の縁を丸める量。牌面はこの分だけ内側に小さくなる。</summary>
        public float EdgeBevel { get; init; } = 0.0016f;

        /// <summary>牌面側の白い部分の厚み。ここより奥は象牙色の胴体になる。</summary>
        public float FacePlateDepth { get; init; } = 0.0055f;

        /// <summary>角丸 1 箇所あたりの分割数。</summary>
        public int CornerSegments { get; init; } = 5;

        /// <summary>縁の丸めの分割数。</summary>
        public int BevelSegments { get; init; } = 3;

        /// <summary>牌面（イラストを貼る面）の幅。</summary>
        public float FaceWidth => Width - EdgeBevel * 2f;

        /// <summary>牌面（イラストを貼る面）の高さ。</summary>
        public float FaceHeight => Height - EdgeBevel * 2f;
    }
}
