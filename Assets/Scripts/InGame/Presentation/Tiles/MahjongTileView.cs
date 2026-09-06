using CardJong.InGame.Cards;
using UnityEngine;

namespace CardJong.InGame.Presentation.Tiles
{
    /// <summary>
    /// 牌 1 個の見た目。どのカードを表示するかと、ドラとして光らせるかを受け取る。
    /// </summary>
    /// <remarks>
    /// マテリアルは 208 枚で共有し、牌ごとの差分は MaterialPropertyBlock だけで渡す。
    /// 明滅そのものはシェーダー側が _Time で回すので、ここが毎フレーム送るのは
    /// フェード中の強度だけになる。
    /// </remarks>
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("CardJong/Mahjong Tile View")]
    public sealed class MahjongTileView : MonoBehaviour
    {
        private static readonly int FaceRectId = Shader.PropertyToID("_FaceRect");
        private static readonly int DoraIntensityId = Shader.PropertyToID("_DoraIntensity");

        [SerializeField]
        [Tooltip("牌面と本体の 2 マテリアルを持つ MeshRenderer。")]
        private MeshRenderer _meshRenderer;

        [SerializeField]
        [Tooltip("牌面のイラストを並べたアトラス。未設定ならテクスチャ全面を貼る。")]
        private CardFaceAtlas _faceAtlas;

        [SerializeField]
        [Tooltip("表示するマーク。")]
        private Suit _suit = Suit.Spade;

        [SerializeField]
        [Tooltip("表示するランク。")]
        private Rank _rank = Rank.Ace;

        [SerializeField]
        [Tooltip("ドラとして光らせるか。")]
        private bool _isDora;

        [SerializeField]
        [Min(0f)]
        [Tooltip("ドラの点灯・消灯にかける秒数。0 なら即座に切り替わる。")]
        private float _doraFadeDuration = 0.35f;

        private MaterialPropertyBlock _properties;
        private float _doraIntensity;

        /// <summary>表示中のカード。</summary>
        public Card Card => new(_suit, _rank);

        /// <summary>ドラとして光らせているか。</summary>
        public bool IsDora => _isDora;

        /// <summary>表示するカードを差し替える。</summary>
        public void SetCard(Card card)
        {
            _suit = card.Suit;
            _rank = card.Rank;
            Apply();
        }

        /// <summary>ドラ表示を切り替える。<paramref name="immediate"/> が true ならフェードせず即反映する。</summary>
        public void SetDora(bool isDora, bool immediate = false)
        {
            _isDora = isDora;
            if (immediate)
            {
                _doraIntensity = isDora ? 1f : 0f;
            }

            Apply();
        }

        private void OnEnable()
        {
            _doraIntensity = _isDora ? 1f : 0f;
            Apply();
        }

        private void Update()
        {
            var target = _isDora ? 1f : 0f;
            if (Mathf.Approximately(_doraIntensity, target)) return;

            _doraIntensity = _doraFadeDuration <= 0f
                ? target
                : Mathf.MoveTowards(_doraIntensity, target, Time.deltaTime / _doraFadeDuration);
            Apply();
        }

        private void Reset()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnValidate()
        {
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            _doraIntensity = _isDora ? 1f : 0f;
            Apply();
        }

        private void Apply()
        {
            if (_meshRenderer == null) return;

            // プレハブ生成直後など、MeshRenderer にまだ 2 マテリアルが割り当たっていない
            // タイミングで OnValidate が走ることがある。サブメッシュ 1 が無ければ本体側は諦める。
            var materialCount = _meshRenderer.sharedMaterials.Length;
            if (materialCount <= MahjongTileMeshFactory.FaceSubMesh) return;

            _properties ??= new MaterialPropertyBlock();
            var faceRect = _faceAtlas != null ? _faceAtlas.GetFaceRect(Card) : CardFaceAtlas.FullRect;

            _meshRenderer.GetPropertyBlock(_properties, MahjongTileMeshFactory.FaceSubMesh);
            _properties.SetVector(FaceRectId, faceRect);
            _properties.SetFloat(DoraIntensityId, _doraIntensity);
            _meshRenderer.SetPropertyBlock(_properties, MahjongTileMeshFactory.FaceSubMesh);

            if (materialCount <= MahjongTileMeshFactory.BodySubMesh) return;

            _meshRenderer.GetPropertyBlock(_properties, MahjongTileMeshFactory.BodySubMesh);
            _properties.SetFloat(DoraIntensityId, _doraIntensity);
            _meshRenderer.SetPropertyBlock(_properties, MahjongTileMeshFactory.BodySubMesh);
        }
    }
}
