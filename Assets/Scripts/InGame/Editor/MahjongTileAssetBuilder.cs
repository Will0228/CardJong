using System.IO;
using CardJong.InGame.Cards;
using CardJong.InGame.Presentation.Tiles;
using UnityEditor;
using UnityEngine;

namespace CardJong.InGame.Editor
{
    /// <summary>
    /// 牌のメッシュ・アトラス・マテリアル・プレハブを一括で作り直す。
    /// </summary>
    /// <remarks>
    /// 生成専用のユーティリティなので static で置く。
    /// 作り直しても GUID が変わらないよう、既存アセットがあれば中身だけ差し替える。
    /// </remarks>
    public static class MahjongTileAssetBuilder
    {
        /// <summary>生成物の置き場所。</summary>
        public const string RootFolder = "Assets/Art/Tiles";

        /// <summary>牌のシェーダー名。</summary>
        public const string ShaderName = "CardJong/Mahjong Tile";

        private const string MeshPath = RootFolder + "/MahjongTile.mesh";
        private const string AtlasTexturePath = RootFolder + "/CardFaceAtlas.png";
        private const string AtlasPath = RootFolder + "/CardFaceAtlas.asset";
        private const string FaceMaterialPath = RootFolder + "/MahjongTileFace.mat";
        private const string BodyMaterialPath = RootFolder + "/MahjongTileBody.mat";
        private const string PrefabPath = RootFolder + "/MahjongTile.prefab";

        private const string FaceKeyword = "MAHJONG_TILE_FACE";
        private const int AtlasCellWidth = 128;

        /// <summary>牌に使うアセットを一式作り直す。</summary>
        [MenuItem("CardJong/麻雀牌のアセットを生成する")]
        public static void BuildAll()
        {
            var shape = MahjongTileShape.Standard;

            EnsureFolder("Assets/Art");
            EnsureFolder(RootFolder);

            var mesh = CreateOrReplace(MahjongTileMeshFactory.Create(shape), MeshPath);
            var atlasTexture = BuildAtlasTexture(shape);
            var atlas = BuildAtlas(atlasTexture);
            var faceMaterial = BuildFaceMaterial(atlasTexture, atlas);
            var bodyMaterial = BuildBodyMaterial();
            BuildPrefab(mesh, faceMaterial, bodyMaterial, atlas);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"麻雀牌のアセットを生成しました: {RootFolder}");
        }

        private static Texture2D BuildAtlasTexture(MahjongTileShape shape)
        {
            var generated = CardFaceAtlasTextureBuilder.Build(AtlasCellWidth, shape);
            var bytes = generated.EncodeToPNG();
            Object.DestroyImmediate(generated);

            File.WriteAllBytes(ToAbsolutePath(AtlasTexturePath), bytes);
            AssetDatabase.ImportAsset(AtlasTexturePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasTexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasTexturePath);
        }

        private static CardFaceAtlas BuildAtlas(Texture2D texture)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<CardFaceAtlas>(AtlasPath);
            if (atlas == null)
            {
                atlas = ScriptableObject.CreateInstance<CardFaceAtlas>();
                AssetDatabase.CreateAsset(atlas, AtlasPath);
            }

            atlas.EditorConfigure(texture, CardFaceAtlasTextureBuilder.Columns, CardFaceAtlasTextureBuilder.SuitRowOrder.Length, CardFaceAtlasTextureBuilder.SuitRowOrder);
            EditorUtility.SetDirty(atlas);
            return atlas;
        }

        private static Material BuildFaceMaterial(Texture2D texture, CardFaceAtlas atlas)
        {
            var material = LoadOrCreateMaterial(FaceMaterialPath, "MahjongTileFace");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_BackColor", Color.white);
            material.SetFloat("_Smoothness", 0.45f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_FaceMode", 1f);
            material.EnableKeyword(FaceKeyword);

            // 何も指し込まれていないときでも 1 枚分のセルが出るよう、既定を A のスペードにしておく。
            material.SetVector("_FaceRect", atlas.GetFaceRect(new Card(Suit.Spade, Rank.Ace)));
            ApplyDoraDefaults(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material BuildBodyMaterial()
        {
            var material = LoadOrCreateMaterial(BodyMaterialPath, "MahjongTileBody");
            material.SetTexture("_BaseMap", null);
            material.SetColor("_BaseColor", new Color(0.965f, 0.957f, 0.925f));
            material.SetColor("_BackColor", new Color(0.886f, 0.741f, 0.400f));
            material.SetFloat("_Smoothness", 0.55f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_FaceMode", 0f);
            material.DisableKeyword(FaceKeyword);
            ApplyDoraDefaults(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyDoraDefaults(Material material)
        {
            material.SetColor("_DoraColor", new Color(1.6f, 1.05f, 0.32f, 1f));
            material.SetFloat("_DoraIntensity", 0f);
            material.SetFloat("_DoraCoreStrength", 0.18f);
            material.SetFloat("_DoraRimPower", 2.5f);
            material.SetFloat("_DoraRimStrength", 1.6f);
            material.SetFloat("_DoraPulseSpeed", 0.7f);
            material.SetFloat("_DoraPulseDepth", 0.45f);
            material.SetFloat("_DoraSweepSpeed", 0.45f);
            material.SetFloat("_DoraSweepWidth", 0.12f);
            material.SetFloat("_DoraSweepStrength", 1.1f);
            material.SetFloat("_DoraTint", 0.35f);
            material.enableInstancing = true;
        }

        private static Material LoadOrCreateMaterial(string path, string name)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null) throw new FileNotFoundException($"シェーダーが見つかりません: {ShaderName}");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
                return material;
            }

            material.shader = shader;
            return material;
        }

        private static void BuildPrefab(Mesh mesh, Material faceMaterial, Material bodyMaterial, CardFaceAtlas atlas)
        {
            var root = new GameObject("MahjongTile", typeof(MeshFilter), typeof(MeshRenderer), typeof(MahjongTileView));
            try
            {
                root.GetComponent<MeshFilter>().sharedMesh = mesh;

                var meshRenderer = root.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = new[] { faceMaterial, bodyMaterial };

                var view = new SerializedObject(root.GetComponent<MahjongTileView>());
                view.FindProperty("_meshRenderer").objectReferenceValue = meshRenderer;
                view.FindProperty("_faceAtlas").objectReferenceValue = atlas;
                view.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static T CreateOrReplace<T>(T asset, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            EditorUtility.CopySerialized(asset, existing);
            Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>Assets/ から始まるパスを実ファイルのパスに直す。</summary>
        private static string ToAbsolutePath(string assetPath)
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
