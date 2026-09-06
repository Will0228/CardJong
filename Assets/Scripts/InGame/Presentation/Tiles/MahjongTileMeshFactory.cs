using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardJong.InGame.Presentation.Tiles
{
    /// <summary>
    /// 麻雀牌のメッシュを組み立てる。角丸の輪郭を Z 方向に押し出し、前後の縁を丸めた形。
    /// サブメッシュ 0 が牌面（イラストを貼る面）、1 が本体（縁・側面・背面）。
    /// </summary>
    /// <remarks>
    /// 生成専用のユーティリティなので CardDeckFactory と同じく static で置く。
    /// 頂点カラーの R に「牌面側の白い部分か」を 1/0 で持たせ、本体側のシェーダーで
    /// 白と象牙色を塗り分ける。マテリアルを増やさずに麻雀牌の 2 色構造を出すため。
    /// </remarks>
    public static class MahjongTileMeshFactory
    {
        /// <summary>牌面のサブメッシュ番号。</summary>
        public const int FaceSubMesh = 0;

        /// <summary>本体のサブメッシュ番号。</summary>
        public const int BodySubMesh = 1;

        /// <summary>
        /// Z 方向の 1 断面。輪郭をどれだけ内側へ寄せるか、法線が横向きと前後向きにどう分かれるかを持つ。
        /// </summary>
        /// <remarks>Unity の C# は 9.0 相当で record struct が使えないため readonly struct で書く。</remarks>
        private readonly struct Ring
        {
            public Ring(float inset, float z, float radialNormal, float axialNormal, float frontMask)
            {
                Inset = inset;
                Z = z;
                RadialNormal = radialNormal;
                AxialNormal = axialNormal;
                FrontMask = frontMask;
            }

            /// <summary>輪郭を内側へ寄せる量。</summary>
            public float Inset { get; }

            /// <summary>断面の Z 位置。</summary>
            public float Z { get; }

            /// <summary>法線の横方向成分。</summary>
            public float RadialNormal { get; }

            /// <summary>法線の前後方向成分。</summary>
            public float AxialNormal { get; }

            /// <summary>牌面側の白い部分なら 1、象牙色の胴体なら 0。</summary>
            public float FrontMask { get; }
        }

        /// <summary>角丸長方形の輪郭 1 周分。末尾に始点を複製して UV の継ぎ目を作らないようにしてある。</summary>
        private readonly struct Outline
        {
            public Outline(Vector2[] arcCenters, Vector2[] directions)
            {
                ArcCenters = arcCenters;
                Directions = directions;
            }

            /// <summary>各サンプル点が属する角丸の中心。</summary>
            public Vector2[] ArcCenters { get; }

            /// <summary>角丸の中心から見た向き。</summary>
            public Vector2[] Directions { get; }

            /// <summary>サンプル点の数。</summary>
            public int Count => Directions.Length;
        }

        /// <summary>牌 1 個分のメッシュを生成する。</summary>
        public static Mesh Create(MahjongTileShape shape)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));

            var halfWidth = shape.Width * 0.5f;
            var halfHeight = shape.Height * 0.5f;
            var halfDepth = shape.Depth * 0.5f;
            var bevel = Mathf.Clamp(shape.EdgeBevel, 1e-5f, Mathf.Min(Mathf.Min(halfWidth, halfHeight), halfDepth) * 0.9f);
            var radius = Mathf.Clamp(shape.CornerRadius, bevel + 1e-5f, Mathf.Min(halfWidth, halfHeight));
            var facePlateDepth = Mathf.Clamp(shape.FacePlateDepth, bevel, shape.Depth - bevel);

            var outline = BuildOutline(halfWidth, halfHeight, radius, Mathf.Max(1, shape.CornerSegments));
            var rings = BuildRings(halfDepth, bevel, facePlateDepth, Mathf.Max(1, shape.BevelSegments));

            var outlineCount = outline.Count;
            var ringCount = rings.Length;
            var backCapStart = 1 + outlineCount;
            var stripStart = backCapStart + 1 + outlineCount;
            var vertexCount = stripStart + ringCount * outlineCount;

            var vertices = new List<Vector3>(vertexCount);
            var normals = new List<Vector3>(vertexCount);
            var uvs = new List<Vector2>(vertexCount);
            var colors = new List<Color>(vertexCount);

            var faceWidth = shape.FaceWidth;
            var faceHeight = shape.FaceHeight;

            // 牌面のキャップ。中心から輪郭へ扇状に張る。UV はイラストがそのまま乗るよう 0..1 に正規化する。
            vertices.Add(new Vector3(0f, 0f, -halfDepth));
            normals.Add(Vector3.back);
            uvs.Add(new Vector2(0.5f, 0.5f));
            colors.Add(Color.white);
            for (var i = 0; i < outlineCount; i++)
            {
                var point = OutlinePoint(outline, i, radius - bevel);
                vertices.Add(new Vector3(point.x, point.y, -halfDepth));
                normals.Add(Vector3.back);
                uvs.Add(new Vector2((point.x + faceWidth * 0.5f) / faceWidth, (point.y + faceHeight * 0.5f) / faceHeight));
                colors.Add(Color.white);
            }

            // 背面のキャップ。裏から見たときに左右が反転しないよう U を反転させる。
            vertices.Add(new Vector3(0f, 0f, halfDepth));
            normals.Add(Vector3.forward);
            uvs.Add(new Vector2(0.5f, 0.5f));
            colors.Add(Color.black);
            for (var i = 0; i < outlineCount; i++)
            {
                var point = OutlinePoint(outline, i, radius - bevel);
                vertices.Add(new Vector3(point.x, point.y, halfDepth));
                normals.Add(Vector3.forward);
                uvs.Add(new Vector2((faceWidth * 0.5f - point.x) / faceWidth, (point.y + faceHeight * 0.5f) / faceHeight));
                colors.Add(Color.black);
            }

            // 縁と側面。断面を前から後ろへ並べ、隣り合う断面を帯状につなぐ。
            for (var j = 0; j < ringCount; j++)
            {
                var ring = rings[j];
                var mask = new Color(ring.FrontMask, ring.FrontMask, ring.FrontMask, 1f);
                for (var i = 0; i < outlineCount; i++)
                {
                    var direction = outline.Directions[i];
                    var point = OutlinePoint(outline, i, radius - ring.Inset);
                    vertices.Add(new Vector3(point.x, point.y, ring.Z));
                    normals.Add(new Vector3(direction.x * ring.RadialNormal, direction.y * ring.RadialNormal, ring.AxialNormal).normalized);
                    uvs.Add(new Vector2((float)i / (outlineCount - 1), (ring.Z + halfDepth) / shape.Depth));
                    colors.Add(mask);
                }
            }

            var faceTriangles = new List<int>((outlineCount - 1) * 3);
            for (var i = 0; i < outlineCount - 1; i++)
            {
                faceTriangles.Add(0);
                faceTriangles.Add(1 + i);
                faceTriangles.Add(1 + i + 1);
            }

            var bodyTriangles = new List<int>((outlineCount - 1) * 3 + (ringCount - 1) * (outlineCount - 1) * 6);
            for (var i = 0; i < outlineCount - 1; i++)
            {
                bodyTriangles.Add(backCapStart);
                bodyTriangles.Add(backCapStart + 1 + i + 1);
                bodyTriangles.Add(backCapStart + 1 + i);
            }

            for (var j = 0; j < ringCount - 1; j++)
            {
                var near = stripStart + j * outlineCount;
                var far = stripStart + (j + 1) * outlineCount;
                for (var i = 0; i < outlineCount - 1; i++)
                {
                    bodyTriangles.Add(near + i);
                    bodyTriangles.Add(far + i);
                    bodyTriangles.Add(far + i + 1);

                    bodyTriangles.Add(near + i);
                    bodyTriangles.Add(far + i + 1);
                    bodyTriangles.Add(near + i + 1);
                }
            }

            var mesh = new Mesh { name = "MahjongTile" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(faceTriangles, FaceSubMesh);
            mesh.SetTriangles(bodyTriangles, BodySubMesh);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector2 OutlinePoint(Outline outline, int index, float radius)
            => outline.ArcCenters[index] + outline.Directions[index] * radius;

        /// <summary>
        /// 角丸長方形の輪郭を、-Z から見て時計回りに並べる。
        /// Unity は時計回りが表なので、この順序のまま扇と帯を張れば牌面が表になる。
        /// </summary>
        private static Outline BuildOutline(float halfWidth, float halfHeight, float radius, int cornerSegments)
        {
            var centerX = halfWidth - radius;
            var centerY = halfHeight - radius;
            var cornerCenters = new[]
            {
                new Vector2(centerX, centerY),
                new Vector2(centerX, -centerY),
                new Vector2(-centerX, -centerY),
                new Vector2(-centerX, centerY),
            };
            var startAngles = new[] { 90f, 0f, -90f, -180f };

            var count = 4 * (cornerSegments + 1) + 1;
            var arcCenters = new Vector2[count];
            var directions = new Vector2[count];
            var index = 0;
            for (var corner = 0; corner < 4; corner++)
            {
                for (var step = 0; step <= cornerSegments; step++)
                {
                    var angle = (startAngles[corner] - 90f * step / cornerSegments) * Mathf.Deg2Rad;
                    arcCenters[index] = cornerCenters[corner];
                    directions[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    index++;
                }
            }

            arcCenters[index] = arcCenters[0];
            directions[index] = directions[0];
            return new Outline(arcCenters, directions);
        }

        /// <summary>
        /// 前の丸め、白い側面、象牙色の側面、後ろの丸め、の順に断面を並べる。
        /// 白と象牙の境目は同じ位置に色違いの断面を 2 枚重ね、グラデーションにならないようにしている。
        /// </summary>
        private static Ring[] BuildRings(float halfDepth, float bevel, float facePlateDepth, int bevelSegments)
        {
            var rings = new List<Ring>(bevelSegments * 2 + 4);

            for (var step = 0; step <= bevelSegments; step++)
            {
                var angle = Mathf.PI * 0.5f * step / bevelSegments;
                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);
                rings.Add(new Ring(bevel * (1f - sin), -halfDepth + bevel * (1f - cos), sin, -cos, 1f));
            }

            var splitZ = -halfDepth + facePlateDepth;
            rings.Add(new Ring(0f, splitZ, 1f, 0f, 1f));
            rings.Add(new Ring(0f, splitZ, 1f, 0f, 0f));
            rings.Add(new Ring(0f, halfDepth - bevel, 1f, 0f, 0f));

            for (var step = 1; step <= bevelSegments; step++)
            {
                var angle = Mathf.PI * 0.5f * (bevelSegments - step) / bevelSegments;
                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);
                rings.Add(new Ring(bevel * (1f - sin), halfDepth - bevel * (1f - cos), sin, cos, 0f));
            }

            return rings.ToArray();
        }
    }
}
