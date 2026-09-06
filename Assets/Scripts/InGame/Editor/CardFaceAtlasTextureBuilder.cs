using CardJong.InGame.Cards;
using CardJong.InGame.Presentation.Tiles;
using UnityEngine;

namespace CardJong.InGame.Editor
{
    /// <summary>
    /// 牌面のイラストが用意できるまでの仮テクスチャを描く。
    /// ランクの文字とマークの記号だけの簡素な絵柄を 13 x 4 のアトラスに並べる。
    /// </summary>
    /// <remarks>
    /// 本番のイラストが入ったら <see cref="CardFaceAtlas"/> のテクスチャを差し替えるだけでよく、
    /// メッシュ・マテリアル・シェーダーはそのまま使える。ここはあくまで仮画像の生成器。
    /// </remarks>
    public static class CardFaceAtlasTextureBuilder
    {
        /// <summary>上の行から順に並べるマーク。</summary>
        public static readonly Suit[] SuitRowOrder = { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };

        /// <summary>ランク数（A〜K）。</summary>
        public const int Columns = 13;

        private const int SuperSample = 3;

        private static readonly Color Background = new(0.972f, 0.960f, 0.925f);
        private static readonly Color RedInk = new(0.752f, 0.145f, 0.157f);
        private static readonly Color BlackInk = new(0.114f, 0.106f, 0.125f);

        /// <summary>
        /// 5 x 7 ドットの文字。上の行から順に 5 文字ずつ並べた 35 文字で 1 字を表す。
        /// 添字は 0〜9 が数字、10 が A、11 が J、12 が Q、13 が K。
        /// </summary>
        private static readonly string[] Glyphs =
        {
            "01110" + "10001" + "10011" + "10101" + "11001" + "10001" + "01110",
            "00100" + "01100" + "00100" + "00100" + "00100" + "00100" + "01110",
            "01110" + "10001" + "00001" + "00010" + "00100" + "01000" + "11111",
            "11111" + "00010" + "00100" + "00010" + "00001" + "10001" + "01110",
            "00010" + "00110" + "01010" + "10010" + "11111" + "00010" + "00010",
            "11111" + "10000" + "11110" + "00001" + "00001" + "10001" + "01110",
            "00110" + "01000" + "10000" + "11110" + "10001" + "10001" + "01110",
            "11111" + "00001" + "00010" + "00100" + "01000" + "01000" + "01000",
            "01110" + "10001" + "10001" + "01110" + "10001" + "10001" + "01110",
            "01110" + "10001" + "10001" + "01111" + "00001" + "00010" + "01100",
            "01110" + "10001" + "10001" + "11111" + "10001" + "10001" + "10001",
            "00111" + "00010" + "00010" + "00010" + "00010" + "10010" + "01100",
            "01110" + "10001" + "10001" + "10001" + "10101" + "10010" + "01101",
            "10001" + "10010" + "10100" + "11000" + "10100" + "10010" + "10001",
        };

        private const int GlyphColumns = 5;
        private const int GlyphRows = 7;

        /// <summary>13 x 4 のアトラスを描いて返す。</summary>
        /// <param name="cellWidth">セル 1 枚の横幅（ピクセル）。縦幅は牌面の縦横比から決まる。</param>
        /// <param name="shape">牌面の縦横比を取るための寸法。</param>
        public static Texture2D Build(int cellWidth, MahjongTileShape shape)
        {
            var cellHeight = Mathf.RoundToInt(cellWidth * (shape.FaceHeight / shape.FaceWidth));
            var rows = SuitRowOrder.Length;
            var width = cellWidth * Columns;
            var height = cellHeight * rows;
            var pixels = new Color32[width * height];

            // 枠線は全セル共通なので 1 枚分だけ先に焼いて使い回す。
            var borderCoverage = BuildBorderCoverage(cellWidth, cellHeight);

            for (var row = 0; row < rows; row++)
            {
                var suit = SuitRowOrder[row];
                var ink = suit is Suit.Heart or Suit.Diamond ? RedInk : BlackInk;

                // テクスチャは下から詰むので、見た目の上の行ほど大きい Y に置く。
                var blockBottom = (rows - 1 - row) * cellHeight;

                for (var column = 0; column < Columns; column++)
                {
                    var glyphIndices = GlyphIndicesOf((Rank)(column + 1));
                    var blockLeft = column * cellWidth;

                    for (var y = 0; y < cellHeight; y++)
                    {
                        for (var x = 0; x < cellWidth; x++)
                        {
                            var index = y * cellWidth + x;
                            var coverage = Mathf.Max(borderCoverage[index], SymbolCoverage(x, y, cellWidth, cellHeight, glyphIndices, suit));
                            pixels[(blockBottom + y) * width + blockLeft + x] = Color.Lerp(Background, ink, coverage);
                        }
                    }
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false) { name = "CardFaceAtlas" };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static float[] BuildBorderCoverage(int cellWidth, int cellHeight)
        {
            var coverage = new float[cellWidth * cellHeight];
            for (var y = 0; y < cellHeight; y++)
            {
                for (var x = 0; x < cellWidth; x++)
                {
                    var hits = 0;
                    for (var sy = 0; sy < SuperSample; sy++)
                    {
                        for (var sx = 0; sx < SuperSample; sx++)
                        {
                            if (IsBorder(SampleU(x, sx, cellWidth), SampleV(y, sy, cellHeight))) hits++;
                        }
                    }

                    coverage[y * cellWidth + x] = (float)hits / (SuperSample * SuperSample);
                }
            }

            return coverage;
        }

        /// <summary>ランクの文字とマークの記号の被覆率。ジャギーを抑えるため 3 x 3 で重ね取りする。</summary>
        private static float SymbolCoverage(int x, int y, int cellWidth, int cellHeight, int[] glyphIndices, Suit suit)
        {
            var hits = 0;
            for (var sy = 0; sy < SuperSample; sy++)
            {
                for (var sx = 0; sx < SuperSample; sx++)
                {
                    var u = SampleU(x, sx, cellWidth);
                    var v = SampleV(y, sy, cellHeight);
                    if (IsRankGlyph(u, v, glyphIndices) || IsSuitSymbol(u, v, suit)) hits++;
                }
            }

            return (float)hits / (SuperSample * SuperSample);
        }

        private static float SampleU(int x, int subSample, int cellWidth)
            => (x + (subSample + 0.5f) / SuperSample) / cellWidth;

        /// <summary>v は上を 0 とする。テクスチャの Y は下向きに増えるので反転させる。</summary>
        private static float SampleV(int y, int subSample, int cellHeight)
            => 1f - (y + (subSample + 0.5f) / SuperSample) / cellHeight;

        /// <summary>ランクを字形の並びに直す。10 だけ 2 文字になる。</summary>
        private static int[] GlyphIndicesOf(Rank rank) => rank switch
        {
            Rank.Ace => new[] { 10 },
            Rank.Jack => new[] { 11 },
            Rank.Queen => new[] { 12 },
            Rank.King => new[] { 13 },
            Rank.Ten => new[] { 1, 0 },
            _ => new[] { (int)rank },
        };

        private static bool IsBorder(float u, float v)
        {
            var point = new Vector2(u - 0.5f, v - 0.5f);
            var distance = RoundedRectDistance(point, new Vector2(0.44f, 0.46f), 0.07f);
            return Mathf.Abs(distance) < 0.006f;
        }

        /// <summary>ランクの文字。10 だけ 2 文字なので横幅を分け合う。</summary>
        private static bool IsRankGlyph(float u, float v, int[] glyphIndices)
        {
            var glyphHeight = 0.30f;
            var glyphWidth = glyphHeight * GlyphColumns / GlyphRows;
            var gap = glyphWidth * 0.25f;
            var totalWidth = glyphWidth * glyphIndices.Length + gap * (glyphIndices.Length - 1);
            var top = 0.10f;

            if (v < top || v > top + glyphHeight) return false;

            var left = 0.5f - totalWidth * 0.5f;
            for (var i = 0; i < glyphIndices.Length; i++)
            {
                var glyphLeft = left + i * (glyphWidth + gap);
                if (u < glyphLeft || u > glyphLeft + glyphWidth) continue;

                var column = Mathf.Clamp((int)((u - glyphLeft) / glyphWidth * GlyphColumns), 0, GlyphColumns - 1);
                var row = Mathf.Clamp((int)((v - top) / glyphHeight * GlyphRows), 0, GlyphRows - 1);
                if (Glyphs[glyphIndices[i]][row * GlyphColumns + column] == '1') return true;
            }

            return false;
        }

        /// <summary>マークの記号。中心を下寄りに置き、上下 1 に正規化した座標で形を判定する。</summary>
        private static bool IsSuitSymbol(float u, float v, Suit suit)
        {
            var scale = 0.19f;
            var point = new Vector2((u - 0.5f) / scale, (0.68f - v) / scale);
            if (point.sqrMagnitude > 4f) return false;

            return suit switch
            {
                Suit.Spade => IsHeartShape(new Vector2(point.x, -point.y)) || IsStem(point),
                Suit.Heart => IsHeartShape(point),
                Suit.Diamond => Mathf.Abs(point.x) / 0.78f + Mathf.Abs(point.y) <= 1f,
                Suit.Club => IsClubShape(point),
                _ => false,
            };
        }

        private static bool IsHeartShape(Vector2 point)
        {
            if ((point - new Vector2(-0.45f, 0.35f)).sqrMagnitude <= 0.25f) return true;
            if ((point - new Vector2(0.45f, 0.35f)).sqrMagnitude <= 0.25f) return true;
            return IsInTriangle(point, new Vector2(-0.92f, 0.35f), new Vector2(0.92f, 0.35f), new Vector2(0f, -1f));
        }

        private static bool IsClubShape(Vector2 point)
        {
            if ((point - new Vector2(0f, 0.48f)).sqrMagnitude <= 0.1764f) return true;
            if ((point - new Vector2(-0.46f, -0.16f)).sqrMagnitude <= 0.1764f) return true;
            if ((point - new Vector2(0.46f, -0.16f)).sqrMagnitude <= 0.1764f) return true;
            return IsStem(point);
        }

        /// <summary>スペードとクラブの軸。下へ向かって末広がりにする。</summary>
        private static bool IsStem(Vector2 point)
        {
            if (point.y < -1f || point.y > -0.15f) return false;

            var spread = Mathf.Pow(Mathf.Max(0f, (-point.y - 0.15f) / 0.85f), 1.6f);
            return Mathf.Abs(point.x) <= 0.07f + 0.33f * spread;
        }

        private static bool IsInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            var d1 = Cross(point - a, b - a);
            var d2 = Cross(point - b, c - b);
            var d3 = Cross(point - c, a - c);
            var hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            var hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>角丸長方形までの符号付き距離。内側が負。</summary>
        private static float RoundedRectDistance(Vector2 point, Vector2 halfSize, float radius)
        {
            var d = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - halfSize + Vector2.one * radius;
            var outside = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f));
            return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + outside.magnitude - radius;
        }
    }
}
